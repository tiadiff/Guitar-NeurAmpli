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
    Public Property ChorusEnabled As Boolean = False
    Public Property DelayEnabled As Boolean = False
    Public Property TremoloEnabled As Boolean = False
    Public Property ReverbEnabled As Boolean = False

    ' VARIABILI STATO
    Private tremoloPhase As Single = 0.0F
    Private chorusPhase As Single = 0.0F

    ' --- VARIABILI STATO PER DSP AVANZATO ---
    Private gateGain As Single = 0.0F       ' Per Soft Gate
    Private compEnvelope As Single = 0.0F   ' Per Compressore
    Private tapeFilterSample As Single = 0.0F ' Per Tape Delay (Low Pass)

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
        
        ' 4x12 Cab Impulse Response Approssimato (24dB/oct LP + 80Hz HP)
        cabSimFilter1 = BiQuadFilter.LowPassFilter(rate, 4500.0F, 0.707F)
        cabSimFilter2 = BiQuadFilter.LowPassFilter(rate, 4500.0F, 0.707F)
        cabSimHPF = BiQuadFilter.HighPassFilter(rate, 80.0F, 0.707F)
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

        For i As Integer = 0 To samplesRead - 1
            Dim sample = buffer(offset + i)
            Dim absSample = Math.Abs(sample)

            ' 1. SOFT NOISE GATE (Niente più click)
            ' Se sopra soglia target=1 (aperto), se sotto target=0 (chiuso)
            Dim targetGate As Single = If(EnableGate AndAlso absSample < GateThreshold, 0.0F, 1.0F)
            ' Interpolazione lenta verso il target (Release lento, Attack veloce)
            Dim gateSpeed As Single = If(targetGate < gateGain, 0.0005F, 0.01F)
            gateGain = (gateGain * (1.0F - gateSpeed)) + (targetGate * gateSpeed)

            sample *= gateGain
            GateActive = (gateGain < 0.1F) ' LED si accende solo se quasi chiuso

            ' 2. VCA COMPRESSOR (Dinamico)
            If CompressorEnabled Then
                ' Envelope Follower (rileva il volume medio nel tempo)
                ' Attack veloce (0.01), Release lento (0.0002)
                If absSample > compEnvelope Then
                    compEnvelope = (compEnvelope * 0.99F) + (absSample * 0.01F)
                Else
                    compEnvelope = (compEnvelope * 0.9998F) + (absSample * 0.0002F)
                End If

                ' Applicazione Gain Reduction se supera soglia (0.25)
                If compEnvelope > 0.25F Then
                    Dim reduction = 0.25F / compEnvelope ' Ratio variabile
                    sample *= reduction
                End If
                sample *= 1.4F ' Makeup Gain automatico
            End If

            ' 3. ASYMMETRIC TUBE DRIVE (Calore Valvolare Reale)
            If Drive > 0 Then
                ' Boost in ingresso (Push forte in griglia)
                sample *= (1.0F + (Drive * 6.0F))

                ' Saturazione Asimmetrica (Armoniche Pari)
                ' Un offset DC costante che "storce" la sinusoide dentro il soft-clipper
                Dim tubeBias As Single = 0.25F * (Drive / 10.0F) 
                
                ' Math.Tanh agisce come la distorsione del transfer valvolare
                sample = CSng(Math.Tanh(sample + tubeBias)) - CSng(Math.Tanh(tubeBias))

                ' Makeup gain per compensare lo schiacciamento
                sample *= 0.8F
            End If

            ' 4. EQ & MULTI-STAGE CAB SIM
            If myFilters IsNot Nothing Then
                For Each f In myFilters : sample = f.Transform(sample) : Next
            End If
            If EnableCabSim AndAlso cabSimFilter1 IsNot Nothing Then
                sample = cabSimFilter1.Transform(sample)
                sample = cabSimFilter2.Transform(sample)
                sample = cabSimHPF.Transform(sample)
            End If

            ' 5. CHORUS
            If ChorusEnabled Then
                Dim lfoHz As Single = 1.0F
                Dim depthSamples As Single = 0.004F * rate ' 4ms modulation depth
                Dim baseDelaySamples As Single = 0.012F * rate ' 12ms base delay
                
                chorusPhase += (Math.PI * 2 * lfoHz) / rate
                If chorusPhase > Math.PI * 2 Then chorusPhase -= Math.PI * 2
                
                Dim currentDelay = baseDelaySamples + (CSng(Math.Sin(chorusPhase)) * depthSamples)
                Dim readPosDelay = chorusPos - currentDelay
                If readPosDelay < 0 Then readPosDelay += maxDelaySamples
                
                Dim index1 = CInt(Math.Floor(readPosDelay))
                Dim index2 = (index1 + 1) Mod maxDelaySamples
                Dim frac = readPosDelay - index1
                
                Dim chorusDelaySample = (chorusBuffer(index1) * (1.0F - frac)) + (chorusBuffer(index2) * frac)
                sample = (sample * 0.65F) + (chorusDelaySample * 0.45F) ' Mix Wet/Dry
            End If
            
            chorusBuffer(chorusPos) = sample
            chorusPos = (chorusPos + 1) Mod maxDelaySamples

            ' 6. TREMOLO (Invariato)
            If TremoloEnabled Then
                Dim modFactor = 1.0F - (0.4F * (1.0F + CSng(Math.Sin(tremoloPhase)))) ' Profondità aumentata
                sample *= modFactor
                tremoloPhase += (Math.PI * 2 * 5.0F) / rate
                If tremoloPhase > Math.PI * 2 Then tremoloPhase -= Math.PI * 2
            End If

            ' 7. TAPE DELAY (Feedback filtrato)
            If DelayEnabled Then
                Dim readPos = delayPos - CInt(0.35F * rate) ' 350ms fissi per ora
                If readPos < 0 Then readPos += maxDelaySamples

                Dim echo = delayBuffer(readPos)

                ' Filtra l'eco (Low Pass) per simulare nastro analogico
                tapeFilterSample = (tapeFilterSample * 0.6F) + (echo * 0.4F)

                sample += tapeFilterSample * 0.4F ' Mix

                ' Scrive nel buffer (Segnale + Feedback Filtrato che decade)
                delayBuffer(delayPos) = sample + (tapeFilterSample * 0.45F)

                delayPos = (delayPos + 1) Mod maxDelaySamples
            Else
                delayBuffer(delayPos) = 0
                delayPos = (delayPos + 1) Mod maxDelaySamples
            End If

            ' 8. REVERB (4-Tap Diffusion - Più denso)
            If ReverbEnabled Then
                ' Leggiamo da 4 punti distanti numeri primi per evitare risonanze
                Dim r1 = reverbBuffer((reverbPos - 1103 + maxDelaySamples) Mod maxDelaySamples)
                Dim r2 = reverbBuffer((reverbPos - 2377 + maxDelaySamples) Mod maxDelaySamples)
                Dim r3 = reverbBuffer((reverbPos - 3559 + maxDelaySamples) Mod maxDelaySamples)
                Dim r4 = reverbBuffer((reverbPos - 4931 + maxDelaySamples) Mod maxDelaySamples)

                Dim revSignal = (r1 + r2 + r3 + r4) * 0.25F ' Media
                sample += revSignal * 0.35F ' Mix Wet

                ' Feedback
                reverbBuffer(reverbPos) = sample * 0.75F
                reverbPos = (reverbPos + 1) Mod maxDelaySamples
            Else
                reverbBuffer(reverbPos) = 0
                reverbPos = (reverbPos + 1) Mod maxDelaySamples
            End If

            ' MASTER
            sample *= Volume

            ' Safety Limiter e Anti-NaN / Anti-Infinity Shield
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