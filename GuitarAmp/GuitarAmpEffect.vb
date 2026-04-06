Imports NAudio.Wave
Imports NAudio.Dsp

Public Class GuitarAmpEffect
    Implements ISampleProvider

    Private ReadOnly source As ISampleProvider
    Private myFilters As BiQuadFilter()

    ' --- CAB SIM FILTERS ---
    Private cabSimFilter1 As BiQuadFilter
    Private cabSimFilter2 As BiQuadFilter
    Private cabSimHPF As BiQuadFilter
    Private cabResonance As BiQuadFilter    ' Risonanza speaker ~100Hz
    Private cabPresenceDip As BiQuadFilter  ' Dip presence ~3.5kHz
    Private cabRolloff As BiQuadFilter      ' Rolloff finale morbido

    ' --- OVERSAMPLING PER DRIVE ---
    Private oversampleUp As BiQuadFilter    ' Anti-imaging LP in salita
    Private oversampleDown As BiQuadFilter  ' Anti-aliasing LP in discesa

    ' --- OUTPUT ANTI-ALIAS ---
    Private outputAAFilter As BiQuadFilter  ' LP ~20kHz in uscita

    ' --- BUFFER ---
    Private delayBuffer As Single()
    Private delayPos As Integer = 0
    Private reverbBuffer As Single()
    Private reverbPos As Integer = 0
    Private chorusBuffer As Single()
    Private chorusPos As Integer = 0
    Private maxDelaySamples As Integer = 384000 ' Abbastanza per 192kHz

    ' --- RECORDER ---
    Private recorder As WaveFileWriter
    Private isRecording As Boolean = False
    Private recLock As New Object

    ' --- PARAMETRI ---
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
    Public Property CompThreshold As Single = 0.5F ' 0.0 - 1.0
    Public Property CompRatio As Single = 4.0F     ' 1.0 - 10.0

    Public Property ChorusEnabled As Boolean = False
    Public Property ChorusRate As Single = 1.0F    ' 0.1 - 5.0 Hz
    Public Property ChorusDepth As Single = 0.5F   ' 0.0 - 1.0

    Public Property DelayEnabled As Boolean = False
    Public Property DelayTimeMs As Single = 350.0F ' 50 - 1000 ms
    Public Property DelayFeedback As Single = 0.45F' 0.0 - 0.95
    Public Property DelayMix As Single = 0.40F     ' 0.0 - 1.0

    Public Property TremoloEnabled As Boolean = False
    Public Property TremoloRate As Single = 5.0F   ' 1.0 - 15.0 Hz
    Public Property TremoloDepth As Single = 0.4F  ' 0.0 - 1.0

    Public Property ReverbEnabled As Boolean = False
    Public Property ReverbMix As Single = 0.35F    ' 0.0 - 1.0
    Public Property ReverbDecay As Single = 0.75F  ' 0.0 - 0.95

    ' VARIABILI STATO
    Private tremoloPhase As Single = 0.0F
    Private chorusPhase As Single = 0.0F

    ' --- VARIABILI STATO PER DSP AVANZATO ---
    Private gateGain As Single = 0.0F       ' Per Soft Gate
    Private compEnvelope As Single = 0.0F   ' Per Compressore
    Private tapeFilterSample As Single = 0.0F ' Per Tape Delay (Low Pass)
    Private reverbDampSample As Single = 0.0F ' Per Reverb HF Damping

    Public Sub New(sourceProvider As ISampleProvider)
        source = sourceProvider
        delayBuffer = New Single(maxDelaySamples) {}
        reverbBuffer = New Single(maxDelaySamples) {}
        chorusBuffer = New Single(maxDelaySamples) {}
        UpdateFilters()
    End Sub

    ' --- CORREZIONE QUI SOTTO (Espanso su più righe) ---
    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return source.WaveFormat
        End Get
    End Property

    Public Sub UpdateFilters()
        If source Is Nothing OrElse source.WaveFormat Is Nothing Then Return
        Dim rate As Single = CSng(source.WaveFormat.SampleRate)
        myFilters = New BiQuadFilter() {
            BiQuadFilter.PeakingEQ(rate, 100.0F, 0.8F, BassGain),
            BiQuadFilter.PeakingEQ(rate, 1000.0F, 0.8F, MidGain),
            BiQuadFilter.PeakingEQ(rate, 5000.0F, 0.8F, TrebleGain)
        }
        
        ' === CAB SIM AVANZATA (Profilo realistico 4x12 chiuso) ===
        ' LP principale: taglio speaker a ~4.2kHz (Butterworth 24dB/oct = 2 stadi)
        cabSimFilter1 = BiQuadFilter.LowPassFilter(rate, 4200.0F, 0.707F)
        cabSimFilter2 = BiQuadFilter.LowPassFilter(rate, 4200.0F, 0.707F)
        ' HP: rimozione sub-bass sotto i 70Hz (accoppiamento del cabinet)
        cabSimHPF = BiQuadFilter.HighPassFilter(rate, 70.0F, 0.707F)
        ' Risonanza speaker: boost leggero a ~90Hz (corpo del cabinet)
        cabResonance = BiQuadFilter.PeakingEQ(rate, 90.0F, 1.2F, 3.0F)
        ' Presence Dip: taglio a ~3.5kHz (caratteristico dei coni da chitarra)
        cabPresenceDip = BiQuadFilter.PeakingEQ(rate, 3500.0F, 1.5F, -4.0F)
        ' Rolloff finale morbido a ~5.5kHz (simula la caduta graduale del cono)
        cabRolloff = BiQuadFilter.LowPassFilter(rate, 5500.0F, 0.5F)

        ' === OVERSAMPLING FILTERS (per Drive) ===
        ' Anti-imaging/anti-aliasing a Nyquist/2
        Dim oversampleCutoff = rate * 0.45F ' Poco sotto Nyquist originale
        oversampleUp = BiQuadFilter.LowPassFilter(rate * 2.0F, oversampleCutoff, 0.707F)
        oversampleDown = BiQuadFilter.LowPassFilter(rate * 2.0F, oversampleCutoff, 0.707F)

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

        ' --- SNAPSHOT thread-safe dei flag (letti una volta sola per buffer) ---
        ' Evita che il thread UI cambi un flag a metà elaborazione del buffer
        Dim snapGate = EnableGate
        Dim snapComp = CompressorEnabled
        Dim snapDrive = Drive
        Dim snapCabSim = EnableCabSim
        Dim snapChorus = ChorusEnabled
        Dim snapTremolo = TremoloEnabled
        Dim snapDelay = DelayEnabled
        Dim snapReverb = ReverbEnabled
        Dim snapVolume = Volume

        ' --- Coefficienti DSP Dipendenti dal Sample Rate ---
        Dim gateAttackCoeff As Single = CSng(1.0 - Math.Exp(-1.0 / (rate * 0.002)))
        Dim gateReleaseCoeff As Single = CSng(1.0 - Math.Exp(-1.0 / (rate * 0.050)))
        Dim compAttackCoeff As Single = CSng(1.0 - Math.Exp(-1.0 / (rate * 0.005)))
        Dim compReleaseCoeff As Single = CSng(1.0 - Math.Exp(-1.0 / (rate * 0.050)))
        Dim tapeLPFAlpha As Single = CSng(1.0 - Math.Exp(-2.0 * Math.PI * 3500.0 / rate))

        For i As Integer = 0 To samplesRead - 1
            Dim sample = buffer(offset + i)
            Dim absSample = Math.Abs(sample)

            ' 1. SOFT NOISE GATE (Rate-Independent)
            Dim targetGate As Single = If(snapGate AndAlso absSample < GateThreshold, 0.0F, 1.0F)
            Dim gateCoeff As Single = If(targetGate < gateGain, gateReleaseCoeff, gateAttackCoeff)
            gateGain += gateCoeff * (targetGate - gateGain)

            sample *= gateGain
            GateActive = (gateGain < 0.1F) ' LED si accende solo se quasi chiuso

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
                ' Makeup automatico
                sample *= 1.0F + (CompThreshold * 0.5F)
            End If

            ' 3. ASYMMETRIC TUBE DRIVE con 2x Oversampling
            If snapDrive > 0 Then
                ' Boost in ingresso
                sample *= (1.0F + (snapDrive * 6.0F))

                ' Saturazione Asimmetrica (Armoniche Pari)
                Dim tubeBias As Single = 0.25F * (snapDrive / 10.0F)

                ' --- 2x OVERSAMPLING: processa a doppio rate per ridurre aliasing ---
                ' Upsample: inseriamo uno zero tra ogni campione (sample, 0)
                ' Campione 1: il segnale originale
                Dim up1 = oversampleUp.Transform(sample)
                Dim sat1 = CSng(Math.Tanh(up1 + tubeBias)) - CSng(Math.Tanh(tubeBias))
                ' Campione 2: lo zero inserito (interpolato dal filtro)
                Dim up2 = oversampleUp.Transform(0.0F)
                Dim sat2 = CSng(Math.Tanh(up2 + tubeBias)) - CSng(Math.Tanh(tubeBias))
                ' Downsample: filtriamo e prendiamo 1 campione su 2
                oversampleDown.Transform(sat1)
                sample = oversampleDown.Transform(sat2)

                ' Makeup gain per compensare lo schiacciamento
                sample *= 0.8F
            End If

            ' 4. EQ & MULTI-STAGE CAB SIM (Profilo 4x12 Realistico)
            If myFilters IsNot Nothing Then
                For Each f In myFilters : sample = f.Transform(sample) : Next
            End If
            If snapCabSim AndAlso cabSimFilter1 IsNot Nothing Then
                sample = cabSimHPF.Transform(sample)        ' HP 70Hz: rimuovi sub
                sample = cabResonance.Transform(sample)     ' Boost 90Hz: corpo cabinet
                sample = cabSimFilter1.Transform(sample)    ' LP 4.2kHz stadio 1
                sample = cabSimFilter2.Transform(sample)    ' LP 4.2kHz stadio 2
                sample = cabPresenceDip.Transform(sample)   ' Dip 3.5kHz: cono speaker
                sample = cabRolloff.Transform(sample)       ' Rolloff morbido 5.5kHz
            End If

            ' 5. CHORUS (Interpolazione Cubica per pulizia)
            If snapChorus Then
                Dim lfoHz = ChorusRate
                Dim depthSamples = (ChorusDepth * 0.006F) * rate ' Fino a 6ms modulazione
                Dim baseDelaySamples = 0.012F * rate       ' 12ms fissi
                
                chorusPhase += CSng((Math.PI * 2 * lfoHz) / rate)
                If chorusPhase > CSng(Math.PI * 2) Then chorusPhase -= CSng(Math.PI * 2)
                
                Dim currentDelay = baseDelaySamples + (CSng(Math.Sin(chorusPhase)) * depthSamples)
                Dim readPosDelay = chorusPos - currentDelay
                If readPosDelay < 0 Then readPosDelay += maxDelaySamples
                
                ' Interpolazione cubica Hermite (4 punti) — molto più pulita della lineare
                Dim idx = CInt(Math.Floor(readPosDelay))
                Dim frac As Single = readPosDelay - idx
                Dim s0 = chorusBuffer((idx - 1 + maxDelaySamples) Mod maxDelaySamples)
                Dim s1 = chorusBuffer(idx Mod maxDelaySamples)
                Dim s2 = chorusBuffer((idx + 1) Mod maxDelaySamples)
                Dim s3 = chorusBuffer((idx + 2) Mod maxDelaySamples)

                ' Hermite coefficients
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

                ' Filtra l'eco (Low Pass rate-independent) per simulare nastro analogico
                tapeFilterSample += tapeLPFAlpha * (echo - tapeFilterSample)

                sample += tapeFilterSample * DelayMix

                ' Scrive nel buffer (Segnale + Feedback Filtrato che decade)
                delayBuffer(delayPos) = sample + (tapeFilterSample * DelayFeedback)
            Else
                ' FONDAMENTALE: scrivi il segnale corrente anche se disabilitato,
                ' così quando riabiliti il delay c'è già audio nel buffer
                delayBuffer(delayPos) = sample
            End If
            delayPos = (delayPos + 1) Mod maxDelaySamples

            ' 8. REVERB (8-Tap Diffusion con damping)
            If snapReverb Then
                ' Tap primari (prime riflessioni)
                Dim r1 = reverbBuffer((reverbPos - CInt(rate * 0.0137F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r2 = reverbBuffer((reverbPos - CInt(rate * 0.0227F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r3 = reverbBuffer((reverbPos - CInt(rate * 0.0371F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r4 = reverbBuffer((reverbPos - CInt(rate * 0.0413F) + maxDelaySamples) Mod maxDelaySamples)
                ' Tap secondari (diffusione tardiva - danno corpo alla coda)
                Dim r5 = reverbBuffer((reverbPos - CInt(rate * 0.0533F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r6 = reverbBuffer((reverbPos - CInt(rate * 0.0671F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r7 = reverbBuffer((reverbPos - CInt(rate * 0.0787F) + maxDelaySamples) Mod maxDelaySamples)
                Dim r8 = reverbBuffer((reverbPos - CInt(rate * 0.0897F) + maxDelaySamples) Mod maxDelaySamples)

                Dim earlyRef = (r1 + r2 + r3 + r4) * 0.25F
                Dim lateRef = (r5 + r6 + r7 + r8) * 0.15F
                Dim revSignal = earlyRef + lateRef

                sample += revSignal * ReverbMix

                ' HF Damping sulla coda (rate-independent ~4kHz LP)
                Dim dampAlpha As Single = CSng(1.0 - Math.Exp(-2.0 * Math.PI * 4000.0 / rate))
                reverbDampSample += dampAlpha * (sample - reverbDampSample)

                reverbBuffer(reverbPos) = reverbDampSample * ReverbDecay
            Else
                ' Scrivi il segnale corrente così il reverb è pronto appena riabilitato
                reverbBuffer(reverbPos) = sample * 0.5F
            End If
            reverbPos = (reverbPos + 1) Mod maxDelaySamples

            ' MASTER
            sample *= snapVolume

            ' OUTPUT ANTI-ALIAS FILTER (LP 20kHz)
            If outputAAFilter IsNot Nothing Then
                sample = outputAAFilter.Transform(sample)
            End If

            ' MASTER LIMITER (Soft-Knee — trasparente sotto 0.9, satura solo i picchi)
            Dim absOut = Math.Abs(sample)
            If absOut > 0.9F Then
                ' Zona di soft-clip: transizione graduale da lineare a saturazione
                Dim excess = absOut - 0.9F
                Dim compressed = 0.9F + CSng(Math.Tanh(excess * 5.0F)) * 0.1F
                sample = Math.Sign(sample) * compressed
            End If

            ' Safety check Anti-NaN / Anti-Infinity Shield
            If Single.IsNaN(sample) OrElse Single.IsInfinity(sample) Then sample = 0.0F
            If sample > 1.0F Then sample = 1.0F
            If sample < -1.0F Then sample = -1.0F

            If Math.Abs(sample) > maxSample Then maxSample = Math.Abs(sample)
            buffer(offset + i) = sample

        Next

        ' RECORDING EFFICIENTE E THREAD-SAFE (Chunk writing instead of sample-by-sample)
        SyncLock recLock
            If isRecording AndAlso recorder IsNot Nothing Then
                Try
                    recorder.WriteSamples(buffer, offset, samplesRead)
                Catch ex As Exception
                    ' Ignora errori di disposal concorrente (durante lo stop)
                End Try
            End If
        End SyncLock

        CurrentPeak = maxSample
        Return samplesRead
    End Function
End Class