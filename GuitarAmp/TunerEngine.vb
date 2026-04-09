Imports System.Math

''' <summary>
''' Motore di rilevazione pitch basato sull'algoritmo YIN (autocorrelazione).
''' Superiore alla FFT pura per strumenti monofonici come la chitarra elettrica.
''' Precisione sub-Hz anche alle basse frequenze (E2 = 82 Hz).
''' </summary>
Public Class TunerEngine

    ' === NOTE NAMES ===
    Private Shared ReadOnly NoteNames() As String = {"C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"}

    ' === STRING INFO ===
    Public Class StringInfo
        Public ReadOnly NoteName As String
        Public ReadOnly MidiNote As Integer
        Public ReadOnly Octave As Integer

        Public Sub New(name As String, midi As Integer, oct As Integer)
            NoteName = name
            MidiNote = midi
            Octave = oct
        End Sub

        Public Function GetFrequency(a4Ref As Single) As Single
            Return CSng(a4Ref * Pow(2.0, (MidiNote - 69) / 12.0))
        End Function

        Public Overrides Function ToString() As String
            Return NoteName & Octave
        End Function
    End Class

    ' === TUNING PRESET ===
    Public Class TuningPreset
        Public ReadOnly Name As String
        Public ReadOnly Strings() As StringInfo ' 0=6th(bassa) → 5=1st(alta)

        Public Sub New(n As String, ParamArray s() As StringInfo)
            Name = n
            Strings = s
        End Sub

        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class

    ' === ACCORDATURE DISPONIBILI ===
    Public Shared ReadOnly Tunings As TuningPreset() = {
        New TuningPreset("Standard (EADGBE)",
            New StringInfo("E", 40, 2), New StringInfo("A", 45, 2), New StringInfo("D", 50, 3),
            New StringInfo("G", 55, 3), New StringInfo("B", 59, 3), New StringInfo("E", 64, 4)),
        New TuningPreset("Drop D",
            New StringInfo("D", 38, 2), New StringInfo("A", 45, 2), New StringInfo("D", 50, 3),
            New StringInfo("G", 55, 3), New StringInfo("B", 59, 3), New StringInfo("E", 64, 4)),
        New TuningPreset("Drop C",
            New StringInfo("C", 36, 2), New StringInfo("G", 43, 2), New StringInfo("C", 48, 3),
            New StringInfo("F", 53, 3), New StringInfo("A", 57, 3), New StringInfo("D", 62, 4)),
        New TuningPreset("D Standard",
            New StringInfo("D", 38, 2), New StringInfo("G", 43, 2), New StringInfo("C", 48, 3),
            New StringInfo("F", 53, 3), New StringInfo("A", 57, 3), New StringInfo("D", 62, 4)),
        New TuningPreset("Eb Standard",
            New StringInfo("Eb", 39, 2), New StringInfo("Ab", 44, 2), New StringInfo("Db", 49, 3),
            New StringInfo("Gb", 54, 3), New StringInfo("Bb", 58, 3), New StringInfo("Eb", 63, 4)),
        New TuningPreset("DADGAD",
            New StringInfo("D", 38, 2), New StringInfo("A", 45, 2), New StringInfo("D", 50, 3),
            New StringInfo("G", 55, 3), New StringInfo("A", 57, 3), New StringInfo("D", 62, 4)),
        New TuningPreset("Open G (DGDGBD)",
            New StringInfo("D", 38, 2), New StringInfo("G", 43, 2), New StringInfo("D", 50, 3),
            New StringInfo("G", 55, 3), New StringInfo("B", 59, 3), New StringInfo("D", 62, 4)),
        New TuningPreset("Open D (DADF#AD)",
            New StringInfo("D", 38, 2), New StringInfo("A", 45, 2), New StringInfo("D", 50, 3),
            New StringInfo("F#", 54, 3), New StringInfo("A", 57, 3), New StringInfo("D", 62, 4))
    }

    ' === RISULTATI RILEVAZIONE (letti dal thread UI) ===
    Public DetectedFrequency As Single = 0
    Public DetectedNoteName As String = ""
    Public DetectedOctave As Integer = 0
    Public DetectedCents As Single = 0
    Public ClosestStringIndex As Integer = -1
    Public SignalLevel As Single = 0
    Public HasPitch As Boolean = False

    ' === IMPOSTAZIONI ===
    Public A4Reference As Single = 440.0F
    Public CurrentTuningIndex As Integer = 0

    ' === COSTANTI YIN ===
    Private Const YIN_THRESHOLD As Single = 0.15F ' Era 0.20F
    Private Const MIN_FREQ As Single = 55.0F   
    Private Const MAX_FREQ As Single = 500.0F  
    Private Const SIGNAL_THRESHOLD As Single = 0.008F

    ' === STABILITÀ ===
    Private Const HISTORY_SIZE As Integer = 5
    Private freqHistory(HISTORY_SIZE - 1) As Single
    Private historyIdx As Integer = 0
    Private historyCount As Integer = 0
     Private framesSincePluck As Integer = 0
    Private lastStableFreq As Single = 0
    Public Confidence As Single = 0  ' 0..1 (quanto è affidabile il rilevamento)

    ''' <summary>
    ''' Analizza un buffer di campioni audio e rileva il pitch fondamentale.
    ''' Usa filtro mediano corto e octave-jump protection.
    ''' </summary>
   Public Sub AnalyzeBuffer(samples() As Single, count As Integer, sampleRate As Integer)
        If count < 512 Then Return

        ' --- 1. Livello segnale RMS ---
        Dim sumSq As Double = 0
        For i = 0 To count - 1
            sumSq += CDbl(samples(i)) * samples(i)
        Next
        SignalLevel = CSng(Sqrt(sumSq / count))

        If SignalLevel < SIGNAL_THRESHOLD Then
            HasPitch = False
            historyCount = 0
            lastStableFreq = 0
            Return
        End If

        ' --- 2. PRE-FILTERING AGGRESSIVO (Il segreto per la precisione) ---
        Dim filtered(count - 1) As Single
        
        ' A) DC Blocker (Rimuove lo scostamento dallo zero)
        Dim prevX As Single = samples(0)
        Dim prevY As Single = samples(0)
        filtered(0) = prevY
        For i = 1 To count - 1
            Dim x = samples(i)
            prevY = x - prevX + 0.995F * prevY
            prevX = x
            filtered(i) = prevY
        Next

        ' B) Filtro a Media Mobile (Multi-pass)
        ' Funziona come un potente filtro passa-basso che uccide il "twang" del plettro 
        ' e le armoniche che causano l'instabilità dell'ago.
        Dim windowSize As Integer = 5
        For pass = 1 To 2 ' Due passaggi per smussare l'onda
            Dim temp(count - 1) As Single
            For i = 0 To count - 1
                Dim sum As Single = 0
                Dim wCount As Integer = 0
                For w = -windowSize To windowSize
                    Dim idx = i + w
                    If idx >= 0 AndAlso idx < count Then
                        sum += filtered(idx)
                        wCount += 1
                    End If
                Next
                temp(i) = sum / wCount
            Next
            Array.Copy(temp, filtered, count)
        Next

        ' --- 3. YIN Pitch Detection ---
        Dim yinConfidence As Single = 0
        Dim freq = YinDetect(filtered, count, sampleRate, yinConfidence)
        
        ' Rifiuta i rilevamenti con bassa confidenza (rumore)
        If freq < 0 OrElse yinConfidence < 0.6F Then
            Return
        End If

        ' --- 4. Octave-Jump Protection (Previene i salti improvvisi di nota) ---
        If lastStableFreq > 0 Then
            Dim ratio = freq / lastStableFreq
            ' Se l'algoritmo rileva un'ottava sopra (es. 164Hz invece di 82Hz), la dimezziamo
            If ratio > 1.85F AndAlso ratio < 2.15F Then
                freq /= 2.0F
            ' Se rileva un'ottava sotto (raro, ma possibile), la raddoppiamo
            ElseIf ratio > 0.45F AndAlso ratio < 0.55F Then
                freq *= 2.0F
            End If
        End If

        ' --- 5. Filtro Stabilità (Outlier Rejection) ---
        freqHistory(historyIdx) = freq
        historyIdx = (historyIdx + 1) Mod HISTORY_SIZE
        If historyCount < HISTORY_SIZE Then historyCount += 1

        ' Aspettiamo di avere almeno 3 campioni validi
        If historyCount >= 3 Then
            Dim recentCount = Min(historyCount, 5) ' Analizza fino a 5 letture precedenti
            Dim sorted(recentCount - 1) As Single
            For i = 0 To recentCount - 1
                Dim idx = (historyIdx - 1 - i + HISTORY_SIZE) Mod HISTORY_SIZE
                sorted(i) = freqHistory(idx)
            Next
            Array.Sort(sorted)
            
            ' Se abbiamo 5 campioni, ignoriamo il più alto e il più basso (potenziali errori)
            ' e facciamo la media di quelli centrali. Questo elimina i "tremolii".
            If recentCount >= 5 Then
                freq = (sorted(1) + sorted(2) + sorted(3)) / 3.0F
            Else
                freq = sorted(recentCount \ 2) ' Mediana classica
            End If

            ' Controllo di dispersione
            Dim spread = (sorted(recentCount - 1) - sorted(0)) / freq
            If spread > 0.05F Then Return ' Se le letture sono troppo distanti tra loro, ignora
        Else
            Return
        End If

        lastStableFreq = freq

        ' --- 6. Calcolo Cents e Corde ---
        Dim midiFloat = 69.0 + 12.0 * Log(CDbl(freq) / A4Reference, 2.0)
        Dim midiNote = CInt(Round(midiFloat))
        Dim noteIdx = ((midiNote Mod 12) + 12) Mod 12

        Dim tuning = Tunings(CurrentTuningIndex)
        Dim bestIdx = ClosestStringIndex
        
        If bestIdx < 0 OrElse bestIdx >= tuning.Strings.Length Then bestIdx = 0
        Dim currentTargetFreq = tuning.Strings(bestIdx).GetFrequency(A4Reference)
        Dim currentCents = CSng(1200.0 * Log(CDbl(freq) / currentTargetFreq, 2.0))

        ' Cerca una corda migliore solo se ci allontaniamo di oltre 100 cents (evita che salti corda per sbaglio)
        If ClosestStringIndex < 0 OrElse Abs(currentCents) > 100.0F Then
            Dim searchBestIdx = 0
            Dim searchBestDist As Single = Single.MaxValue
            Dim searchBestCents As Single = 0
            For si = 0 To tuning.Strings.Length - 1
                Dim tf = tuning.Strings(si).GetFrequency(A4Reference)
                If tf <= 0 Then Continue For
                Dim c = CSng(1200.0 * Log(CDbl(freq) / tf, 2.0))
                If Abs(c) < searchBestDist Then
                    searchBestDist = Abs(c)
                    searchBestIdx = si
                    searchBestCents = c
                End If
            Next
            bestIdx = searchBestIdx
            currentCents = searchBestCents
        End If

        DetectedFrequency = freq
        DetectedNoteName = NoteNames(noteIdx)
        DetectedOctave = (midiNote \ 12) - 1
        DetectedCents = currentCents
        ClosestStringIndex = bestIdx
        HasPitch = True
    End Sub

    ''' <summary>
    ''' Algoritmo YIN — Autocorrelazione con CMNDF e interpolazione parabolica.
    ''' Restituisce la frequenza fondamentale o -1 se non rilevata.
    ''' </summary>
   Private Function YinDetect(samples() As Single, count As Integer, sampleRate As Integer, ByRef confidence As Single) As Single
        confidence = 0
        Dim halfLen = count \ 2
        Dim minLag = Max(2, CInt(sampleRate / MAX_FREQ))
        Dim maxLag = Min(halfLen - 1, CInt(sampleRate / MIN_FREQ))
        If maxLag <= minLag OrElse maxLag >= halfLen Then Return -1

        ' CMNDF
        Dim cmndf(maxLag) As Single
        Dim runningSum As Single = 0
        cmndf(0) = 1.0F

        For tau = 1 To maxLag
            Dim diff As Single = 0
            For j = 0 To halfLen - 1
                Dim d = samples(j) - samples(j + tau)
                diff += d * d
            Next
            cmndf(tau) = diff
            runningSum += diff
            ' Evita divisioni per zero aggiungendo un minuscolo epsilon
            cmndf(tau) = cmndf(tau) * tau / (runningSum + 0.000001F)
        Next

        ' Ricerca del primo minimo locale sotto la soglia
        Dim bestTau = -1
        Dim currentThreshold As Single = 0.15F ' Soglia stretta iniziale

        ' Facciamo fino a 2 tentativi allargando la soglia, invece di prendere il minimo globale
        For attempt = 1 To 2
            For tau = minLag To maxLag - 1
                If cmndf(tau) < currentThreshold Then
                    ' Cerca il fondo esatto della "valle"
                    While tau + 1 < maxLag AndAlso cmndf(tau + 1) < cmndf(tau)
                        tau += 1
                    End While
                    bestTau = tau
                    Exit For
                End If
            Next
            
            If bestTau <> -1 Then Exit For
            currentThreshold = 0.3F ' Secondo tentativo più tollerante se il segnale è sporco
        Next

        ' Se non troviamo nulla, RIFIUTIAMO il frame. Niente minimo globale!
        If bestTau = -1 Then Return -1

        confidence = CSng(Max(0, 1.0 - cmndf(bestTau)))

        ' Interpolazione parabolica per precisione al millesimo di Hertz
        Dim s0 = If(bestTau > 0, cmndf(bestTau - 1), cmndf(bestTau))
        Dim s1 = cmndf(bestTau)
        Dim s2 = If(bestTau < maxLag, cmndf(bestTau + 1), cmndf(bestTau))
        
        Dim denom = 2.0F * (s0 - 2.0F * s1 + s2)
        Dim refinedTau As Single = bestTau
        If Abs(denom) > 0.0001F Then
            refinedTau = bestTau + (s0 - s2) / denom
        End If

        If refinedTau <= 0 Then Return -1
        Dim detectedFreq = CSng(sampleRate) / refinedTau

        If detectedFreq < MIN_FREQ OrElse detectedFreq > MAX_FREQ Then Return -1
        Return detectedFreq
    End Function

End Class
