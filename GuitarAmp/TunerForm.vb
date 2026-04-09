Imports NAudio.Wave
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Math

''' <summary>
''' Form del tuner cromatico per chitarra con gauge visivo ad arco,
''' indicatori per le 6 corde, e supporto per accordature multiple.
''' </summary>
Public Class TunerForm
    Inherits Form

    ' === AUDIO ===
    Private audioInput As WaveInEvent
    Private tunerEngine As New TunerEngine()
    Private isCapturing As Boolean = False

    ' === CONTROLS ===
    Private cmbTuning As ComboBox
    Private cmbInput As ComboBox
    Private lblA4 As Label
    Private btnA4Down As ModernButton
    Private btnA4Up As ModernButton
    Private btnClose As ModernButton
    Private tmrUpdate As Timer

    ' === DISPLAY STATE (smoothed per animazione) ===
    Private displayCentsVelocity As Single = 0 ' Per la fisica dell'ago
    Private displayCents As Single = 0
    Private displayFreq As Single = 0
    Private displayStringIdx As Integer = -1
    Private displayNoteName As String = ""
    Private displayOctave As Integer = 0
    Private displaySignalLevel As Single = 0
    Private hasSignal As Boolean = False
    Private isInTune As Boolean = False
    Private glowPhase As Single = 0
    Private pitchHoldCounter As Integer = 0
    Private Const PITCH_HOLD_FRAMES As Integer = 120 ' ~2 secondi a 30fps

    ' === LAYOUT CONSTANTS ===
    Private Const FORM_W As Integer = 560
    Private Const FORM_H As Integer = 500
    Private Const TITLE_H As Integer = 48
    Private Const GAUGE_CX As Integer = 280
    Private Const GAUGE_CY As Integer = 200
    Private Const GAUGE_R As Integer = 130
    Private Const STRINGS_Y As Integer = 355
    Private Const STRINGS_H As Integer = 82
    Private Const BOTTOM_Y As Integer = 450

    ' === DRAG SUPPORT ===
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HT_CAPTION As Integer = &H2

    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, Msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function

    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function

    ' ================================================================
    ' CONSTRUCTOR
    ' ================================================================
    Public Sub New(Optional selectedInputIndex As Integer = 0)
        Me.FormBorderStyle = FormBorderStyle.None
        Me.Size = New Size(FORM_W, FORM_H)
        Me.BackColor = ThemeColors.BgDeep
        Me.StartPosition = FormStartPosition.CenterParent
        Me.DoubleBuffered = True
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
        Me.TopMost = True
        Me.ShowInTaskbar = True
        Me.Text = "NeurAmpli Tuner"

        CreateControls(selectedInputIndex)
        SetupTimer()
    End Sub

    ' ================================================================
    ' CONTROLS SETUP (Programmatico — no Designer)
    ' ================================================================
    Private Sub CreateControls(selectedIdx As Integer)
        ' --- Tuning Preset Combo ---
        cmbTuning = New ComboBox()
        cmbTuning.BackColor = Color.FromArgb(37, 37, 41)
        cmbTuning.ForeColor = Color.FromArgb(240, 236, 229)
        cmbTuning.FlatStyle = FlatStyle.Flat
        cmbTuning.DropDownStyle = ComboBoxStyle.DropDownList
        cmbTuning.Location = New Point(130, 12)
        cmbTuning.Size = New Size(210, 23)
        cmbTuning.Font = New Font("Segoe UI", 9)
        For Each t In TunerEngine.Tunings
            cmbTuning.Items.Add(t.Name)
        Next
        cmbTuning.SelectedIndex = 0
        AddHandler cmbTuning.SelectedIndexChanged, AddressOf CmbTuning_Changed
        Me.Controls.Add(cmbTuning)

        ' --- A4 Reference Controls ---
        btnA4Down = New ModernButton()
        btnA4Down.Text = "−"
        btnA4Down.Location = New Point(385, 11)
        btnA4Down.Size = New Size(26, 26)
        btnA4Down.BackColor = ThemeColors.Surface
        btnA4Down.ForeColor = ThemeColors.TextPrimary
        AddHandler btnA4Down.Click, AddressOf BtnA4Down_Click
        Me.Controls.Add(btnA4Down)

        lblA4 = New Label()
        lblA4.Text = "440 Hz"
        lblA4.Location = New Point(413, 14)
        lblA4.Size = New Size(52, 20)
        lblA4.ForeColor = ThemeColors.TextPrimary
        lblA4.BackColor = Color.Transparent
        lblA4.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        lblA4.TextAlign = ContentAlignment.MiddleCenter
        Me.Controls.Add(lblA4)

        btnA4Up = New ModernButton()
        btnA4Up.Text = "+"
        btnA4Up.Location = New Point(467, 11)
        btnA4Up.Size = New Size(26, 26)
        btnA4Up.BackColor = ThemeColors.Surface
        btnA4Up.ForeColor = ThemeColors.TextPrimary
        AddHandler btnA4Up.Click, AddressOf BtnA4Up_Click
        Me.Controls.Add(btnA4Up)

        ' --- Close Button ---
        btnClose = New ModernButton()
        btnClose.Text = "X"
        btnClose.Location = New Point(520, 10)
        btnClose.Size = New Size(28, 28)
        btnClose.BackColor = Color.FromArgb(60, 20, 25)
        btnClose.ForeColor = ThemeColors.Danger
        AddHandler btnClose.Click, Sub(s, e) Me.Close()
        Me.Controls.Add(btnClose)

        ' --- Input Device Combo ---
        cmbInput = New ComboBox()
        cmbInput.BackColor = Color.FromArgb(37, 37, 41)
        cmbInput.ForeColor = Color.FromArgb(240, 236, 229)
        cmbInput.FlatStyle = FlatStyle.Flat
        cmbInput.DropDownStyle = ComboBoxStyle.DropDownList
        cmbInput.Location = New Point(20, BOTTOM_Y + 18)
        cmbInput.Size = New Size(340, 23)
        cmbInput.Font = New Font("Segoe UI", 9)
        For i As Integer = 0 To WaveIn.DeviceCount - 1
            cmbInput.Items.Add(WaveIn.GetCapabilities(i).ProductName)
        Next
        If cmbInput.Items.Count > 0 Then
            cmbInput.SelectedIndex = Min(selectedIdx, cmbInput.Items.Count - 1)
        End If
        AddHandler cmbInput.SelectedIndexChanged, AddressOf CmbInput_Changed
        Me.Controls.Add(cmbInput)
    End Sub

 Private Sub SetupTimer()
        tmrUpdate = New Timer()
        ' MODIFICA QUI: ~60 fps per un'animazione fluida dell'ago
        tmrUpdate.Interval = 16 ' Era 33
        tmrUpdate.Start()
        AddHandler tmrUpdate.Tick, AddressOf OnTimerTick
    End Sub

    ' ================================================================
    ' AUDIO MANAGEMENT
    ' ================================================================
