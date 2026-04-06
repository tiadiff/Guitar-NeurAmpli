Imports NAudio.Wave
Imports NAudio.Dsp

Public Class GuitarAmpEffect
    Implements ISampleProvider

    Private ReadOnly source As ISampleProvider
    Private myFilters As BiQuadFilter()

    ' --- INPUT CONDITIONING ---
    Private inputHPF As BiQuadFilter       ' HPF ~30Hz: rimuove DC offset e rumble subsonico

    ' --- CAB SIM FILTERS ---
    Private cabSimFilter1 As BiQuadFilter
    Private cabSimFilter2 As BiQuadFilter
    Private cabSimHPF As BiQuadFilter
    Private cabResonance As BiQuadFilter
    Private cabPresenceDip As BiQuadFilter
    Private cabRolloff As BiQuadFilter

    ' --- 2x OVERSAMPLING PER DRIVE (4 stadi separati per 24dB/oct) ---
    Private oversampleUpF1 As BiQuadFilter     ' Anti-imaging stage 1
    Private oversampleUpF2 As BiQuadFilter     ' Anti-imaging stage 2
    Private oversampleDownF1 As BiQuadFilter   ' Anti-aliasing stage 1
    Private oversampleDownF2 As BiQuadFilter   ' Anti-aliasing stage 2

    ' --- OUTPUT ANTI-ALIAS ---
    Private outputAAFilter As BiQuadFilter

    ' --- BUFFER ---
    Private delayBuffer As Single()
    Private delayPos As Integer = 0
    Private reverbBuffer As Single()
    Private reverbPos As Integer = 0
    Private chorusBuffer As Single()
    Private chorusPos As Integer = 0
    Private maxDelaySamples As Integer = 384000

    ' --- RECORDER ---
    Private recorder As WaveFileWriter
    Private isRecording As Boolean = False
    Private recLock As New Object

    ' --- PARAMETRI ---
    Public Property InputGain As Single = 3.0F  ' Pre-amp boost per pickup passivi (1.0-10.0)
    Public Property Volume As Single = 1.0F
    Public Property Drive As Single = 0.0F
    Public Property EnableCabSim As Boolean = True
    Public Property CurrentPeak As Single = 0.0F
    Public Property EnableGate As Boolean = True
    Public Property GateThreshold As Single = 0.05F
    Public Property GateActive As Boolean = False

    Public Property BassGain As Single = 0
    Public Property MidGain As Single = 0
    Public Property TrebleGain As Single = 0

    ' EFFETTI
    Public Property CompressorEnabled As Boolean = False
    Public Property CompThreshold As Single = 0.5F
    Public Property CompRatio As Single = 4.0F

    Public Property ChorusEnabled As Boolean = False
    Public Property ChorusRate As Single = 1.0F
    Public Property ChorusDepth As Single = 0.5F

    Public Property DelayEnabled As Boolean = False
    Public Property DelayTimeMs As Single = 350.0F
    Public Property DelayFeedback As Single = 0.45F
    Public Property DelayMix As Single = 0.40F

    Public Property TremoloEnabled As Boolean = False
    Public Property TremoloRate As Single = 5.0F
    Public Property TremoloDepth As Single = 0.4F

    Public Property ReverbEnabled As Boolean = False
    Public Property ReverbMix As Single = 0.35F
    Public Property ReverbDecay As Single = 0.75F

    ' VARIABILI STATO
    Private tremoloPhase As Single = 0.0F
    Private chorusPhase As Single = 0.0F

    ' --- VARIABILI STATO PER DSP AVANZATO ---
    Private gateGain As Single = 0.0F
    Private compEnvelope As Single = 0.0F
    Private tapeFilterSample As Single = 0.0F
    Private reverbDampSample As Single = 0.0F

    Public Sub New(sourceProvider As ISampleProvider)
        source = sourceProvider
        delayBuffer = New Single(maxDelaySamples) {}
        reverbBuffer = New Single(maxDelaySamples) {}
        chorusBuffer = New Single(maxDelaySamples) {}
        UpdateFilters()
    End Sub

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return source.WaveFormat
        End Get
    End Property

    Public Sub UpdateFilters()
        If source Is Nothing OrElse source.WaveFormat Is Nothing Then Return
        Dim rate As Single = CSng(source.WaveFormat.SampleRate)

        ' === INPUT CONDITIONING ===
        ' HPF a 30Hz: rimuove DC offset dalla scheda audio e rumble subsonico
        inputHPF = BiQuadFilter.HighPassFilter(rate, 30.0F, 0.707F)

        ' === EQ OTTIMIZZATO PER CHITARRA ELETTRICA ===
        ' Frequenze centrate sullo spettro reale della chitarra, Q più ampio per musicale warmth
        myFilters = New BiQuadFilter() {
            BiQuadFilter.PeakingEQ(rate, 200.0F, 0.6F, BassGain),    ' Corpo/fondamentale (era 100Hz)
            BiQuadFilter.PeakingEQ(rate, 800.0F, 0.7F, MidGain),     ' Punch/growl (era 1000Hz)
            BiQuadFilter.PeakingEQ(rate, 3200.0F, 0.8F, TrebleGain)  ' Presence/bite (era 5000Hz)
        }

        ' === CAB SIM (Profilo 4x12 bilanciato — più aperto e realistico) ===
        cabSimFilter1 = BiQuadFilter.LowPassFilter(rate, 4800.0F, 0.707F)  ' LP (da 4200→4800: più aria)
        cabSimFilter2 = BiQuadFilter.LowPassFilter(rate, 4800.0F, 0.707F)  ' 2° stadio (24dB/oct)
        cabSimHPF = BiQuadFilter.HighPassFilter(rate, 75.0F, 0.707F)
        cabResonance = BiQuadFilter.PeakingEQ(rate, 100.0F, 1.0F, 2.5F)    ' Corpo (da 3.0→2.5dB, più gentile)
        cabPresenceDip = BiQuadFilter.PeakingEQ(rate, 3500.0F, 1.5F, -2.5F) ' Cono (da -4→-2.5dB)
        cabRolloff = BiQuadFilter.LowPassFilter(rate, 6000.0F, 0.5F)        ' Rolloff (da 5500→6000Hz)

        ' === 2x OVERSAMPLING FILTERS (4 stadi separati per 24dB/oct) ===
        Dim osCutoff = rate * 0.45F
        Dim osRate = rate * 2.0F
        oversampleUpF1 = BiQuadFilter.LowPassFilter(osRate, osCutoff, 0.707F)
        oversampleUpF2 = BiQuadFilter.LowPassFilter(osRate, osCutoff, 0.707F)
        oversampleDownF1 = BiQuadFilter.LowPassFilter(osRate, osCutoff, 0.707F)
        oversampleDownF2 = BiQuadFilter.LowPassFilter(osRate, osCutoff, 0.707F)

        ' === OUTPUT ANTI-ALIAS ===
        outputAAFilter = BiQuadFilter.LowPassFilter(rate, 20000.0F, 0.707F)
    End Sub

    Public Sub StartRecording(filename As String)
        SyncLock recLock
            recorder = New WaveFileWriter(filename, source.WaveFormat)
            isRecording = True
        End SyncLock
    End Sub

    Public Sub StopRecording()
        SyncLock recLock
            isRecording = False
            If recorder IsNot Nothing Then
                recorder.Dispose()
                recorder = Nothing
            End If
        End SyncLock
    End Sub

    Public Function Read(buffer As Single(), offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        Dim samplesRead = source.Read(buffer, offset, count)
        Dim maxSample As Single = 0
        Dim rate As Single = CSng(source.WaveFormat.SampleRate)

        ' --- SNAPSHOT thread-safe dei flag ---
        Dim snapGate = EnableGate
        Dim snapComp = CompressorEnabled
        Dim snapDrive = Drive
        Dim snapCabSim = EnableCabSim
        Dim snapChorus = ChorusEnabled
        Dim snapTremolo = TremoloEnabled
        Dim snapDelay = DelayEnabled
        Dim snapReverb = ReverbEnabled
        Dim snapVolume = Volume
        Dim snapInputGain = InputGain

        ' --- Coefficienti DSP Dipendenti dal Sample Rate ---
        Dim gateAttackCoeff As Single = CSng(1.0 - Math.Exp(-1.0 / (rate * 0.002)))
        Dim gateReleaseCoeff As Single = CSng(1.0 - Math.Exp(-1.0 / (rate * 0.050)))
        Dim compAttackCoeff As Single = CSng(1.0 - Math.Exp(-1.0 / (rate * 0.005)))
        Dim compReleaseCoeff As Single = CSng(1.0 - Math.Exp(-1.0 / (rate * 0.050)))
        Dim tapeLPFAlpha As Single = CSng(1.0 - Math.Exp(-2.0 * Math.PI * 3500.0 / rate))

        For i As Integer = 0 To samplesRead - 1
            Dim sample = buffer(offset + i)

            ' 0. INPUT CONDITIONING: HPF 30Hz + Pre-Amp Gain
            If inputHPF IsNot Nothing Then
                sample = inputHPF.Transform(sample)
            End If
            sample *= snapInputGain

            Dim absSample = Math.Abs(sample)

            ' 1. SOFT NOISE GATE (Rate-Independent)
            Dim targetGate As Single = If(snapGate AndAlso absSample < GateThreshold, 0.0F, 1.0F)
            Dim gateCoeff As Single = If(targetGate < gateGain, gateReleaseCoeff, gateAttackCoeff)
            gateGain += gateCoeff * (targetGate - gateGain)

            sample *= gateGain
            GateActive = (gateGain < 0.1F)

            ' 2. COMPRESSORE (Soft-Knee Peak Compressor, Rate-Independent)
            If snapComp Then
                Dim threshAbsolute = 1.0F - CompThreshold
                If threshAbsolute < 0.01F Then threshAbsolute = 0.01F 
                Dim diff = Math.Abs(sample) - threshAbsolute
                
                If diff > 0 Then
                    Dim targetG = 1.0F - (diff * (1.0F - (1.0F / CompRatio)))
                    compEnvelope += compAttackCoeff * (targetG - compEnvelope)
                Else
                    compEnvelope += compReleaseCoeff * (1.0F - compEnvelope)
                End If
                If compEnvelope > 1.0F Then compEnvelope = 1.0F
                
                sample *= compEnvelope
                sample *= 1.0F + (CompThreshold * 0.5F)
            End If

            ' 3. ASYMMETRIC TUBE DRIVE con 2x Oversampling CORRETTO
            If snapDrive > 0 Then
                sample *= (1.0F + (snapDrive * 6.0F))
                Dim tubeBias As Single = 0.25F * (snapDrive / 10.0F)

                ' --- 2x OVERSAMPLING: 4 stadi filtro (24dB/oct), implementazione corretta ---
                ' Upsample: zero-stuffing [sample*2, 0] → filtro anti-imaging a 2 stadi
                Dim up1 = oversampleUpF2.Transform(oversampleUpF1.Transform(sample * 2.0F))
                Dim up2 = oversampleUpF2.Transform(oversampleUpF1.Transform(0.0F))

                ' Saturazione asimmetrica a rate doppio (entrambi i campioni oversampled)
                Dim sat1 = CSng(Math.Tanh(up1 + tubeBias)) - CSng(Math.Tanh(tubeBias))
                Dim sat2 = CSng(Math.Tanh(up2 + tubeBias)) - CSng(Math.Tanh(tubeBias))

                ' Downsample: filtro anti-aliasing a 2 stadi → decimazione (primo campione)
                Dim d1 = oversampleDownF2.Transform(oversampleDownF1.Transform(sat1))
                oversampleDownF2.Transform(oversampleDownF1.Transform(sat2)) ' processa ma scarta
                sample = d1 ' Prende il campione allineato al tempo originale

                ' Makeup gain ADATTIVO: preserva livello a basso drive, comprime ad alto gain
                sample *= 0.6F + (0.4F / (1.0F + snapDrive * 0.3F))
            End If

            ' 4. EQ & MULTI-STAGE CAB SIM
            If myFilters IsNot Nothing Then
                For Each f In myFilters : sample = f.Transform(sample) : Next
            End If
            If snapCabSim AndAlso cabSimFilter1 IsNot Nothing Then
                sample = cabSimHPF.Transform(sample)
                sample = cabResonance.Transform(sample)
                sample = cabSimFilter1.Transform(sample)
                sample = cabSimFilter2.Transform(sample)
                sample = cabPresenceDip.Transform(sample)
                sample = cabRolloff.Transform(sample)
            End If

            ' 5. CHORUS (Interpolazione Cubica Hermite)
            If snapChorus Then
                Dim lfoHz = ChorusRate
                Dim depthSamples = (ChorusDepth * 0.006F) * rate
                Dim baseDelaySamples = 0.012F * rate
                
                chorusPhase += CSng((Math.PI * 2 * lfoHz) / rate)
                If chorusPhase > CSng(Math.PI * 2) Then chorusPhase -= CSng(Math.PI * 2)
                
                Dim currentDelay = baseDelaySamples + (CSng(Math.Sin(chorusPhase)) * depthSamples)
                Dim readPosDelay = chorusPos - currentDelay
                If readPosDelay < 0 Then readPosDelay += maxDelaySamples
                
                Dim idx = CInt(Math.Floor(readPosDelay))
                Dim frac As Single = readPosDelay - idx
                Dim s0 = chorusBuffer((idx - 1 + maxDelaySamples) Mod maxDelaySamples)
                Dim s1 = chorusBuffer(idx Mod maxDelaySamples)
                Dim s2 = chorusBuffer((idx + 1) Mod maxDelaySamples)
                Dim s3 = chorusBuffer((idx + 2) Mod maxDelaySamples)

                Dim c0 = s1
                Dim c1 = 0.5F * (s2 - s0)
                Dim c2 = s0 - 2.5F * s1 + 2.0F * s2 - 0.5F * s3
                Dim c3 = 0.5F * (s3 - s0) + 1.5F * (s1 - s2)
                Dim chorusDelaySample = ((c3 * frac + c2) * frac + c1) * frac + c0

                sample = (sample * (1.0F - (ChorusDepth * 0.5F))) + (chorusDelaySample * ChorusDepth)
            End If
            
            chorusBuffer(chorusPos) = sample
            chorusPos = (chorusPos + 1) Mod maxDelaySamples

            ' 6. TREMOLO
            If snapTremolo Then
                Dim modFactor = 1.0F - (TremoloDepth * (0.5F + (0.5F * CSng(Math.Sin(tremoloPhase)))))
                sample *= modFactor
                tremoloPhase += CSng((Math.PI * 2 * TremoloRate) / rate)
                If tremoloPhase > Math.PI * 2 Then tremoloPhase -= CSng(Math.PI * 2)
            End If

            ' 7. TAPE DELAY (Feedback filtrato)
            If snapDelay Then
                Dim tMs = DelayTimeMs
                If tMs < 10 Then tMs = 10
                Dim readPos = delayPos - CInt((tMs / 1000.0F) * rate)
                If readPos < 0 Then readPos += maxDelaySamples

                Dim echo = delayBuffer(readPos)
                tapeFilterSample += tapeLPFAlpha * (echo - tapeFilterSample)
                sample += tapeFilterSample * DelayMix
                delayBuffer(delayPos) = sample + (tapeFilterSample * DelayFeedback)
            Else
                delayBuffer(delayPos) = sample
            End If
            delayPos = (delayPos + 1) Mod maxDelaySamples

            ' 8. REVERB (8-Tap Diffusion con HF damping)
            If snapReverb Then
                Dim r1 = reverbBuffer((reverbPos - CInt(rate * 0.0137F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r2 = reverbBuffer((reverbPos - CInt(rate * 0.0227F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r3 = reverbBuffer((reverbPos - CInt(rate * 0.0371F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r4 = reverbBuffer((reverbPos - CInt(rate * 0.0413F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r5 = reverbBuffer((reverbPos - CInt(rate * 0.0533F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r6 = reverbBuffer((reverbPos - CInt(rate * 0.0671F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r7 = reverbBuffer((reverbPos - CInt(rate * 0.0787F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r8 = reverbBuffer((reverbPos - CInt(rate * 0.0897F) + maxDelaySamples) Mod maxDelaySamples)

                Dim earlyRef = (r1 + r2 + r3 + r4) * 0.25F
                Dim lateRef = (r5 + r6 + r7 + r8) * 0.15F
                Dim revSignal = earlyRef + lateRef

                sample += revSignal * ReverbMix

                Dim dampAlpha As Single = CSng(1.0 - Math.Exp(-2.0 * Math.PI * 4000.0 / rate))
                reverbDampSample += dampAlpha * (sample - reverbDampSample)

                reverbBuffer(reverbPos) = reverbDampSample * ReverbDecay
            Else
                reverbBuffer(reverbPos) = sample * 0.5F
            End If
            reverbPos = (reverbPos + 1) Mod maxDelaySamples

            ' MASTER
            sample *= snapVolume

            ' OUTPUT ANTI-ALIAS FILTER (LP 20kHz)
            If outputAAFilter IsNot Nothing Then
                sample = outputAAFilter.Transform(sample)
            End If

            ' MASTER LIMITER (Soft-Knee)
            Dim absOut = Math.Abs(sample)
            If absOut > 0.9F Then
                Dim excess = absOut - 0.9F
                Dim compressed = 0.9F + CSng(Math.Tanh(excess * 5.0F)) * 0.1F
                sample = Math.Sign(sample) * compressed
            End If

            ' Safety Anti-NaN / Anti-Infinity
            If Single.IsNaN(sample) OrElse Single.IsInfinity(sample) Then sample = 0.0F
            If sample > 1.0F Then sample = 1.0F
            If sample < -1.0F Then sample = -1.0F

            If Math.Abs(sample) > maxSample Then maxSample = Math.Abs(sample)
            buffer(offset + i) = sample

        Next

        ' RECORDING THREAD-SAFE
        SyncLock recLock
            If isRecording AndAlso recorder IsNot Nothing Then
                Try
                    recorder.WriteSamples(buffer, offset, samplesRead)
                Catch ex As Exception
                End Try
            End If
        End SyncLock

        CurrentPeak = maxSample
        Return samplesRead
    End Function
End Class