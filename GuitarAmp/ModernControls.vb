Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

' ============================================================
' DESIGN SYSTEM - Colori e Costanti del tema (FLAT / MODERN)
' ============================================================
Public Module ThemeColors
    Public ReadOnly BgDeep As Color = Color.FromArgb(14, 14, 16)      ' Sfondo principale ultra scuro
    Public ReadOnly BgPanel As Color = Color.FromArgb(24, 24, 28)     ' Sfondo dei pannelli (flat)
    Public ReadOnly Surface As Color = Color.FromArgb(36, 36, 42)     ' Superficie dei controlli
    Public ReadOnly BorderClr As Color = Color.FromArgb(48, 48, 56)   ' Bordi leggeri
    Public ReadOnly TextPrimary As Color = Color.FromArgb(245, 245, 250)
    Public ReadOnly TextSecondary As Color = Color.FromArgb(130, 130, 145)

    ' Colori Neon
    Public ReadOnly AccentAmber As Color = Color.FromArgb(255, 170, 0)
    Public ReadOnly AccentCyan As Color = Color.FromArgb(0, 240, 255)
    Public ReadOnly Danger As Color = Color.FromArgb(255, 75, 95)
    Public ReadOnly Success As Color = Color.FromArgb(30, 215, 96)    ' Verde digitale brillante

    Public Function CreateRoundedRect(rect As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        If radius <= 0 Then
            path.AddRectangle(rect)
            Return path
        End If
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
' FLAT PANEL - Pannello Base Pulito
' ============================================================
Public Class GlassPanel
    Inherits Panel

    Public Sub New()
        Me.DoubleBuffered = True
        Me.BorderStyle = BorderStyle.None
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        ' Trasparenza apparente: disegniamo il colore del parent
        Dim parentBg = If(Me.Parent IsNot Nothing, Me.Parent.BackColor, ThemeColors.BgDeep)
        e.Graphics.Clear(parentBg)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim radius = 16 ' Curve gentili
        Dim rect = New Rectangle(0, 0, Width - 1, Height - 1)

        Using path = ThemeColors.CreateRoundedRect(rect, radius)
            ' Sfondo solido flat (rispetta il vero BackColor del controllo per blending perfetto)
            Using brush As New SolidBrush(Me.BackColor)
                g.FillPath(brush, path)
            End Using

            ' Bordo sottile minimale
            Using pen As New Pen(Color.FromArgb(50, 50, 60), 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using

        MyBase.OnPaint(e)
    End Sub
End Class

' ============================================================
' FLAT/NEOMURPHIC KNOB - Manopola con Neon Arc ad alto contrasto
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
        Dim bgColor = If(Parent IsNot Nothing, Parent.BackColor, ThemeColors.BgPanel)
        e.Graphics.Clear(bgColor)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        Dim knobSize = Width - 16
        Dim cx = Width \ 2
        Dim cy = (knobSize \ 2) + 8
        Dim knobRect As New Rectangle(cx - knobSize \ 2, cy - knobSize \ 2, knobSize, knobSize)

        ' === SOFT OUTSIDE DROP SHADOW (Neumorphic touch) ===
        For i = 6 To 1 Step -1
            Dim sr = New Rectangle(knobRect.X - i + 2, knobRect.Y - i + 4, knobRect.Width + i * 2, knobRect.Height + i * 2)
            Using b As New SolidBrush(Color.FromArgb(8 * i, 0, 0, 0))
                g.FillEllipse(b, sr)
            End Using
        Next

        ' === SOLID KNOB BODY ===
        Using brush As New SolidBrush(ThemeColors.Surface)
            g.FillEllipse(brush, knobRect)
        End Using
        
        ' Bordo interno sottilissimo e poco visibile per definizione
        Using pen As New Pen(Color.FromArgb(60, 60, 68), 1.0F)
            g.DrawEllipse(pen, knobRect)
        End Using

        ' === PURE NEON VALUE ARC ===
        Dim arcRect = New Rectangle(knobRect.X + 4, knobRect.Y + 4, knobSize - 8, knobSize - 8)
        Dim startAngle As Single = 135
        Dim sweepAngle As Single = 270
        Dim range = Maximum - Minimum : If range = 0 Then range = 1
        Dim percent As Single = CSng(_value - Minimum) / range
        Dim currentSweep As Single = sweepAngle * percent

        ' Track di sfondo (grigio molto scuro, spesso)
        Using pen As New Pen(Color.FromArgb(20, 20, 24), 4.5F)
            pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
            g.DrawArc(pen, arcRect, startAngle, sweepAngle)
        End Using

        ' Glow e linea neon
        If currentSweep > 0.5F Then
            ' Glow blurrato
            Using pen As New Pen(Color.FromArgb(60, AccentColor), 8.0F)
                pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
                g.DrawArc(pen, arcRect, startAngle, currentSweep)
            End Using
            ' Linea solida vivida
            Using pen As New Pen(AccentColor, 4.5F)
                pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
                g.DrawArc(pen, arcRect, startAngle, currentSweep)
            End Using
        End If

        ' === INDICATOR DOT (Stile Minimal) ===
        Dim indicatorAngle = (startAngle + currentSweep) * Math.PI / 180.0
        Dim dotDistance = (knobSize \ 2) - 14
        Dim dx = cx + CInt(dotDistance * Math.Cos(indicatorAngle))
        Dim dy = cy + CInt(dotDistance * Math.Sin(indicatorAngle))
        
        Using brush As New SolidBrush(Color.White)
            g.FillEllipse(brush, dx - 2, dy - 2, 5, 5) ' Dot bianco opaco
        End Using

        ' === VALUE E LABEL TEXT (Piccoli, PULITI) ===
        Using fontVal As New Font("Segoe UI", 9.0F, FontStyle.Bold)
            Dim strVal = _value.ToString()
            Dim szVal = g.MeasureString(strVal, fontVal)
            Using b As New SolidBrush(ThemeColors.TextPrimary)
                g.DrawString(strVal, fontVal, b, cx - szVal.Width / 2, cy - szVal.Height / 2)
            End Using
        End Using

        Using fontLbl As New Font("Segoe UI", 8.0F, FontStyle.Regular)
            Dim szLbl = g.MeasureString(KnobText.ToUpper(), fontLbl)
            ' Hover accentua la label
            Dim lblColor = If(_isHovered, ThemeColors.TextPrimary, ThemeColors.TextSecondary)
            Using b As New SolidBrush(lblColor)
                g.DrawString(KnobText.ToUpper(), fontLbl, b, cx - szLbl.Width / 2, Height - 18)
            End Using
        End Using
    End Sub
End Class

' ============================================================
' PILL SWITCH - Toggle Flat iOS Style
' ============================================================
Public Class RockSwitch
    Inherits Control

    Private _checked As Boolean = False
    Private _isHovered As Boolean = False
    Private _animationSlide As Single = 0.0F ' Simula l'animazione slide
    Private _isSelected As Boolean = False
    Public Property CheckedColor As Color = Color.Lime
    Public Property LabelText As String = "SWITCH"

    Public Event SelectClicked As EventHandler

    Public Property Checked As Boolean
        Get
            Return _checked
        End Get
        Set(value As Boolean)
            If _checked <> value Then
                _checked = value
                _animationSlide = If(_checked, 1.0F, 0.0F) ' Senza animazione fluida reale, switch netto ma pulito
                Me.Invalidate()
                RaiseEvent CheckedChanged(Me, EventArgs.Empty)
            End If
        End Set
    End Property

    Public Property IsSelected As Boolean
        Get
            Return _isSelected
        End Get
        Set(value As Boolean)
            If _isSelected <> value Then
                _isSelected = value
                Me.Invalidate()
            End If
        End Set
    End Property

    Public Event CheckedChanged As EventHandler

    Public Sub New()
        Me.Size = New Size(120, 30)
        Me.DoubleBuffered = True
        Me.Cursor = Cursors.Hand
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        If e.X < 50 Then
            Checked = Not Checked
        Else
            RaiseEvent SelectClicked(Me, EventArgs.Empty)
        End If
        MyBase.OnMouseUp(e)
    End Sub
    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        _isHovered = True : Me.Invalidate()
    End Sub
    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        _isHovered = False : Me.Invalidate()
    End Sub

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        Dim bgColor = If(Parent IsNot Nothing, Parent.BackColor, ThemeColors.BgPanel)
        e.Graphics.Clear(bgColor)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        ' Toggle Track (Pill shape)
        Dim trackWidth = 44
        Dim trackHeight = 22
        Dim trackY = (Height - trackHeight) \ 2
        Dim trackRect = New Rectangle(4, trackY, trackWidth, trackHeight)
        Dim trackRadius = trackHeight \ 2

        Using path = ThemeColors.CreateRoundedRect(trackRect, trackRadius)
            ' Colore Sfondo Track
            Dim bgColor = If(_checked, CheckedColor, ThemeColors.Surface)
            Using brush As New SolidBrush(bgColor)
                g.FillPath(brush, path)
            End Using
            
            ' Inner shadow se SPENTO per dare profondità di "buco", Glow in hover se ACCESO
            If Not _checked Then
                Using pen As New Pen(Color.FromArgb(100, 0, 0, 0), 2.0F)
                    g.DrawPath(pen, path)
                End Using
            ElseIf _isHovered Then
                ' Hover highlight sulla track se acceso
                Using pen As New Pen(Color.FromArgb(100, 255, 255, 255), 2.0F)
                    g.DrawPath(pen, path)
                End Using
            End If
        End Using

        ' Toggle Thumb (Cerchio bianco perfetto)
        Dim thumbSize = 18
        Dim thumbY = trackY + 2
        Dim thumbXStart = 6
        Dim thumbXEnd = trackWidth + 4 - thumbSize - 2
        Dim thumbX = If(_checked, thumbXEnd, thumbXStart)
        
        Dim thumbRect = New Rectangle(thumbX, thumbY, thumbSize, thumbSize)
        
        ' Ombra sotto il thumb
        Using brush As New SolidBrush(Color.FromArgb(100, 0, 0, 0))
            g.FillEllipse(brush, thumbRect.X, thumbRect.Y + 1, thumbSize, thumbSize)
        End Using
        
        Using brush As New SolidBrush(Color.White)
            g.FillEllipse(brush, thumbRect)
        End Using

        ' Label Text
        Using font As New Font("Segoe UI", 9.0F, FontStyle.Regular)
            Dim txtColor = If(_checked, ThemeColors.TextPrimary, ThemeColors.TextSecondary)
            If _isHovered AndAlso Not _checked Then txtColor = Color.FromArgb(180, 180, 190)
            If _isSelected Then txtColor = ThemeColors.AccentCyan ' Text becomes Cyan when selected!
            
            Dim sz = g.MeasureString(LabelText, font)
            Dim lblY = (Height - sz.Height) / 2
            
            ' Selezionato: Glow dietro il testo
            If _isSelected Then
                Dim bgRect = New Rectangle(54, CInt(lblY), CInt(sz.Width + 8), CInt(sz.Height))
                Using bGlow As New SolidBrush(Color.FromArgb(20, ThemeColors.AccentCyan))
                    g.FillRectangle(bGlow, bgRect)
                End Using
            End If

            Using b As New SolidBrush(txtColor)
                g.DrawString(LabelText, font, b, 58, lblY)
            End Using
        End Using
    End Sub
End Class

' ============================================================
' MODERN BUTTON - "Flat/Solid Pill" Style
' ============================================================
Public Class ModernButton
    Inherits Control

    Private _isHovered As Boolean = False
    Private _isPressed As Boolean = False

    Public Sub New()
        Me.Size = New Size(100, 30)
        Me.DoubleBuffered = True
        Me.Cursor = Cursors.Hand
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
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
        Dim bgColor = If(Parent IsNot Nothing, Parent.BackColor, ThemeColors.BgPanel)
        e.Graphics.Clear(bgColor)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        Dim rect = New Rectangle(1, 1, Width - 3, Height - 3)
        Dim radius = Height \ 2 ' Round Pill

        Using path = ThemeColors.CreateRoundedRect(rect, radius)
            ' Definiamo i colori base e le variazioni usando ForeColor e BackColor del controllo
            Dim baseBg = Me.BackColor
            Dim baseFore = Me.ForeColor
            
            ' Hover & Pressed Effects
            If Not Me.Enabled Then
                baseBg = ThemeColors.Surface
                baseFore = ThemeColors.TextSecondary
            ElseIf _isPressed Then
                baseBg = Color.FromArgb(Math.Max(baseBg.R - 20, 0), Math.Max(baseBg.G - 20, 0), Math.Max(baseBg.B - 20, 0))
            ElseIf _isHovered Then
                baseBg = Color.FromArgb(Math.Min(baseBg.R + 25, 255), Math.Min(baseBg.G + 25, 255), Math.Min(baseBg.B + 25, 255))
            End If

            ' Sfondo super opaco e solido
            Using brush As New SolidBrush(baseBg)
                g.FillPath(brush, path)
            End Using

            ' Bordo "Neon" visibile ad alto contrasto (che prende il colore del testo, di solito accentuato)
            Dim penAlpha = If(_isHovered, 255, 120)
            Using pen As New Pen(Color.FromArgb(penAlpha, baseFore), 1.5F)
                g.DrawPath(pen, path)
            End Using
            
            If _isHovered AndAlso Me.Enabled Then
                ' Outer glow su hover
                Using penGlow As New Pen(Color.FromArgb(40, baseFore), 4.0F)
                    g.DrawPath(penGlow, path)
                End Using
            End If
            
            ' Text
            Using font As New Font("Segoe UI", 9.0F, FontStyle.Bold)
                Dim sz = g.MeasureString(Text, font)
                Using b As New SolidBrush(baseFore)
                    g.DrawString(Text, font, b, (Width - sz.Width) / 2, (Height - sz.Height) / 2)
                End Using
            End Using
        End Using
    End Sub
End Class