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

    ' --- 2x OVERSAMPLING PER DRIVE (filtri separati per ogni sub-sample) ---
    Private oversampleUpF1_A As BiQuadFilter   ' Anti-imaging stage 1, sub-sample A
    Private oversampleUpF2_A As BiQuadFilter   ' Anti-imaging stage 2, sub-sample A
    Private oversampleUpF1_B As BiQuadFilter   ' Anti-imaging stage 1, sub-sample B (zero-stuffed)
    Private oversampleUpF2_B As BiQuadFilter   ' Anti-imaging stage 2, sub-sample B (zero-stuffed)
    Private oversampleDownF1 As BiQuadFilter   ' Anti-aliasing stage 1
    Private oversampleDownF2 As BiQuadFilter   ' Anti-aliasing stage 2

    ' --- PRE-DRIVE LOW-SHELF (attenua bass prima della distorsione senza tagliare le fondamentali) ---
    Private preDriveLowShelf As BiQuadFilter
    ' --- POST-DRIVE PRESENCE (ripristina articolazione dopo la saturazione) ---
    Private postDrivePresence As BiQuadFilter

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

    ' --- METRONOME ---
    Public Property MetronomeEnabled As Boolean = False
    Public Property MetronomeBPM As Single = 120.0F
    Public Property MetronomeVolume As Single = 0.5F

    ' --- LOOPER ---
    Public Enum LooperStates
        Stopped = 0
        Recording = 1
        Playing = 2
    End Enum
    Public Property LooperState As LooperStates = LooperStates.Stopped
    Public Property LooperVolume As Single = 0.5F

    Private Const MAX_LOOPER_SAMPLES As Integer = 192000 * 120 ' 120 seconds max
    Private looperBuffer() As Single
    Public looperLength As Integer = 0
    Public currentLooperPos As Integer = 0

    Public ReadOnly Property LooperProgress As Single
        Get
            If looperLength <= 0 Then Return 0.0F
            Return CSng(currentLooperPos) / CSng(looperLength)
        End Get
    End Property

    ' --- SIGNAL CHAIN ---
    Public Enum FXType
        Compressor = 0
        Drive = 1
        AmpCab = 2
        Chorus = 3
        Tremolo = 4
        Delay = 5
        Reverb = 6
    End Enum

    Public SignalChain() As FXType = {
        FXType.Compressor,
        FXType.Drive,
        FXType.AmpCab,
        FXType.Chorus,
        FXType.Tremolo,
        FXType.Delay,
        FXType.Reverb
    }

    ' VARIABILI STATO
    Private tremoloPhase As Single = 0.0F
    Private chorusPhase As Single = 0.0F

    ' --- VARIABILI STATO PER DSP AVANZATO ---
    Private gateGain As Single = 0.0F
    Private compEnvelope As Single = 0.0F
    Private tapeFilterSample As Single = 0.0F
    Private reverbDampSample As Single = 0.0F

    ' --- METRONOME STATE ---
    Private metronomeSampleCounter As Integer = 0
    Private metronomeBeatCount As Integer = 0
    Private metronomePhase As Single = 0.0F
    Private metronomeEnv As Single = 0.0F

    ' --- GATE SILENCE TRACKING ---
    ' Quando il gate è chiuso per più di GATE_FLUSH_SAMPLES, resetta tutti i filtri
    ' per prevenire l'accumulo di errori numerici float32 nei registri z1/z2 dei BiQuad
    ' (a 192kHz i coefficienti sono vicini a 1.0 → drift numerico → rumore fantasma)
    Private gateClosedCounter As Integer = 0
    Private Const GATE_FLUSH_SAMPLES As Integer = 19200  ' 100ms @ 192kHz

    ' --- FADE-IN ANTI-POP ---
    Private fadeInCounter As Integer = 0
    Private Const FADE_IN_SAMPLES As Integer = 4096  ' ~21ms @ 192kHz

    Public Sub New(sourceProvider As ISampleProvider)
        source = sourceProvider
        delayBuffer = New Single(maxDelaySamples) {}
        reverbBuffer = New Single(maxDelaySamples) {}
        chorusBuffer = New Single(maxDelaySamples - 1) {}
        ReDim looperBuffer(MAX_LOOPER_SAMPLES - 1)
        
        ' Azzera buffer e state DSP per evitare pop all'avvio
        Array.Clear(delayBuffer, 0, delayBuffer.Length)
        Array.Clear(reverbBuffer, 0, reverbBuffer.Length)
        Array.Clear(chorusBuffer, 0, chorusBuffer.Length)
        gateGain = 0.0F
        compEnvelope = 0.0F
        tapeFilterSample = 0.0F
        reverbDampSample = 0.0F
        gateClosedCounter = 0
        fadeInCounter = 0
        metronomeSampleCounter = 0
        metronomeBeatCount = 0
        MetronomeBPM = 120.0F
        MetronomeVolume = 0.5F
        
        ClearLooper()
        UpdateFilters()
    End Sub

    Public Sub ClearLooper()
        looperLength = 0
        currentLooperPos = 0
        LooperState = LooperStates.Stopped
        If looperBuffer IsNot Nothing Then
            Array.Clear(looperBuffer, 0, looperBuffer.Length)
        End If
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

        ' === PRE-DRIVE FILTERS ===
        ' Low-shelf a 200Hz con -4dB: ATTENUA (non taglia) le basse prima della saturazione.
        ' Riduce l'intermodulazione nei power chord ma preserva le fondamentali delle note aperte
        ' (E2=82Hz, A2=110Hz non vengono eliminate, solo attenuate di ~4dB)
        preDriveLowShelf = BiQuadFilter.LowShelf(rate, 200.0F, 0.707F, -4.0F)
        ' Post-drive presence: ripristina l'attacco e l'articolazione persi dalla tanh()
        postDrivePresence = BiQuadFilter.PeakingEQ(rate, 2500.0F, 1.2F, 2.0F)

        ' === 2x OVERSAMPLING FILTERS (filtri separati per sub-sample A e B) ===
        ' CRITICO: ogni sub-sample deve avere i propri filtri per non corrompere lo stato z1/z2
        Dim osCutoff = rate * 0.45F
        Dim osRate = rate * 2.0F
        oversampleUpF1_A = BiQuadFilter.LowPassFilter(osRate, osCutoff, 0.707F)
        oversampleUpF2_A = BiQuadFilter.LowPassFilter(osRate, osCutoff, 0.707F)
        oversampleUpF1_B = BiQuadFilter.LowPassFilter(osRate, osCutoff, 0.707F)
        oversampleUpF2_B = BiQuadFilter.LowPassFilter(osRate, osCutoff, 0.707F)
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

    ''' <summary>
    ''' Flush denormali: i numeri subnormali float32 (sotto ~1.18e-38) causano
    ''' CPU stall sui registri FPU, drift numerico nei filtri BiQuad, e
    ''' accumulo di rumore fantasma. Clampa a zero.
    ''' </summary>
    Private Shared Function FlushDenormal(v As Single) As Single
        If Math.Abs(v) < 1.0E-10F Then Return 0.0F
        Return v
    End Function

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
        Dim snapMetronomeEnabled = MetronomeEnabled
        Dim snapMetronomeBPM = MetronomeBPM
        Dim snapMetronomeVol = MetronomeVolume

        ' --- Coefficienti DSP Dipendenti dal Sample Rate ---
        Dim gateAttackCoeff As Single = CSng(1.0 - Math.Exp(-1.0 / (rate * 0.002)))
        Dim gateReleaseCoeff As Single = CSng(1.0 - Math.Exp(-1.0 / (rate * 0.050)))
        Dim compAttackCoeff As Single = CSng(1.0 - Math.Exp(-1.0 / (rate * 0.005)))
        Dim compReleaseCoeff As Single = CSng(1.0 - Math.Exp(-1.0 / (rate * 0.050)))
        Dim tapeLPFAlpha As Single = CSng(1.0 - Math.Exp(-2.0 * Math.PI * 3500.0 / rate))

        Dim snapChain(6) As FXType
        Array.Copy(SignalChain, snapChain, 7)

        For i As Integer = 0 To samplesRead - 1
            Dim sample = buffer(offset + i)
            Dim metroSample As Single = 0.0F

            ' 0. METRONOMO
            If snapMetronomeEnabled AndAlso snapMetronomeBPM > 0 Then
                Dim samplesPerBeat = rate * 60.0F / snapMetronomeBPM
                metronomeSampleCounter += 1
                If metronomeSampleCounter >= samplesPerBeat Then
                    metronomeSampleCounter -= CInt(samplesPerBeat)
                    metronomeBeatCount = (metronomeBeatCount + 1) Mod 4
                    metronomeEnv = 1.0F
                    metronomePhase = 0.0F
                End If

                If metronomeEnv > 0 Then
                    Dim freq As Single = If(metronomeBeatCount = 1, 1500.0F, 800.0F) ' Beat 1 has a higher pitch
                    metronomePhase += CSng(freq * Math.PI * 2 / rate)
                    If metronomePhase > CSng(Math.PI * 2) Then metronomePhase -= CSng(Math.PI * 2)
                    
                    metroSample = CSng(Math.Sin(metronomePhase)) * metronomeEnv * snapMetronomeVol * 0.7F

                    metronomeEnv *= CSng(Math.Exp(-1.0 / (rate * 0.015))) ' 15ms decay const
                    If metronomeEnv < 0.001F Then metronomeEnv = 0.0F
                End If
            End If

            ' 0. INPUT CONDITIONING: HPF 30Hz (rimuove DC offset)
            If inputHPF IsNot Nothing Then
                sample = inputHPF.Transform(sample)
            End If

            ' 1. NOISE GATE — applicato PRIMA dell'InputGain
            Dim absSampleRaw = Math.Abs(sample)
            Dim targetGate As Single = If(snapGate AndAlso absSampleRaw < GateThreshold, 0.0F, 1.0F)
            Dim gateCoeff As Single = If(targetGate < gateGain, gateReleaseCoeff, gateAttackCoeff)
            gateGain += gateCoeff * (targetGate - gateGain)
            GateActive = (gateGain < 0.1F)

            ' === HARD GATE + DSP BYPASS ===
            If gateGain < 0.001F Then
                gateClosedCounter += 1

                ' Dopo 100ms di silenzio: resetta TUTTI gli stati interni dei filtri
                If gateClosedCounter = GATE_FLUSH_SAMPLES Then
                    UpdateFilters()
                    Array.Clear(delayBuffer, 0, delayBuffer.Length)
                    Array.Clear(reverbBuffer, 0, reverbBuffer.Length)
                    Array.Clear(chorusBuffer, 0, chorusBuffer.Length)
                    tapeFilterSample = 0.0F
                    reverbDampSample = 0.0F
                    compEnvelope = 0.0F
                End If

                ' L'output chitarra è muto. Passiamo direttamente ai mix esterni.
                sample = 0.0F
                GoTo MixExternal
            Else
                gateClosedCounter = 0
            End If

            ' Da qui in poi il segnale è "vivo" (gate aperto)
            sample *= gateGain

            ' 2. PRE-AMP GAIN
            sample *= snapInputGain

            ' FADE-IN ANTI-POP
            If fadeInCounter < FADE_IN_SAMPLES Then
                sample *= CSng(fadeInCounter) / CSng(FADE_IN_SAMPLES)
                fadeInCounter += 1
            End If

            ' === DYNAMIC SIGNAL CHAIN ===
            For q As Integer = 0 To 6
                Select Case snapChain(q)
                    Case FXType.Compressor
                        ' 3. COMPRESSORE (Soft-Knee Peak Compressor)
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

                    Case FXType.Drive
                        ' 4. ASYMMETRIC TUBE DRIVE con 2x Oversampling
                        ' Reworked: gain curve compressa, filtri separati, HPF pre-drive
                        If snapDrive > 0 Then
                            ' PRE-DRIVE LOW-SHELF: attenua bass per ridurre IMD, preserva fondamentali
                            If preDriveLowShelf IsNot Nothing Then
                                sample = preDriveLowShelf.Transform(sample)
                            End If

                            ' Gain curve compressa: drive 1→x4, drive 5→x9, drive 10→x14
                            ' (prima era lineare: drive 10 = x61, troppo per tanh)
                            Dim driveGain = 1.0F + (snapDrive * 1.3F)
                            sample *= driveGain
                            Dim tubeBias As Single = 0.15F * (snapDrive / 10.0F)

                            ' 2x Oversampling con filtri SEPARATI per sub-sample A (segnale) e B (zero)
                            ' Questo previene la corruzione di stato z1/z2 che causava aliasing
                            Dim up1 = oversampleUpF2_A.Transform(oversampleUpF1_A.Transform(sample * 2.0F))
                            Dim up2 = oversampleUpF2_B.Transform(oversampleUpF1_B.Transform(0.0F))

                            ' Saturazione asimmetrica con bias ridotto
                            Dim sat1 = CSng(Math.Tanh(up1 + tubeBias)) - CSng(Math.Tanh(tubeBias))
                            Dim sat2 = CSng(Math.Tanh(up2 + tubeBias)) - CSng(Math.Tanh(tubeBias))

                            ' Decimazione: entrambi i campioni passano per i filtri down
                            Dim d1 = oversampleDownF2.Transform(oversampleDownF1.Transform(sat1))
                            oversampleDownF2.Transform(oversampleDownF1.Transform(sat2))
                            sample = d1

                            ' Makeup gain compensato
                            sample *= 0.7F + (0.3F / (1.0F + snapDrive * 0.2F))

                            ' Post-drive presence: ripristina definizione delle note
                            If postDrivePresence IsNot Nothing Then
                                sample = postDrivePresence.Transform(sample)
                            End If
                        End If

                    Case FXType.AmpCab
                        ' 5. EQ & CAB SIM + Denormal flush dopo i filtri
                        If myFilters IsNot Nothing Then
                            For Each f In myFilters : sample = f.Transform(sample) : Next
                        End If
                        sample = FlushDenormal(sample)

                        If snapCabSim AndAlso cabSimFilter1 IsNot Nothing Then
                            sample = cabSimHPF.Transform(sample)
                            sample = cabResonance.Transform(sample)
                            sample = cabSimFilter1.Transform(sample)
                            sample = cabSimFilter2.Transform(sample)
                            sample = cabPresenceDip.Transform(sample)
                            sample = cabRolloff.Transform(sample)
                            sample = FlushDenormal(sample)
                        End If

                    Case FXType.Chorus
                        ' 6. CHORUS (Interpolazione Cubica Hermite)
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

                    Case FXType.Tremolo
                        ' 7. TREMOLO
                        If snapTremolo Then
                            Dim modFactor = 1.0F - (TremoloDepth * (0.5F + (0.5F * CSng(Math.Sin(tremoloPhase)))))
                            sample *= modFactor
                            tremoloPhase += CSng((Math.PI * 2 * TremoloRate) / rate)
                            If tremoloPhase > Math.PI * 2 Then tremoloPhase -= CSng(Math.PI * 2)
                        End If

                    Case FXType.Delay
                        ' 8. TAPE DELAY (feedback corretto)
                        If snapDelay Then
                            Dim tMs = DelayTimeMs
                            If tMs < 10 Then tMs = 10
                            Dim readPos = delayPos - CInt((tMs / 1000.0F) * rate)
                            If readPos < 0 Then readPos += maxDelaySamples

                            Dim echo = delayBuffer(readPos)
                            tapeFilterSample += tapeLPFAlpha * (echo - tapeFilterSample)
                            Dim drySampleBeforeDelay = sample
                            sample += tapeFilterSample * DelayMix
                            delayBuffer(delayPos) = drySampleBeforeDelay + (tapeFilterSample * DelayFeedback)
                        Else
                            delayBuffer(delayPos) = sample
                        End If
                        delayPos = (delayPos + 1) Mod maxDelaySamples

                    Case FXType.Reverb
                        ' 9. REVERB
                        Dim drySampleBeforeReverb = sample
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
                            reverbDampSample += dampAlpha * (drySampleBeforeReverb - reverbDampSample)
                            reverbBuffer(reverbPos) = reverbDampSample * ReverbDecay
                        Else
                            reverbBuffer(reverbPos) = sample * 0.5F
                        End If
                        reverbPos = (reverbPos + 1) Mod maxDelaySamples

                End Select
            Next

            ' MASTER
            sample *= snapVolume

            ' MASTER LIMITER (Soft-Knee)
            Dim absOut = Math.Abs(sample)
            If absOut > 0.9F Then
                Dim excess = absOut - 0.9F
                Dim compressed = 0.9F + CSng(Math.Tanh(excess * 5.0F)) * 0.1F
                sample = Math.Sign(sample) * compressed
            End If

MixExternal:
            ' --- LOOPER (Post-Limiter, Independent) ---
            Dim snapLooperState = LooperState
            Dim looperOut As Single = 0.0F

            If snapLooperState = LooperStates.Recording Then
                If currentLooperPos < MAX_LOOPER_SAMPLES Then
                    looperBuffer(currentLooperPos) = sample
                    currentLooperPos += 1
                Else
                    looperLength = currentLooperPos
                    currentLooperPos = 0
                    LooperState = LooperStates.Playing
                End If
            ElseIf snapLooperState = LooperStates.Playing Then
                If looperLength > 0 AndAlso currentLooperPos < looperLength Then
                    looperOut = looperBuffer(currentLooperPos) * LooperVolume
                    currentLooperPos += 1
                    If currentLooperPos >= looperLength Then
                        currentLooperPos = 0
                    End If
                End If
            End If

            sample += looperOut

            ' --- MIX METRONOMO (100% Indipendente dal DSP Chitarra e dal Limiter) ---
            sample += metroSample

            ' Safety Anti-NaN / Anti-Infinity + Denormal Flush finale
            If Single.IsNaN(sample) OrElse Single.IsInfinity(sample) Then sample = 0.0F
            If sample > 1.0F Then sample = 1.0F
            If sample < -1.0F Then sample = -1.0F
            If Math.Abs(sample) < 1.0E-10F Then sample = 0.0F

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