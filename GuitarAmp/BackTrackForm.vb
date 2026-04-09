Imports System.IO
Imports NAudio.Wave
Imports NAudio.Wave.SampleProviders
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices

Public Class BackTrackForm
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HT_CAPTION As Integer = &H2

    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, Msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function

    Private audioReader As AudioFileReader
    Private resampler As MediaFoundationResampler
    Private btVolume As VolumeSampleProvider
    Private isPlaying As Boolean = False
    Private loadedPath As String = ""

    Public Sub New()
        InitializeComponent()
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
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        If e.Button = MouseButtons.Left AndAlso e.Y < 40 Then
            ReleaseCapture()
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0)
        End If
        MyBase.OnMouseDown(e)
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Hide()
    End Sub

    Private Sub btnLoad_Click(sender As Object, e As EventArgs) Handles btnLoad.Click
        Using ofd As New OpenFileDialog()
            ofd.Title = "Select Backing Track"
            ofd.Filter = "Music Files (*.mp3;*.wav;*.aiff)|*.mp3;*.wav;*.aiff|All Files (*.*)|*.*"
            If ofd.ShowDialog() = DialogResult.OK Then
                loadedPath = ofd.FileName
                lblPath.Text = Path.GetFileName(loadedPath)
                lblPath.ForeColor = Color.White
                
                StopPlayback()
                btnPlay.Enabled = True
                btnPlay.Text = "PLAY"
            End If
        End Using
    End Sub

    Private Sub btnPlay_Click(sender As Object, e As EventArgs) Handles btnPlay.Click
        If isPlaying Then
            ' Pause
            If btVolume IsNot Nothing AndAlso Form1.masterMixer IsNot Nothing Then
                Form1.masterMixer.RemoveMixerInput(btVolume)
            End If
            isPlaying = False
            btnPlay.Text = "RESUME"
            Return
        End If

        Try
            If Form1.masterMixer Is Nothing Then
                MessageBox.Show("Please START the Amplifier first before playing a backing track.")
                Return
            End If

            If audioReader Is Nothing Then
                audioReader = New AudioFileReader(loadedPath)
                
                ' Passaggio 1: Ricampioniamo a 192000Hz usando gli stessi canali originali.
                resampler = New MediaFoundationResampler(audioReader, New WaveFormat(192000, audioReader.WaveFormat.Channels))
                resampler.ResamplerQuality = 60

                Dim resampledFloat = resampler.ToSampleProvider()

                ' Passaggio 2: Convertiamo in Mono (Target del MasterBus è 192000 1-ch)
                Dim finalMono As ISampleProvider
                If audioReader.WaveFormat.Channels = 2 Then
                    Dim monoNode = New StereoToMonoSampleProvider(resampledFloat)
                    monoNode.LeftVolume = 0.5F
                    monoNode.RightVolume = 0.5F
                    finalMono = monoNode
                Else
                    finalMono = resampledFloat
                End If

                ' Passaggio 3: Controllo Volume
                btVolume = New VolumeSampleProvider(finalMono)
                btVolume.Volume = CSng(knobVol.Value) / 100.0F
            End If

            Form1.masterMixer.AddMixerInput(btVolume)
            isPlaying = True
            btnPlay.Text = "PAUSE"
            btnStop.Enabled = True

        Catch ex As Exception
            MessageBox.Show("Errore caricamento traccia: " & ex.Message)
            StopPlayback()
        End Try
    End Sub

    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        StopPlayback()
        If loadedPath <> "" Then
            btnPlay.Enabled = True
            btnPlay.Text = "PLAY"
        End If
    End Sub

    Private Sub StopPlayback()
        If Form1.masterMixer IsNot Nothing AndAlso btVolume IsNot Nothing Then
            Form1.masterMixer.RemoveMixerInput(btVolume)
        End If
        
        If resampler IsNot Nothing Then resampler.Dispose()
        If audioReader IsNot Nothing Then audioReader.Dispose()
        
        resampler = Nothing
        audioReader = Nothing
        btVolume = Nothing
        isPlaying = False
        btnStop.Enabled = False
    End Sub

    Private Sub knobVol_ValueChanged(sender As Object, e As EventArgs) Handles knobVol.ValueChanged
        If btVolume IsNot Nothing Then
            btVolume.Volume = CSng(knobVol.Value) / 100.0F
        End If
    End Sub

    Private Sub BackTrackForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Me.Hide()
        End If
    End Sub
End Class
