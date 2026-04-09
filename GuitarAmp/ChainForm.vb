Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices

Public Class ChainForm
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HT_CAPTION As Integer = &H2

    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, Msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function

    Private Class VirtualPedal
        Public FX As GuitarAmpEffect.FXType
        Public Bounds As RectangleF
        Public DrawX As Single
        Public Name As String
        Public AccentColor As Color
        Public IsEnabled As Boolean = True
    End Class

    Private pedals As New List(Of VirtualPedal)
    Private draggingPedal As VirtualPedal = Nothing
    Private dragOffsetX As Single = 0.0F

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub ChainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SyncFromDSP()
        tmrAnim.Start()
        Me.Invalidate()
    End Sub

    Private Sub SyncFromDSP()
        Dim mainForm As Form1 = Nothing
        If Me.Owner IsNot Nothing AndAlso TypeOf Me.Owner Is Form1 Then
            mainForm = DirectCast(Me.Owner, Form1)
        End If
        If mainForm Is Nothing Then Return
        
        pedals.Clear()
        Dim dspChain = mainForm.globalSignalChain
        Dim startX As Single = 20.0F
        Dim pedW As Single = 100.0F
        Dim pedH As Single = 130.0F
        Dim gap As Single = 20.0F

        For i As Integer = 0 To dspChain.Length - 1
            Dim vp As New VirtualPedal()
            vp.FX = dspChain(i)
            vp.Bounds = New RectangleF(startX + i * (pedW + gap), 50.0F, pedW, pedH)
            vp.DrawX = vp.Bounds.X
            Select Case vp.FX
                Case GuitarAmpEffect.FXType.Compressor
                    vp.Name = "COMP"
                    vp.AccentColor = Color.Orange
                Case GuitarAmpEffect.FXType.Drive
                    vp.Name = "DRIVE"
                    vp.AccentColor = Color.Red
                Case GuitarAmpEffect.FXType.AmpCab
                    vp.Name = "AMP"
                    vp.AccentColor = Color.White
                Case GuitarAmpEffect.FXType.Chorus
                    vp.Name = "CHORUS"
                    vp.AccentColor = Color.Magenta
                Case GuitarAmpEffect.FXType.Tremolo
                    vp.Name = "TREM"
                    vp.AccentColor = Color.Lime
                Case GuitarAmpEffect.FXType.Delay
                    vp.Name = "DELAY"
                    vp.AccentColor = Color.DeepSkyBlue
                Case GuitarAmpEffect.FXType.Reverb
                    vp.Name = "REVERB"
                    vp.AccentColor = Color.Cyan
            End Select
            pedals.Add(vp)
        Next
    End Sub

    Private Sub CheckStates(ByRef needsDraw As Boolean)
        Dim mainForm As Form1 = Nothing
        If Me.Owner IsNot Nothing AndAlso TypeOf Me.Owner Is Form1 Then
            mainForm = DirectCast(Me.Owner, Form1)
        End If
        If mainForm Is Nothing Then Return

        For Each p In pedals
            Dim oldState = p.IsEnabled
            Select Case p.FX
                Case GuitarAmpEffect.FXType.Compressor : p.IsEnabled = mainForm.swComp.Checked
                Case GuitarAmpEffect.FXType.Drive : p.IsEnabled = (mainForm.knobDrive.Value > 0)
                Case GuitarAmpEffect.FXType.AmpCab : p.IsEnabled = mainForm.swCabSim.Checked
                Case GuitarAmpEffect.FXType.Chorus : p.IsEnabled = mainForm.swChorus.Checked
                Case GuitarAmpEffect.FXType.Tremolo : p.IsEnabled = mainForm.swTremolo.Checked
                Case GuitarAmpEffect.FXType.Delay : p.IsEnabled = mainForm.swDelay.Checked
                Case GuitarAmpEffect.FXType.Reverb : p.IsEnabled = mainForm.swReverb.Checked
            End Select
            If oldState <> p.IsEnabled Then
                needsDraw = True
            End If
        Next
    End Sub

    Protected Overrides Sub OnPaintBackground(e As PaintEventArgs)
        ' Evita flickering
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(Me.BackColor)
        
        Using path As GraphicsPath = ThemeColors.CreateRoundedRect(New Rectangle(0, 0, Width - 1, Height - 1), 12)
            Using pen As New Pen(Color.FromArgb(50, 50, 60), 1.0F)
                g.DrawPath(pen, path)
            End Using
        End Using

        ' Draw connections
        Using pLine As New Pen(Color.FromArgb(60, 60, 65), 4)
            pLine.StartCap = LineCap.Round
            pLine.EndCap = LineCap.Round
            g.DrawLine(pLine, 20, 115, Width - 20, 115)
        End Using

        ' Draw inactive first
        For Each p In pedals
            If p Is draggingPedal Then Continue For
            DrawPedal(g, p)
        Next
        
        ' Draw dragging last so it stays on top
        If draggingPedal IsNot Nothing Then
            DrawPedal(g, draggingPedal)
        End If
    End Sub

    Private Sub DrawPedal(g As Graphics, p As VirtualPedal)
        Dim r = New Rectangle(CInt(p.DrawX), CInt(p.Bounds.Y), CInt(p.Bounds.Width), CInt(p.Bounds.Height))
        Dim isDrag = (p Is draggingPedal)
        If isDrag Then
            r.Inflate(4, 4)
        End If

        Dim bodyColor = Color.FromArgb(25, 25, 30)
        Dim edgeColor = Color.FromArgb(45, 45, 55)

        Using path = ThemeColors.CreateRoundedRect(r, 6)
            Using b As New SolidBrush(bodyColor)
                g.FillPath(b, path)
            End Using
            Using pen As New Pen(edgeColor, If(isDrag, 2.0F, 1.0F))
                g.DrawPath(pen, path)
            End Using
        End Using

        ' Accent strip
        Dim stripRect As New Rectangle(r.X + 2, r.Y + 2, r.Width - 4, 8)
        Using sb As New SolidBrush(p.AccentColor)
            g.FillRectangle(sb, stripRect)
        End Using

        ' LED
        Dim ledColor = If(p.IsEnabled, p.AccentColor, Color.FromArgb(30, 30, 40))
        Dim ledRect As New Rectangle(r.X + r.Width \ 2 - 4, r.Y + 15, 8, 8)
        Using br As New SolidBrush(ledColor)
            g.FillEllipse(br, ledRect)
        End Using

        ' Name
        Using sf As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
            Dim fontColor = If(p.IsEnabled, Color.White, Color.Gray)
            Using f As New Font("Segoe UI", 10, FontStyle.Bold)
                Using tb As New SolidBrush(fontColor)
                    g.DrawString(p.Name, f, tb, New RectangleF(r.X, r.Y + 30, r.Width, 30), sf)
                End Using
            End Using
        End Using
        
        ' Decor knobs
        Dim decRect = New Rectangle(r.X + 15, r.Y + 70, 20, 20)
        g.FillEllipse(Brushes.DimGray, decRect)
        decRect.X = r.X + r.Width - 35
        g.FillEllipse(Brushes.DimGray, decRect)

        ' Footswitch
        Dim fsRect = New Rectangle(r.X + r.Width \ 2 - 12, r.Y + 95, 24, 24)
        g.FillEllipse(Brushes.Silver, fsRect)
        g.DrawEllipse(Pens.Black, fsRect)
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            If e.Y < 40 Then
                ReleaseCapture()
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0)
                Return
            End If

            For Each p In pedals
                Dim pRect = New RectangleF(p.DrawX, p.Bounds.Y, p.Bounds.Width, p.Bounds.Height)
                If pRect.Contains(e.Location) Then
                    draggingPedal = p
                    dragOffsetX = e.X - p.DrawX
                    Exit For
                End If
            Next
        End If
        MyBase.OnMouseDown(e)
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        If draggingPedal IsNot Nothing AndAlso e.Button = MouseButtons.Left Then
            draggingPedal.DrawX = e.X - dragOffsetX
            Dim pedW As Single = 100.0F
            Dim gap As Single = 20.0F
            Dim minX As Single = 20.0F
            Dim maxX As Single = minX + (pedW + gap) * (pedals.Count - 1)
            If draggingPedal.DrawX < minX Then draggingPedal.DrawX = minX
            If draggingPedal.DrawX > maxX Then draggingPedal.DrawX = maxX
        End If
        MyBase.OnMouseMove(e)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        If draggingPedal IsNot Nothing Then
            draggingPedal = Nothing
            ResortPedals()
        End If
        MyBase.OnMouseUp(e)
    End Sub

    Private Sub ResortPedals()
        pedals.Sort(Function(a, b) a.DrawX.CompareTo(b.DrawX))
        
        ' Update bounds
        Dim startX As Single = 20.0F
        Dim pedW As Single = 100.0F
        Dim gap As Single = 20.0F
        
        For i As Integer = 0 To pedals.Count - 1
            pedals(i).Bounds = New RectangleF(startX + i * (pedW + gap), 50.0F, pedW, 130.0F)
        Next
        
        ' Push to DSP
        Dim mainForm As Form1 = Nothing
        If Me.Owner IsNot Nothing AndAlso TypeOf Me.Owner Is Form1 Then
            mainForm = DirectCast(Me.Owner, Form1)
        End If

        If mainForm IsNot Nothing Then
            Dim newArr(6) As GuitarAmpEffect.FXType
            For i As Integer = 0 To 6
                newArr(i) = pedals(i).FX
            Next
            mainForm.globalSignalChain = newArr
            If mainForm.guitarEffect IsNot Nothing Then
                mainForm.guitarEffect.SignalChain = CType(newArr.Clone(), GuitarAmpEffect.FXType())
            End If
        End If
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Hide()
    End Sub

    Private Sub tmrAnim_Tick(sender As Object, e As EventArgs) Handles tmrAnim.Tick
        Dim needsDraw = False
        CheckStates(needsDraw)
        
        For Each p In pedals
            If p IsNot draggingPedal Then
                If Math.Abs(p.DrawX - p.Bounds.X) > 1.0F Then
                    p.DrawX += (p.Bounds.X - p.DrawX) * 0.3F
                    needsDraw = True
                Else
                    p.DrawX = p.Bounds.X
                End If
            Else
                needsDraw = True ' Update dragging state
            End If
        Next
        
        ' Always draw on first ticks if not requested yet, or we force it periodically?
        ' VB.NET double buffered controls don't strictly require manual invalidation unless animating,
        ' but we'll force invalidate initially by putting Me.Invalidate()
        If needsDraw Then
            Me.Invalidate()
        End If
    End Sub
End Class