Private Sub StartCapture()
        StopCapture()
        Try
            If cmbInput.SelectedIndex < 0 Then Return
            audioInput = New WaveInEvent()
            audioInput.DeviceNumber = cmbInput.SelectedIndex
            audioInput.WaveFormat = New WaveFormat(44100, 16, 1)
            
            ' MODIFICA QUI: Riduciamo la latenza per una reattività istantanea
            audioInput.BufferMilliseconds = 85 ' Era 100
            audioInput.NumberOfBuffers = 3
            
            AddHandler audioInput.DataAvailable, AddressOf OnAudioData
            audioInput.StartRecording()
            isCapturing = True
        Catch ex As Exception
            isCapturing = False
        End Try
    End Sub

    Private Sub StopCapture()
        Try
            If audioInput IsNot Nothing Then
                audioInput.StopRecording()
                audioInput.Dispose()
                audioInput = Nothing
            End If
            isCapturing = False
        Catch
        End Try
    End Sub

    Private Sub OnAudioData(sender As Object, e As WaveInEventArgs)
        ' Converti 16-bit PCM → float e analizza
        Dim sampleCount = e.BytesRecorded \ 2
        If sampleCount < 512 Then Return

        Dim samples(sampleCount - 1) As Single
        For i = 0 To sampleCount - 1
            samples(i) = BitConverter.ToInt16(e.Buffer, i * 2) / 32768.0F
        Next

        tunerEngine.AnalyzeBuffer(samples, sampleCount, 44100)
    End Sub

    ' ================================================================
    ' TIMER — Aggiornamento UI con smoothing
    ' ================================================================

