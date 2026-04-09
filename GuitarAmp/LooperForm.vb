Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices

Public Class LooperForm
    Private currentVol As Integer = 50
    
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HT_CAPTION As Integer = &H2

    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, Msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub LooperForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Me.Owner IsNot Nothing AndAlso TypeOf Me.Owner Is Form1 Then
            Dim mainForm = DirectCast(Me.Owner, Form1)
            currentVol = CInt(mainForm.LooperVol)
            knobVol.Value = currentVol
        End If
        tmrUI.Start()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using path As GraphicsPath = ThemeColors.CreateRoundedRect(New Rectangle(0, 0, Width - 1, Height - 1), 12)
            Using pen As New Pen(Color.FromArgb(50, 50, 60), 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            ReleaseCapture()
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0)
        End If
        MyBase.OnMouseDown(e)
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Hide()
    End Sub

    Private Sub knobVol_ValueChanged(sender As Object, e As EventArgs) Handles knobVol.ValueChanged
        currentVol = knobVol.Value
        If Me.Owner IsNot Nothing AndAlso TypeOf Me.Owner Is Form1 Then
            Dim mainForm = DirectCast(Me.Owner, Form1)
            mainForm.LooperVol = currentVol
            mainForm.ApplySettingsPublic()
        End If
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        If Form1.guitarEffect IsNot Nothing Then
            Form1.guitarEffect.ClearLooper()
        End If
    End Sub
    
    Private Sub btnState_Click(sender As Object, e As EventArgs) Handles btnState.Click
        If Me.Owner IsNot Nothing AndAlso TypeOf Me.Owner Is Form1 Then
            Dim mainForm = DirectCast(Me.Owner, Form1)
            mainForm.ToggleLooperState()
        End If
    End Sub

    Private Sub tmrUI_Tick(sender As Object, e As EventArgs) Handles tmrUI.Tick
        btnState.Invalidate()
        
        If Form1.guitarEffect IsNot Nothing Then
            Dim s = Form1.guitarEffect.LooperState
            Dim hasAudio = Form1.guitarEffect.looperLength > 0
            
            If s = GuitarAmpEffect.LooperStates.Recording Then
                lblState.Text = "REC"
                lblState.ForeColor = Color.FromArgb(255, 60, 60)
            ElseIf s = GuitarAmpEffect.LooperStates.Playing Then
                lblState.Text = "PLAY"
                lblState.ForeColor = Color.FromArgb(60, 255, 100)
            Else
                If hasAudio Then
                    lblState.Text = "STOPPED"
                    lblState.ForeColor = Color.FromArgb(200, 180, 50)
                Else
                    lblState.Text = "EMPTY"
                    lblState.ForeColor = Color.Gray
                End If
            End If
        End If
    End Sub

    Private Sub btnState_Paint(sender As Object, e As PaintEventArgs) Handles btnState.Paint
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        e.Graphics.Clear(Me.BackColor)
        
        Dim r = btnState.ClientRectangle
        r.Inflate(-5, -5)
        
        Dim baseColor As Color = Color.FromArgb(40, 40, 45)
        Dim progressColor As Color = Color.DimGray
        Dim progress As Single = 0.0F
        
        If Form1.guitarEffect IsNot Nothing Then
            Dim s = Form1.guitarEffect.LooperState
            Dim hasAudio = Form1.guitarEffect.looperLength > 0
            
            If s = GuitarAmpEffect.LooperStates.Recording Then
                baseColor = Color.FromArgb(120, 20, 20)
            ElseIf s = GuitarAmpEffect.LooperStates.Playing Then
                baseColor = Color.FromArgb(20, 100, 40)
                progress = Form1.guitarEffect.LooperProgress
            Else
                If hasAudio Then
                    baseColor = Color.FromArgb(100, 80, 20)
                End If
            End If
            
            If s = GuitarAmpEffect.LooperStates.Playing Then
                progressColor = Color.FromArgb(100, 255, 150)
            ElseIf s = GuitarAmpEffect.LooperStates.Recording Then
                progressColor = Color.FromArgb(255, 80, 80)
            End If
        End If

        ' Sfondo bottone
        Using b As New SolidBrush(baseColor)
            g.FillEllipse(b, r)
        End Using
        
        ' Anello progresso
        Using pOutline As New Pen(Color.FromArgb(20, 20, 25), 6)
            r.Inflate(-8, -8)
            g.DrawEllipse(pOutline, r)
            
            If progress > 0 Then
                Using pProg As New Pen(progressColor, 6)
                    pProg.StartCap = LineCap.Round
                    pProg.EndCap = LineCap.Round
                    g.DrawArc(pProg, r, -90, progress * 360.0F)
                End Using
            End If
        End Using
        
        ' Icona Play/Rec (semplificata con testo o simboli)
        ' Draw a small circle in the middle if recording, a triangle if playing
        If Form1.guitarEffect IsNot Nothing Then
            Dim s = Form1.guitarEffect.LooperState
            If s = GuitarAmpEffect.LooperStates.Recording Then
                Dim centerRect As New Rectangle(r.X + r.Width \ 2 - 15, r.Y + r.Height \ 2 - 15, 30, 30)
                Using bRec As New SolidBrush(Color.White)
                    g.FillEllipse(bRec, centerRect)
                End Using
            ElseIf s = GuitarAmpEffect.LooperStates.Playing Then
                Dim cx = r.X + CInt(r.Width / 2)
                Dim cy = r.Y + CInt(r.Height / 2)
                Dim pts() As PointF = {
                    New PointF(cx - 10, cy - 15),
                    New PointF(cx + 15, cy),
                    New PointF(cx - 10, cy + 15)
                }
                Using bPlay As New SolidBrush(Color.White)
                    g.FillPolygon(bPlay, pts)
                End Using
            End If
        End If
    End Sub

    ' Key capture for LooperForm
    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Space Then
            If Me.Owner IsNot Nothing AndAlso TypeOf Me.Owner Is Form1 Then
                Dim mainForm = DirectCast(Me.Owner, Form1)
                mainForm.ToggleLooperState()
            End If
            Return True ' handled
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

End Class
