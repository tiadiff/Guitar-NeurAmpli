Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

' ============================================================
' DESIGN SYSTEM - Colori e Costanti del tema
' ============================================================
Public Module ThemeColors
    Public ReadOnly BgDeep As Color = Color.FromArgb(13, 13, 13)
    Public ReadOnly BgPanel As Color = Color.FromArgb(26, 26, 30)
    Public ReadOnly Surface As Color = Color.FromArgb(37, 37, 41)
    Public ReadOnly BorderClr As Color = Color.FromArgb(58, 58, 66)
    Public ReadOnly TextPrimary As Color = Color.FromArgb(240, 236, 229)
    Public ReadOnly TextSecondary As Color = Color.FromArgb(138, 138, 150)
    Public ReadOnly AccentAmber As Color = Color.FromArgb(255, 140, 0)
    Public ReadOnly AccentCyan As Color = Color.FromArgb(0, 229, 255)
    Public ReadOnly Danger As Color = Color.FromArgb(255, 71, 87)
    Public ReadOnly Success As Color = Color.FromArgb(46, 213, 115)

    Public Function CreateRoundedRect(rect As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim d = radius * 2
        If d > rect.Height Then d = rect.Height
        If d > rect.Width Then d = rect.Width
        path.AddArc(rect.X, rect.Y, d, d, 180, 90)
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        Return path
    End Function
End Module

' ============================================================
' GLASS PANEL - Pannello con effetto Glassmorphism
' ============================================================
Public Class GlassPanel
    Inherits Panel

    Public Sub New()
        Me.DoubleBuffered = True
        Me.BorderStyle = BorderStyle.None
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        ' Suppressed - handled in OnPaint
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim radius = 14
        Dim rect = New Rectangle(0, 0, Width - 1, Height - 1)

        Using path = ThemeColors.CreateRoundedRect(rect, radius)
            ' Solid background to match BackColor exactly
            Using brush As New SolidBrush(Me.BackColor)
                g.FillPath(brush, path)
            End Using

            ' Glass border
            Using pen As New Pen(Color.FromArgb(45, 45, 52), 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using

        ' Paint children on top
        MyBase.OnPaint(e)
    End Sub
End Class

' ============================================================
' ROCK KNOB - Manopola Rotativa Premium
' ============================================================
Public Class RockKnob
    Inherits Control

    Private _value As Integer = 0
    Private _isHovered As Boolean = False

    Public Property Maximum As Integer = 10
    Public Property Minimum As Integer = 0
    Public Property AccentColor As Color = Color.Gold
    Public Property KnobText As String = ""

    Public Property Value As Integer
        Get
            Return _value
        End Get
        Set(val As Integer)
            If val > Maximum Then val = Maximum
            If val < Minimum Then val = Minimum
            If _value <> val Then
                _value = val
                Me.Invalidate()
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End If
        End Set
    End Property

    Private isDragging As Boolean = False
    Private lastY As Integer = 0

    Public Sub New()
        Me.Size = New Size(80, 100)
        Me.DoubleBuffered = True
        Me.ForeColor = Color.White
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        isDragging = True : lastY = e.Y : Me.Capture = True
    End Sub
    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        isDragging = False : Me.Capture = False
    End Sub
    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        If isDragging Then
            Dim delta = lastY - e.Y
            If Math.Abs(delta) > 2 Then
                Value += Math.Sign(delta)
                lastY = e.Y
            End If
        End If
    End Sub
    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        _isHovered = True : Me.Invalidate()
    End Sub
    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        _isHovered = False : Me.Invalidate()
    End Sub

    Public Event ValueChanged As EventHandler

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        ' Paint parent's background color for seamless look
        Dim bgColor = If(Parent IsNot Nothing, Parent.BackColor, Color.FromArgb(22, 22, 28))
        e.Graphics.Clear(bgColor)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        Dim knobSize = Width - 12
        Dim cx = Width \ 2
        Dim cy = (knobSize \ 2) + 6
        Dim knobRect As New Rectangle(cx - knobSize \ 2, 6, knobSize, knobSize)

        ' === DROP SHADOW ===
        For i = 4 To 1 Step -1
            Dim sr = New Rectangle(knobRect.X + 1, knobRect.Y + 1 + i, knobRect.Width, knobRect.Height)
            Using b As New SolidBrush(Color.FromArgb(12 * i, 0, 0, 0))
                g.FillEllipse(b, sr)
            End Using
        Next

        ' === OUTER RING (metallic gradient) ===
        Using path As New GraphicsPath()
            path.AddEllipse(knobRect)
            Using pgb As New PathGradientBrush(path)
                pgb.CenterColor = Color.FromArgb(58, 58, 64)
                pgb.SurroundColors = New Color() {Color.FromArgb(22, 22, 25)}
                g.FillEllipse(pgb, knobRect)
            End Using
        End Using
        Using pen As New Pen(Color.FromArgb(72, 72, 80), 1.2F)
            g.DrawEllipse(pen, knobRect)
        End Using

        ' === INNER CIRCLE ===
        Dim innerSize = knobSize - 14
        Dim innerRect = New Rectangle(cx - innerSize \ 2, cy - innerSize \ 2, innerSize, innerSize)
        Using path As New GraphicsPath()
            path.AddEllipse(innerRect)
            Using pgb As New PathGradientBrush(path)
                pgb.CenterColor = Color.FromArgb(48, 48, 54)
                pgb.SurroundColors = New Color() {Color.FromArgb(26, 26, 30)}
                g.FillEllipse(pgb, innerRect)
            End Using
        End Using

        ' === VALUE ARC ===
        Dim arcRect = New Rectangle(cx - (knobSize \ 2) + 4, 10, knobSize - 8, knobSize - 8)
        Dim startAngle As Single = 135
        Dim sweepAngle As Single = 270
        Dim range = Maximum - Minimum : If range = 0 Then range = 1
        Dim percent As Single = CSng(_value - Minimum) / range
        Dim currentSweep As Single = sweepAngle * percent

        ' Track
        Using pen As New Pen(Color.FromArgb(35, 35, 42), 3.0F)
            pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
            g.DrawArc(pen, arcRect, startAngle, sweepAngle)
        End Using

        ' Glow + main arc
        If currentSweep > 0.5F Then
            Using pen As New Pen(Color.FromArgb(35, AccentColor), 8.0F)
                pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
                g.DrawArc(pen, arcRect, startAngle, currentSweep)
            End Using
            Using pen As New Pen(AccentColor, 3.0F)
                pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
                g.DrawArc(pen, arcRect, startAngle, currentSweep)
            End Using
        End If

        ' === INDICATOR LINE ===
        Dim indicatorAngle = (startAngle + currentSweep) * Math.PI / 180.0
        Dim lineIn = (innerSize \ 2) - 8 : Dim lineOut = (innerSize \ 2) + 2
        Dim x1 = cx + CInt(lineIn * Math.Cos(indicatorAngle))
        Dim y1 = cy + CInt(lineIn * Math.Sin(indicatorAngle))
        Dim x2 = cx + CInt(lineOut * Math.Cos(indicatorAngle))
        Dim y2 = cy + CInt(lineOut * Math.Sin(indicatorAngle))
        Using pen As New Pen(AccentColor, 2.0F)
            pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
            g.DrawLine(pen, x1, y1, x2, y2)
        End Using

        ' === VALUE TEXT ===
        Using font As New Font("Segoe UI", 9, FontStyle.Bold)
            Dim sz = g.MeasureString(_value.ToString(), font)
            Using b As New SolidBrush(ThemeColors.TextPrimary)
                g.DrawString(_value.ToString(), font, b, cx - sz.Width / 2, cy - sz.Height / 2)
            End Using
        End Using

        ' === LABEL TEXT ===
        Using font As New Font("Segoe UI", 7.5F, FontStyle.Regular)
            Dim sz = g.MeasureString(KnobText, font)
            Using b As New SolidBrush(ThemeColors.TextSecondary)
                g.DrawString(KnobText, font, b, cx - sz.Width / 2, Height - 16)
            End Using
        End Using

        ' === HOVER RING ===
        If _isHovered Then
            Using pen As New Pen(Color.FromArgb(25, AccentColor), 1.5F)
                g.DrawEllipse(pen, knobRect)
            End Using
        End If
    End Sub
End Class

' ============================================================
' ROCK SWITCH - Interruttore Premium con LED Glow
' ============================================================
Public Class RockSwitch
    Inherits Control

    Private _checked As Boolean = False
    Private _isHovered As Boolean = False
    Public Property CheckedColor As Color = Color.Lime
    Public Property LabelText As String = "SWITCH"

    Public Property Checked As Boolean
        Get
            Return _checked
        End Get
        Set(value As Boolean)
            If _checked <> value Then
                _checked = value
                Me.Invalidate()
                RaiseEvent CheckedChanged(Me, EventArgs.Empty)
            End If
        End Set
    End Property

    Public Event CheckedChanged As EventHandler

    Public Sub New()
        Me.Size = New Size(100, 30)
        Me.DoubleBuffered = True
        Me.Cursor = Cursors.Hand
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Protected Overrides Sub OnClick(e As EventArgs)
        Checked = Not Checked
    End Sub
    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        _isHovered = True : Me.Invalidate()
    End Sub
    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        _isHovered = False : Me.Invalidate()
    End Sub

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        Dim bgColor = If(Parent IsNot Nothing, Parent.BackColor, Color.FromArgb(22, 22, 28))
        e.Graphics.Clear(bgColor)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        Dim rect = New Rectangle(0, 0, Width - 1, Height - 1)
        Dim radius = 6

        ' === BACKGROUND ===
        Using path = ThemeColors.CreateRoundedRect(rect, radius)
            Dim bgColor = If(_checked, Color.FromArgb(30, 30, 36), Color.FromArgb(20, 20, 24))
            If _isHovered Then bgColor = Color.FromArgb(bgColor.R + 6, bgColor.G + 6, bgColor.B + 8)
            Using brush As New SolidBrush(bgColor)
                g.FillPath(brush, path)
            End Using
            Dim borderColor = If(_checked, Color.FromArgb(70, CheckedColor), Color.FromArgb(45, 45, 52))
            If _isHovered AndAlso Not _checked Then borderColor = Color.FromArgb(65, 65, 75)
            Using pen As New Pen(borderColor, 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using

        ' === LED with GLOW ===
        Dim ledX = 10, ledY = (Height - 12) \ 2, ledSize = 12
        If _checked Then
            For i = 3 To 1 Step -1
                Dim gr = New Rectangle(ledX - i * 3, ledY - i * 3, ledSize + i * 6, ledSize + i * 6)
                Using b As New SolidBrush(Color.FromArgb(10 * i, CheckedColor))
                    g.FillEllipse(b, gr)
                End Using
            Next
            Using b As New SolidBrush(CheckedColor)
                g.FillEllipse(b, ledX, ledY, ledSize, ledSize)
            End Using
            Using b As New SolidBrush(Color.FromArgb(180, 255, 255, 255))
                g.FillEllipse(b, ledX + 3, ledY + 2, 5, 5)
            End Using
        Else
            Using b As New SolidBrush(Color.FromArgb(35, 35, 40))
                g.FillEllipse(b, ledX, ledY, ledSize, ledSize)
            End Using
            Using pen As New Pen(Color.FromArgb(50, 50, 56), 1.0F)
                g.DrawEllipse(pen, ledX, ledY, ledSize, ledSize)
            End Using
        End If

        ' === LABEL ===
        Using font As New Font("Segoe UI", 8.5F, FontStyle.Regular)
            Dim txtColor = If(_checked, ThemeColors.TextPrimary, ThemeColors.TextSecondary)
            Dim sz = g.MeasureString(LabelText, font)
            Using b As New SolidBrush(txtColor)
                g.DrawString(LabelText, font, b, 30, (Height - sz.Height) / 2)
            End Using
        End Using
    End Sub
End Class

' ============================================================
' MODERN BUTTON - Bottone Premium con Rounded Corners
' ============================================================
Public Class ModernButton
    Inherits Control

    Private _isHovered As Boolean = False
    Private _isPressed As Boolean = False

    Public Sub New()
        Me.Size = New Size(100, 30)
        Me.DoubleBuffered = True
        Me.Cursor = Cursors.Hand
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.SupportsTransparentBackColor, True)
    End Sub

    Public Sub PerformClick()
        OnClick(EventArgs.Empty)
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        _isHovered = True : Me.Invalidate()
    End Sub
    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        _isHovered = False : _isPressed = False : Me.Invalidate()
    End Sub
    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        _isPressed = True : Me.Invalidate()
        MyBase.OnMouseDown(e)
    End Sub
    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        _isPressed = False : Me.Invalidate()
        MyBase.OnMouseUp(e)
    End Sub

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        Dim bgColor = If(Parent IsNot Nothing, Parent.BackColor, Color.FromArgb(13, 13, 13))
        e.Graphics.Clear(bgColor)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        Dim rect = New Rectangle(0, 0, Width - 1, Height - 1)
        Dim radius = 8

        Using path = ThemeColors.CreateRoundedRect(rect, radius)
            Dim bg = Me.BackColor
            If Not Me.Enabled Then
                bg = Color.FromArgb(30, 30, 35)
            ElseIf _isPressed Then
                bg = Color.FromArgb(Math.Max(bg.R - 20, 0), Math.Max(bg.G - 20, 0), Math.Max(bg.B - 20, 0))
            ElseIf _isHovered Then
                bg = Color.FromArgb(Math.Min(bg.R + 15, 255), Math.Min(bg.G + 15, 255), Math.Min(bg.B + 15, 255))
            End If

            ' Gradient fill
            Dim c1 = Color.FromArgb(Math.Min(bg.R + 10, 255), Math.Min(bg.G + 10, 255), Math.Min(bg.B + 10, 255))
            Dim c2 = Color.FromArgb(Math.Max(bg.R - 8, 0), Math.Max(bg.G - 8, 0), Math.Max(bg.B - 8, 0))
            Using brush As New LinearGradientBrush(rect, c1, c2, 90.0F)
                g.FillPath(brush, path)
            End Using

            ' Border
            Dim brd = If(_isHovered, Color.FromArgb(100, 255, 255, 255), Color.FromArgb(40, 255, 255, 255))
            Using pen As New Pen(brd, 1.0F)
                g.DrawPath(pen, path)
            End Using

            ' Hover top glow
            If _isHovered Then
                Dim glowR = New Rectangle(2, 1, Width - 5, 3)
                Using gb As New LinearGradientBrush(glowR, Color.FromArgb(30, Me.ForeColor), Color.Transparent, 90.0F)
                    g.FillRectangle(gb, glowR)
                End Using
            End If
        End Using

        ' Text
        Using font As New Font("Segoe UI", 8.5F, FontStyle.Bold)
            Dim sz = g.MeasureString(Text, font)
            Dim txtColor = If(Me.Enabled, Me.ForeColor, ThemeColors.TextSecondary)
            Using b As New SolidBrush(txtColor)
                g.DrawString(Text, font, b, (Width - sz.Width) / 2, (Height - sz.Height) / 2)
            End Using
        End Using
    End Sub
End Class