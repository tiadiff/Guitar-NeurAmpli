Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices

Public Class MetronomeForm
    Private currentBpm As Integer = 120
    Private currentVol As Integer = 50
    Private isRunning As Boolean = False
    
    Private beatFlashTimer As Single = 0.0F

    ' Drag Support
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

    Private Sub MetronomeForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        UpdateBPMDisplay()
        
        ' Sync with Form1 settings
        If Me.Owner IsNot Nothing AndAlso TypeOf Me.Owner Is Form1 Then
            Dim mainForm = DirectCast(Me.Owner, Form1)
            currentBpm = CInt(mainForm.MetronomeBpm)
            currentVol = CInt(mainForm.MetronomeVol)
            isRunning = mainForm.isMetronomeON
            
            knobBPM.Value = currentBpm
            knobVol.Value = currentVol
            swMetronome.Checked = isRunning
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

    Private Sub btnBpmDown_Click(sender As Object, e As EventArgs) Handles btnBpmDown.Click
        ChangeBPM(-1)
    End Sub

    Private Sub btnBpmUp_Click(sender As Object, e As EventArgs) Handles btnBpmUp.Click
        ChangeBPM(1)
    End Sub

    Private Sub ChangeBPM(delta As Integer)
        Dim newBpm = currentBpm + delta
        If newBpm >= knobBPM.Minimum AndAlso newBpm <= knobBPM.Maximum Then
            currentBpm = newBpm
            knobBPM.Value = currentBpm
            UpdateBPMDisplay()
            PushSettings()
        End If
    End Sub

    Private Sub knobBPM_ValueChanged(sender As Object, e As EventArgs) Handles knobBPM.ValueChanged
        currentBpm = knobBPM.Value
        UpdateBPMDisplay()
        PushSettings()
    End Sub

    Private Sub UpdateBPMDisplay()
        lblBPMDisplay.Text = currentBpm.ToString()
    End Sub

    Private Sub knobVol_ValueChanged(sender As Object, e As EventArgs) Handles knobVol.ValueChanged
        currentVol = knobVol.Value
        PushSettings()
    End Sub

    Private Sub swMetronome_CheckedChanged(sender As Object, e As EventArgs) Handles swMetronome.CheckedChanged
        isRunning = swMetronome.Checked
        PushSettings()
    End Sub

    Private Sub PushSettings()
        If Me.Owner IsNot Nothing AndAlso TypeOf Me.Owner Is Form1 Then
            Dim mainForm = DirectCast(Me.Owner, Form1)
            mainForm.isMetronomeON = isRunning
            mainForm.MetronomeBpm = currentBpm
            mainForm.MetronomeVol = currentVol
            mainForm.ApplySettingsPublic()
        End If
    End Sub

    ' Beat Flasher
    Private Sub tmrUI_Tick(sender As Object, e As EventArgs) Handles tmrUI.Tick
        If Not isRunning Then
            picBeat.BackColor = Color.Transparent
            picBeat.Invalidate()
            Return
        End If
        
        beatFlashTimer += tmrUI.Interval
        Dim msPerBeat = 60000.0F / currentBpm
        If beatFlashTimer >= msPerBeat Then
            beatFlashTimer = CSng(beatFlashTimer Mod msPerBeat)
        End If

        If beatFlashTimer < 80.0F Then ' Flash length 80ms
            picBeat.BackColor = ThemeColors.AccentAmber
        Else
            picBeat.BackColor = ThemeColors.BgDeep
        End If
        picBeat.Invalidate()
    End Sub
    
    Private Sub picBeat_Paint(sender As Object, e As PaintEventArgs) Handles picBeat.Paint
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        e.Graphics.Clear(Me.BackColor)
        
        Dim r = picBeat.ClientRectangle
        If r.Width <= 0 OrElse r.Height <= 0 Then Return
        r.Width -= 1 : r.Height -= 1
        
        Using b As New SolidBrush(picBeat.BackColor)
            g.FillEllipse(b, r)
        End Using
        
        Using p As New Pen(Color.FromArgb(60, 60, 68), 1)
            g.DrawEllipse(p, r)
        End Using
    End Sub
End Class