Private Sub OnTimerTick(sender As Object, e As EventArgs)
        If tunerEngine.HasPitch Then
            pitchHoldCounter = PITCH_HOLD_FRAMES ' Reset hold timer
            Dim stringChanged = (displayStringIdx <> tunerEngine.ClosestStringIndex)

            If Not hasSignal OrElse stringChanged Then
                ' SNAP DIRETTO: Se è un nuovo segnale o abbiamo cambiato corda, l'ago salta istantaneamente
                displayCents = tunerEngine.DetectedCents
                displayFreq = tunerEngine.DetectedFrequency
                displayCentsVelocity = 0
            Else
                ' FISICA DELL'AGO DINAMICA (Solo se restiamo sulla stessa corda)
                Dim targetCents = tunerEngine.DetectedCents
                Dim diffCents = targetCents - displayCents
                
                ' Se la deviazione è grande (es. bending improvviso), muoviti velocemente
                ' Se siamo vicini allo 0, applica un attrito forte per stabilizzare l'ago (niente tremolii)
                Dim stiffness As Single = If(Abs(diffCents) > 10, 0.2F, 0.05F)
                
                ' Fisica stile molla smorzata
                displayCentsVelocity += diffCents * stiffness
                displayCentsVelocity *= 0.7F ' Attrito (Damping)
                displayCents += displayCentsVelocity

                ' Smooth leggero solo testuale per la Frequenza in Hz (non influenza l'ago)
                displayFreq += 0.2F * (tunerEngine.DetectedFrequency - displayFreq)
            End If

            displayNoteName = tunerEngine.DetectedNoteName
            displayOctave = tunerEngine.DetectedOctave
            displayStringIdx = tunerEngine.ClosestStringIndex
            hasSignal = True
            
            ' Applica una "zona morta" visiva: se sei entro +-1.5 centesimi, consideralo 0 perfetto (in tune)
            isInTune = Abs(displayCents) < 2.0F
        Else
            ' HOLD: mantieni il valore stabile per ~2 secondi dopo l'ultima lettura valida
            If pitchHoldCounter > 0 Then
                pitchHoldCounter -= 1
            Else
                hasSignal = False
                isInTune = False
                displayCentsVelocity = 0
            End If
        End If

        displaySignalLevel += 0.4F * (tunerEngine.SignalLevel - displaySignalLevel)
        glowPhase += 0.1F
        If glowPhase > CSng(2 * Math.PI) Then glowPhase -= CSng(2 * Math.PI)

        Me.Invalidate()
    End Sub

    ' ================================================================
    ' EVENT HANDLERS
    ' ================================================================
    Private Sub CmbTuning_Changed(sender As Object, e As EventArgs)
        tunerEngine.CurrentTuningIndex = cmbTuning.SelectedIndex
        Me.Invalidate()
    End Sub

    Private Sub CmbInput_Changed(sender As Object, e As EventArgs)
        StartCapture()
    End Sub

    Private Sub BtnA4Down_Click(sender As Object, e As EventArgs)
        If tunerEngine.A4Reference > 430 Then
            tunerEngine.A4Reference -= 1
            lblA4.Text = CInt(tunerEngine.A4Reference).ToString() & " Hz"
            Me.Invalidate()
        End If
    End Sub

    Private Sub BtnA4Up_Click(sender As Object, e As EventArgs)
        If tunerEngine.A4Reference < 450 Then
            tunerEngine.A4Reference += 1
            lblA4.Text = CInt(tunerEngine.A4Reference).ToString() & " Hz"
            Me.Invalidate()
        End If
    End Sub

    Private Sub TunerForm_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        StartCapture()
    End Sub

    Private Sub TunerForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        StopCapture()
        tmrUpdate?.Stop()
        tmrUpdate?.Dispose()
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        If e.Button = MouseButtons.Left AndAlso e.Y < TITLE_H Then
            ReleaseCapture()
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0)
        End If
        MyBase.OnMouseDown(e)
    End Sub

    ' ================================================================
    ' PAINTING — Rendering completo custom
    ' ================================================================
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit

        PaintTitleBar(g)
        PaintGauge(g)
        PaintStringCards(g)
        PaintBottomBar(g)

        ' Bordo form
        Using path = ThemeColors.CreateRoundedRect(New Rectangle(0, 0, FORM_W - 1, FORM_H - 1), 12)
            Using pen As New Pen(Color.FromArgb(50, 50, 60), 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using
    End Sub

    ' ----------------------------------------------------------------
    ' TITLE BAR
    ' ----------------------------------------------------------------
    Private Sub PaintTitleBar(g As Graphics)
        Using brush As New SolidBrush(Color.FromArgb(20, 20, 26))
            g.FillRectangle(brush, 0, 0, FORM_W, TITLE_H)
        End Using
        Using pen As New Pen(Color.FromArgb(40, 40, 50))
            g.DrawLine(pen, 0, TITLE_H - 1, FORM_W, TITLE_H - 1)
        End Using

        ' Titolo
        Using font As New Font("Segoe UI", 14, FontStyle.Bold)
            Using brush As New SolidBrush(ThemeColors.AccentAmber)
                g.DrawString(ChrW(&H266A) & " TUNER", font, brush, 14, 10)
            End Using
        End Using

        ' Label "A4" accanto ai controlli di riferimento
        Using font As New Font("Segoe UI", 8)
            Using brush As New SolidBrush(ThemeColors.TextSecondary)
                g.DrawString("A4", font, brush, 368, 17)
            End Using
        End Using
    End Sub

    ' ----------------------------------------------------------------
    ' GAUGE (metro ad arco principale)
    ' ----------------------------------------------------------------
    Private Sub PaintGauge(g As Graphics)
        Dim cx = GAUGE_CX
        Dim cy = GAUGE_CY
        Dim radius = GAUGE_R
        Dim meterColor = GetMeterColor(displayCents)

        ' === ARCO SFONDO ===
        Dim arcRect = New Rectangle(cx - radius, cy - radius, radius * 2, radius * 2)
        Using pen As New Pen(Color.FromArgb(28, 28, 35), 8)
            pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
            g.DrawArc(pen, arcRect, 135, 270)
        End Using

        ' === TICK MARKS ===
        For cents = -50 To 50 Step 5
            Dim angle = CentsToAngle(CSng(cents))
            Dim isZero = (cents = 0)
            Dim isMajor = (cents Mod 10 = 0)
            Dim tickInner = CSng(radius + 2)
            Dim tickOuter = tickInner + If(isZero, 16.0F, If(isMajor, 11.0F, 6.0F))
            Dim tickColor = If(isZero, Color.FromArgb(100, ThemeColors.Success), Color.FromArgb(55, 55, 65))
            Dim tickWidth = If(isZero, 2.5F, If(isMajor, 1.5F, 0.8F))

            DrawRadialLine(g, cx, cy, tickInner, tickOuter, angle, tickColor, tickWidth)

            ' Label ai multipli di 25
            If cents Mod 25 = 0 Then
                Dim labelR = tickOuter + 12
                Dim pos = AngleToPoint(cx, cy, labelR, angle)
                Dim labelText = If(cents > 0, "+" & cents, If(cents = 0, "0", cents.ToString()))
                Using font As New Font("Segoe UI", 7)
                    Dim sz = g.MeasureString(labelText, font)
                    Dim labelColor = If(isZero, ThemeColors.Success, Color.FromArgb(80, 80, 95))
                    Using brush As New SolidBrush(labelColor)
                        g.DrawString(labelText, font, brush, pos.X - sz.Width / 2, pos.Y - sz.Height / 2)
                    End Using
                End Using
            End If
        Next

        ' === LABEL FLAT / SHARP ===
        Using font As New Font("Segoe UI", 8, FontStyle.Italic)
            Using brush As New SolidBrush(Color.FromArgb(55, 55, 70))
                g.DrawString(ChrW(&H25C2) & " FLAT", font, brush, 90, cy + radius - 8)
                Dim sharpStr = "SHARP " & ChrW(&H25B8)
                Dim sharpSz = g.MeasureString(sharpStr, font)
                g.DrawString(sharpStr, font, brush, FORM_W - 90 - sharpSz.Width, cy + radius - 8)
            End Using
        End Using

        If hasSignal AndAlso displayStringIdx >= 0 Then
            ' === ARCO ATTIVO (da 0 cents alla posizione corrente) ===
            Dim zeroAngle = 270.0F
            Dim currentAngle = CentsToAngle(displayCents)
            Dim sweep = ClampF(currentAngle - zeroAngle, -135.0F, 135.0F)

            If Abs(sweep) > 0.5F Then
                ' Glow
                Using pen As New Pen(Color.FromArgb(50, meterColor), 12)
                    pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
                    g.DrawArc(pen, arcRect, zeroAngle, sweep)
                End Using
                ' Solido
                Using pen As New Pen(meterColor, 6)
                    pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
                    g.DrawArc(pen, arcRect, zeroAngle, sweep)
                End Using
            End If

            ' === NEEDLE ===
            Dim needleAngle = CentsToAngle(ClampF(displayCents, -50, 50))
            Dim needleTip = AngleToPoint(cx, cy, radius - 8, needleAngle)
            Dim needleBase = AngleToPoint(cx, cy, 18, needleAngle)

            ' Glow dietro il needle
            Using pen As New Pen(Color.FromArgb(30, meterColor), 6)
                g.DrawLine(pen, CSng(cx), CSng(cy), needleTip.X, needleTip.Y)
            End Using
            ' Linea needle
            Using pen As New Pen(Color.FromArgb(220, Color.White), 2.0F)
                pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
                g.DrawLine(pen, needleBase.X, needleBase.Y, needleTip.X, needleTip.Y)
            End Using
            ' Dot sulla punta
            Using brush As New SolidBrush(meterColor)
                g.FillEllipse(brush, needleTip.X - 5, needleTip.Y - 5, 10, 10)
            End Using
            ' Hub centrale
            Using brush As New SolidBrush(Color.FromArgb(50, 50, 60))
                g.FillEllipse(brush, cx - 7, cy - 7, 14, 14)
            End Using
            Using brush As New SolidBrush(Color.FromArgb(75, 75, 85))
                g.FillEllipse(brush, cx - 4, cy - 4, 8, 8)
            End Using

            ' === IN TUNE GLOW (pulsante) ===
            If isInTune Then
                Dim glowAlpha = CInt(25 + 20 * Sin(glowPhase))
                Using brush As New SolidBrush(Color.FromArgb(glowAlpha, ThemeColors.Success))
                    g.FillEllipse(brush, cx - 65, cy - 45, 130, 90)
                End Using
            End If

            ' === NOTA TARGET (dalla corda più vicina) ===
            Dim tuning = TunerEngine.Tunings(tunerEngine.CurrentTuningIndex)
            Dim si = displayStringIdx
            If si >= 0 AndAlso si < tuning.Strings.Length Then
                Dim targetNote = tuning.Strings(si).NoteName
                Dim targetOctave = tuning.Strings(si).Octave

                ' Nome nota (grande)
                Using font As New Font("Segoe UI", 42, FontStyle.Bold)
                    Dim sz = g.MeasureString(targetNote, font)
                    Dim noteX = cx - sz.Width / 2 - 6
                    Dim noteY = CSng(cy - 55)
                    Using brush As New SolidBrush(Color.White)
                        g.DrawString(targetNote, font, brush, noteX, noteY)
                    End Using

                    ' Ottava (subscript accanto)
                    Using fontOct As New Font("Segoe UI", 18)
                        Using brush As New SolidBrush(ThemeColors.TextSecondary)
                            g.DrawString(targetOctave.ToString(), fontOct, brush, noteX + sz.Width - 10, noteY + sz.Height - 32)
                        End Using
                    End Using
                End Using
            End If

            ' Frequenza rilevata
            Using font As New Font("Segoe UI", 10)
                Dim freqStr = displayFreq.ToString("F1") & " Hz"
                Dim sz = g.MeasureString(freqStr, font)
                Using brush As New SolidBrush(ThemeColors.TextSecondary)
                    g.DrawString(freqStr, font, brush, cx - sz.Width / 2, cy + 15)
                End Using
            End Using

            ' Cents deviazione
            Dim centsDisplay = ClampF(displayCents, -99.9F, 99.9F)
            Using font As New Font("Segoe UI", 13, FontStyle.Bold)
                Dim centsStr = If(centsDisplay >= 0, "+", "") & centsDisplay.ToString("F1") & " " & ChrW(&HA2)
                Dim sz = g.MeasureString(centsStr, font)
                Using brush As New SolidBrush(meterColor)
                    g.DrawString(centsStr, font, brush, cx - sz.Width / 2, cy + 38)
                End Using
            End Using

            ' Status
            If isInTune Then
                Using font As New Font("Segoe UI", 10, FontStyle.Bold)
                    Dim statusStr = ChrW(&H2713) & " IN TUNE"
                    Dim sz = g.MeasureString(statusStr, font)
                    Using brush As New SolidBrush(ThemeColors.Success)
                        g.DrawString(statusStr, font, brush, cx - sz.Width / 2, cy + 62)
                    End Using
                End Using
            ElseIf Abs(displayCents) > 50 Then
                Using font As New Font("Segoe UI", 9, FontStyle.Bold)
                    Dim dir = If(displayCents < 0, ChrW(&H2191) & " TUNE UP", "TUNE DOWN " & ChrW(&H2193))
                    Dim sz = g.MeasureString(dir, font)
                    Using brush As New SolidBrush(ThemeColors.Danger)
                        g.DrawString(dir, font, brush, cx - sz.Width / 2, cy + 62)
                    End Using
                End Using
            End If
        Else
            ' === STATO IDLE ===
            Using font As New Font("Segoe UI", 28)
                Dim sym = ChrW(&H266A)
                Dim sz = g.MeasureString(sym, font)
                Using brush As New SolidBrush(Color.FromArgb(35, 35, 45))
                    g.DrawString(sym, font, brush, cx - sz.Width / 2, cy - 40)
                End Using
            End Using
            Using font As New Font("Segoe UI", 11)
                Dim msg = If(isCapturing, "Suona una corda...", "Seleziona un input")
                Dim sz = g.MeasureString(msg, font)
                Using brush As New SolidBrush(Color.FromArgb(55, 55, 70))
                    g.DrawString(msg, font, brush, cx - sz.Width / 2, cy + 5)
                End Using
            End Using
        End If
    End Sub

    ' ----------------------------------------------------------------
    ' STRING CARDS (indicatori 6 corde)
    ' ----------------------------------------------------------------
    Private Sub PaintStringCards(g As Graphics)
        If tunerEngine.CurrentTuningIndex < 0 OrElse tunerEngine.CurrentTuningIndex >= TunerEngine.Tunings.Length Then Return
        Dim tuning = TunerEngine.Tunings(tunerEngine.CurrentTuningIndex)

        Dim cardW = 75
        Dim cardH = STRINGS_H
        Dim gap = 14
        Dim totalW = 6 * cardW + 5 * gap
        Dim startX = (FORM_W - totalW) \ 2
        Dim y = STRINGS_Y

        For i = 0 To 5
            Dim x = startX + i * (cardW + gap)
            Dim isActive = (hasSignal AndAlso displayStringIdx = i)
            Dim stringNum = 6 - i

            ' Card sfondo
            Dim cardRect = New Rectangle(x, y, cardW, cardH)
            Using path = ThemeColors.CreateRoundedRect(cardRect, 10)
                If isActive Then
                    Dim glowColor = If(isInTune, ThemeColors.Success, GetMeterColor(displayCents))
                    ' Glow esterno
                    Using pen As New Pen(Color.FromArgb(50, glowColor), 4)
                        g.DrawPath(pen, path)
                    End Using
                    Using brush As New SolidBrush(Color.FromArgb(35, 35, 42))
                        g.FillPath(brush, path)
                    End Using
                    Using pen As New Pen(glowColor, 1.5F)
                        g.DrawPath(pen, path)
                    End Using
                Else
                    Using brush As New SolidBrush(Color.FromArgb(25, 25, 32))
                        g.FillPath(brush, path)
                    End Using
                    Using pen As New Pen(Color.FromArgb(40, 40, 50), 1.0F)
                        g.DrawPath(pen, path)
                    End Using
                End If
            End Using

            If i < tuning.Strings.Length Then
                ' Nome nota
                Dim noteColor = If(isActive, Color.White, ThemeColors.TextSecondary)
                Using font As New Font("Segoe UI", 14, FontStyle.Bold)
                    Dim noteStr = tuning.Strings(i).ToString()
                    Dim sz = g.MeasureString(noteStr, font)
                    Using brush As New SolidBrush(noteColor)
                        g.DrawString(noteStr, font, brush, x + (cardW - sz.Width) / 2, y + 8)
                    End Using
                End Using

                ' Frequenza target
                Dim freq = tuning.Strings(i).GetFrequency(tunerEngine.A4Reference)
                Using font As New Font("Segoe UI", 8)
                    Dim freqStr = freq.ToString("F0") & " Hz"
                    Dim sz = g.MeasureString(freqStr, font)
                    Dim freqColor = If(isActive, Color.FromArgb(180, 180, 195), Color.FromArgb(80, 80, 95))
                    Using brush As New SolidBrush(freqColor)
                        g.DrawString(freqStr, font, brush, x + (cardW - sz.Width) / 2, y + 36)
                    End Using
                End Using

                ' Numero corda
                Using font As New Font("Segoe UI", 8)
                    Dim numStr = stringNum & GetOrdinalSuffix(stringNum)
                    Dim sz = g.MeasureString(numStr, font)
                    Dim numColor = If(isActive, Color.FromArgb(150, 150, 165), Color.FromArgb(60, 60, 75))
                    Using brush As New SolidBrush(numColor)
                        g.DrawString(numStr, font, brush, x + (cardW - sz.Width) / 2, y + 56)
                    End Using
                End Using
            End If
        Next
    End Sub

    ' ----------------------------------------------------------------
    ' BOTTOM BAR (input + signal level)
    ' ----------------------------------------------------------------
    Private Sub PaintBottomBar(g As Graphics)
        ' Sfondo
        Using brush As New SolidBrush(Color.FromArgb(18, 18, 22))
            g.FillRectangle(brush, 0, BOTTOM_Y, FORM_W, FORM_H - BOTTOM_Y)
        End Using
        Using pen As New Pen(Color.FromArgb(35, 35, 45))
            g.DrawLine(pen, 0, BOTTOM_Y, FORM_W, BOTTOM_Y)
        End Using

        ' Label "INPUT"
        Using font As New Font("Segoe UI", 7)
            Using brush As New SolidBrush(ThemeColors.TextSecondary)
                g.DrawString("INPUT DEVICE", font, brush, 20, BOTTOM_Y + 4)
            End Using
        End Using

        ' Signal level meter
        Dim levelX = 390
        Dim levelY = BOTTOM_Y + 18
        Dim levelW = 140
        Dim levelH = 8

        Using font As New Font("Segoe UI", 7)
            Using brush As New SolidBrush(ThemeColors.TextSecondary)
                g.DrawString("INPUT LEVEL", font, brush, levelX, BOTTOM_Y + 4)
            End Using
        End Using

        ' Barra sfondo
        Dim barRect = New Rectangle(levelX, levelY, levelW, levelH)
        Using path = ThemeColors.CreateRoundedRect(barRect, 4)
            Using brush As New SolidBrush(Color.FromArgb(30, 30, 38))
                g.FillPath(brush, path)
            End Using
        End Using

        ' Barra riempimento
        Dim fillW = CInt(Min(CSng(displaySignalLevel * 5), 1.0F) * levelW)
        If fillW > 2 Then
            Dim fillRect = New Rectangle(levelX, levelY, fillW, levelH)
            Dim fillColor = If(displaySignalLevel > 0.5F, ThemeColors.Danger, ThemeColors.Success)
            Using path = ThemeColors.CreateRoundedRect(fillRect, 4)
                Using brush As New SolidBrush(fillColor)
                    g.FillPath(brush, path)
                End Using
            End Using
        End If
    End Sub

    ' ================================================================
    ' HELPER FUNCTIONS
    ' ================================================================

    ''' <summary>
    ''' Converte cents (-50..+50) in angolo GDI+ (135°..405°).
    ''' 0 cents = 270° (in alto). -50 = 135° (sinistra). +50 = 405° (destra).
    ''' </summary>
    Private Function CentsToAngle(cents As Single) As Single
        Dim c = Max(-50.0F, Min(50.0F, cents))
        Return 270.0F + c * 2.7F
    End Function

    Private Function AngleToPoint(cx As Single, cy As Single, radius As Single, angleDeg As Single) As PointF
        Dim angleRad = angleDeg * PI / 180.0
        Return New PointF(
            CSng(cx + radius * Cos(angleRad)),
            CSng(cy + radius * Sin(angleRad)))
    End Function

    Private Sub DrawRadialLine(g As Graphics, cx As Integer, cy As Integer, innerR As Single, outerR As Single, angleDeg As Single, color As Color, width As Single)
        Dim inner = AngleToPoint(cx, cy, innerR, angleDeg)
        Dim outer = AngleToPoint(cx, cy, outerR, angleDeg)
        Using pen As New Pen(color, width)
            g.DrawLine(pen, inner, outer)
        End Using
    End Sub

    Private Function GetMeterColor(cents As Single) As Color
        If Single.IsNaN(cents) OrElse Single.IsInfinity(cents) Then Return ThemeColors.Danger
        Dim a = CSng(Abs(cents))
        If a <= 3 Then Return ThemeColors.Success
        If a <= 10 Then
            Return LerpColor(ThemeColors.Success, ThemeColors.AccentAmber, (a - 3) / 7.0F)
        End If
        If a <= 25 Then
            Return LerpColor(ThemeColors.AccentAmber, ThemeColors.Danger, (a - 10) / 15.0F)
        End If
        Return ThemeColors.Danger
    End Function

    Private Function LerpColor(c1 As Color, c2 As Color, t As Single) As Color
        If Single.IsNaN(t) Then t = 0
        If t < 0 Then t = 0
        If t > 1 Then t = 1
        Dim r = CInt(c1.R) + CInt((CInt(c2.R) - CInt(c1.R)) * t)
        Dim g = CInt(c1.G) + CInt((CInt(c2.G) - CInt(c1.G)) * t)
        Dim b = CInt(c1.B) + CInt((CInt(c2.B) - CInt(c1.B)) * t)
        If r < 0 Then r = 0 : If r > 255 Then r = 255
        If g < 0 Then g = 0 : If g > 255 Then g = 255
        If b < 0 Then b = 0 : If b > 255 Then b = 255
        Return Color.FromArgb(r, g, b)
    End Function

    Private Function ClampF(value As Single, minVal As Single, maxVal As Single) As Single
        Return CSng(Max(CDbl(minVal), Min(CDbl(maxVal), CDbl(value))))
    End Function

    Private Function GetOrdinalSuffix(n As Integer) As String
        Select Case n
            Case 1 : Return "st"
            Case 2 : Return "nd"
            Case 3 : Return "rd"
            Case Else : Return "th"
        End Select
    End Function

End Class
