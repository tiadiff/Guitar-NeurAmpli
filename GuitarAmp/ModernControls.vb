Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

' --- MANOPOLA ROTATIVA (KNOB) ---
Public Class RockKnob
    Inherits Control

    ' Variabile privata per gestire il valore
    Private _value As Integer = 0

    Public Property Maximum As Integer = 10
    Public Property Minimum As Integer = 0
    Public Property AccentColor As Color = Color.Gold
    Public Property KnobText As String = ""

    ' MODIFICA CRUCIALE: Il Setter ora lancia l'evento e ridisegna
    Public Property Value As Integer
        Get
            Return _value
        End Get
        Set(val As Integer)
            If val > Maximum Then val = Maximum
            If val < Minimum Then val = Minimum

            ' Se il valore cambia, aggiorna grafica e audio
            If _value <> val Then
                _value = val
                Me.Invalidate() ' Ridisegna
                RaiseEvent ValueChanged(Me, EventArgs.Empty) ' Avvisa Form1
            End If
        End Set
    End Property

    Private isDragging As Boolean = False
    Private lastY As Integer = 0

    Public Sub New()
        Me.Size = New Size(60, 80)
        Me.DoubleBuffered = True
        Me.ForeColor = Color.White
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        isDragging = True : lastY = e.Y
    End Sub
    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        isDragging = False
    End Sub
    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        If isDragging Then
            Dim delta = lastY - e.Y
            If Math.Abs(delta) > 2 Then
                ' Modifichiamo la Property pubblica, che farà scattare l'evento
                Value += Math.Sign(delta)
                lastY = e.Y
            End If
        End If
    End Sub

    Public Event ValueChanged As EventHandler

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim knobRect As New Rectangle(5, 5, Width - 11, Width - 11)
        g.FillEllipse(New SolidBrush(Color.FromArgb(40, 40, 40)), knobRect)
        g.DrawEllipse(New Pen(Color.FromArgb(20, 20, 20), 2), knobRect)

        Dim startAngle As Single = 135
        Dim sweepAngle As Single = 270
        Dim percent As Single = CSng(_value - Minimum) / (Maximum - Minimum)
        Dim currentSweep As Single = sweepAngle * percent

        Using penBg As New Pen(Color.FromArgb(60, 60, 60), 4)
            g.DrawArc(penBg, 10, 10, Width - 21, Width - 21, startAngle, sweepAngle)
        End Using
        Using penVal As New Pen(AccentColor, 4)
            g.DrawArc(penVal, 10, 10, Width - 21, Width - 21, startAngle, currentSweep)
        End Using

        Dim strVal As String = _value.ToString()
        Dim strSize = g.MeasureString(strVal, Me.Font)
        g.DrawString(strVal, New Font(Me.Font.FontFamily, 8, FontStyle.Bold), Brushes.White, (Width - strSize.Width) / 2, (Width / 2) - 6)

        Dim titleSize = g.MeasureString(KnobText, Me.Font)
        g.DrawString(KnobText, Me.Font, Brushes.Silver, (Width - titleSize.Width) / 2, Height - 18)
    End Sub
End Class

' --- INTERRUTTORE MODERNO (SWITCH) ---
Public Class RockSwitch
    Inherits Control

    Private _checked As Boolean = False
    Public Property CheckedColor As Color = Color.Lime
    Public Property LabelText As String = "SWITCH"

    ' MODIFICA CRUCIALE: Setter con evento
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
    End Sub

    Protected Overrides Sub OnClick(e As EventArgs)
        Checked = Not Checked ' Questo attiverà il Setter sopra
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim bgRect As New Rectangle(0, 0, Width - 1, Height - 1)
        Dim bgColor As Color = If(_checked, Color.FromArgb(40, 40, 40), Color.FromArgb(20, 20, 20))
        Dim borderColor As Color = If(_checked, CheckedColor, Color.Gray)

        Using brush As New SolidBrush(bgColor)
            g.FillRectangle(brush, bgRect)
        End Using
        Using pen As New Pen(borderColor, 1)
            g.DrawRectangle(pen, bgRect)
        End Using

        Dim ledRect As New Rectangle(10, 8, 14, 14)
        Dim ledColor As Color = If(_checked, CheckedColor, Color.FromArgb(50, 50, 50))
        Using brush As New SolidBrush(ledColor)
            g.FillEllipse(brush, ledRect)
        End Using
        If _checked Then
            g.FillEllipse(Brushes.White, 12, 10, 4, 4)
            Using pen As New Pen(Color.FromArgb(100, CheckedColor), 4)
                g.DrawEllipse(pen, ledRect)
            End Using
        End If

        Dim txtBrush As Brush = If(_checked, Brushes.White, Brushes.Gray)
        Dim fontSize = g.MeasureString(LabelText, Me.Font)
        g.DrawString(LabelText, Me.Font, txtBrush, 35, (Height - fontSize.Height) / 2)
    End Sub
End Class