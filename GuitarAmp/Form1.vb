Imports NAudio.Wave
Imports NAudio.CoreAudioApi
Imports NAudio.Dsp
Imports System.Drawing

Public Class Form1
    Private audioInput As WaveInEvent
    Private audioOutput As WasapiOut
    Private bufferedProvider As BufferedWaveProvider
    Private guitarEffect As GuitarAmpEffect
    Private currentPeakLevel As Single = 0.0F
    Private isRecording As Boolean = False
    Private isUpdatingPreset As Boolean = False
    Private recSeconds As Integer = 0

    Public Const WM_NCLBUTTONDOWN As Integer = &HA1
    Public Const HT_CAPTION As Integer = &H2

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Public Shared Function SendMessage(hWnd As IntPtr, Msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Public Shared Function ReleaseCapture() As Boolean
    End Function


    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        For i As Integer = 0 To WaveIn.DeviceCount - 1
            cmbInput.Items.Add(WaveIn.GetCapabilities(i).ProductName)
        Next
        If cmbInput.Items.Count > 0 Then cmbInput.SelectedIndex = 0
        LoadPresetClean()
    End Sub

    ' --- AUDIO ENGINE (Ottimizzato Bassa Latenza) ---
    Private Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        Try
            If cmbInput.SelectedIndex < 0 Then Return
            audioInput = New WaveInEvent()
            audioInput.DeviceNumber = cmbInput.SelectedIndex
            audioInput.WaveFormat = New WaveFormat(192000, 1)
            ' Ripristino buffer a 20ms (Sicurezza contro starvation di I/O MME di Windows)
            audioInput.BufferMilliseconds = 20
            audioInput.NumberOfBuffers = 3
            
            ' ReadFully = True è CRITICO per WasapiOut Exclusive, altrimenti alla prima carenza di dati lo stream muore in un Muta permanente.
            bufferedProvider = New BufferedWaveProvider(audioInput.WaveFormat) With {.DiscardOnBufferOverflow = True, .ReadFully = True}

            AddHandler audioInput.DataAvailable, Sub(s, a)
                                                     bufferedProvider.AddSamples(a.Buffer, 0, a.BytesRecorded)
                                                     ' La soglia di reset deve essere > della dimensione del pacchetto (20ms * 2 = 40ms)
                                                     If bufferedProvider.BufferedDuration.TotalMilliseconds > 40 Then bufferedProvider.ClearBuffer()
                                                 End Sub

            guitarEffect = New GuitarAmpEffect(bufferedProvider.ToSampleProvider())
            ApplySettings()

            ' Scelta tra WASAPI Esclusivo (latenza 10ms) e Condiviso (15ms, permette audio app)
            Dim shareMode = If(swExclusive.Checked, AudioClientShareMode.Exclusive, AudioClientShareMode.Shared)
            Dim latencyMode = If(swExclusive.Checked, 10, 15)
            audioOutput = New WasapiOut(shareMode, latencyMode)
            audioOutput.Init(guitarEffect)
            audioInput.StartRecording()
            audioOutput.Play()

            btnStart.Enabled = False : btnStop.Enabled = True
        Catch ex As Exception
            MessageBox.Show("Errore Audio: " & ex.Message)
        End Try
    End Sub

    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        audioInput?.StopRecording() : audioOutput?.Stop()
        audioInput?.Dispose() : audioOutput?.Dispose()
        audioInput = Nothing : audioOutput = Nothing
        btnStart.Enabled = True : btnStop.Enabled = False
        picVuMeter.Invalidate()
    End Sub

    Private Sub btnRec_Click(sender As Object, e As EventArgs) Handles btnRec.Click
        If guitarEffect Is Nothing Then Return
        If Not isRecording Then
            guitarEffect.StartRecording("Rec.wav")
            isRecording = True
            btnRec.BackColor = Color.Red
            recSeconds = 0
            Timer1.Interval = 1000
            Timer1.Start()
        Else
            guitarEffect.StopRecording()
            isRecording = False
            btnRec.BackColor = Color.Orange
            Timer1.Stop()
        End If
    End Sub

    ' --- PARAMETRI ---
    Private Sub ApplySettings()
        If guitarEffect Is Nothing Then Return

        guitarEffect.GateThreshold = CSng(knobGate.Value) / 100.0F
        guitarEffect.Volume = CSng(knobVol.Value) / 10.0F
        guitarEffect.Drive = CSng(knobDrive.Value)
        guitarEffect.BassGain = CSng(knobBass.Value)
        guitarEffect.MidGain = CSng(knobMid.Value)
        guitarEffect.TrebleGain = CSng(knobTreble.Value)

        guitarEffect.CompressorEnabled = swComp.Checked
        guitarEffect.ChorusEnabled = swChorus.Checked
        guitarEffect.DelayEnabled = swDelay.Checked
        guitarEffect.TremoloEnabled = swTremolo.Checked
        guitarEffect.ReverbEnabled = swReverb.Checked
        guitarEffect.EnableCabSim = swCabSim.Checked

        guitarEffect.UpdateFilters()
    End Sub

    ' Collega gli eventi dei nuovi controlli custom
    Private Sub Controls_Changed(sender As Object, e As EventArgs) Handles _
        knobGate.ValueChanged, knobVol.ValueChanged, knobDrive.ValueChanged,
        knobBass.ValueChanged, knobMid.ValueChanged, knobTreble.ValueChanged,
        swComp.CheckedChanged, swChorus.CheckedChanged, swDelay.CheckedChanged,
        swTremolo.CheckedChanged, swReverb.CheckedChanged, swCabSim.CheckedChanged

        If Not isUpdatingPreset Then
            ApplySettings()
        End If
    End Sub

    Private Sub swExclusive_CheckedChanged(sender As Object, e As EventArgs) Handles swExclusive.CheckedChanged
        ' Se cambia modalità a runtime, riavviamo l'audio in automatico
        If audioOutput IsNot Nothing AndAlso btnStop.Enabled Then
            btnStop.PerformClick()
            btnStart.PerformClick()
        End If
    End Sub

    ' --- PRESETS ---
    Private Sub btnClean_Click(sender As Object, e As EventArgs) Handles btnClean.Click
        LoadPresetClean()
    End Sub

    Private Sub btnCrunch_Click(sender As Object, e As EventArgs) Handles btnCrunch.Click
        isUpdatingPreset = True
        knobDrive.Value = 4 : knobBass.Value = 2 : knobMid.Value = 2 : knobTreble.Value = 1
        swComp.Checked = False : swChorus.Checked = False : swDelay.Checked = False
        swTremolo.Checked = False : swReverb.Checked = True : swCabSim.Checked = True
        isUpdatingPreset = False
        ApplySettings()
    End Sub

    Private Sub btnMetal_Click(sender As Object, e As EventArgs) Handles btnMetal.Click
        isUpdatingPreset = True
        knobDrive.Value = 10 : knobBass.Value = 5 : knobMid.Value = -4 : knobTreble.Value = 5
        swComp.Checked = True : swChorus.Checked = False : swDelay.Checked = True
        swTremolo.Checked = False : swReverb.Checked = False : swCabSim.Checked = True
        isUpdatingPreset = False
        ApplySettings()
    End Sub

    Private Sub LoadPresetClean()
        isUpdatingPreset = True
        knobDrive.Value = 0 : knobBass.Value = 0 : knobMid.Value = 0 : knobTreble.Value = 0
        swComp.Checked = True : swChorus.Checked = True : swDelay.Checked = True
        swTremolo.Checked = False : swReverb.Checked = True : swCabSim.Checked = True
        isUpdatingPreset = False
        ApplySettings()
    End Sub

    ' --- VISUALS ---
    Private Sub tmrVisuals_Tick(sender As Object, e As EventArgs) Handles tmrVisuals.Tick
        If guitarEffect IsNot Nothing Then
            Dim target = guitarEffect.CurrentPeak
            currentPeakLevel = If(target > currentPeakLevel, target, currentPeakLevel - 0.1F)
            If currentPeakLevel < 0 Then currentPeakLevel = 0
            picVuMeter.Invalidate()
        End If
    End Sub

    Private Sub picVuMeter_Paint(sender As Object, e As PaintEventArgs) Handles picVuMeter.Paint
        Dim g = e.Graphics
        Dim width = CInt(picVuMeter.Width * currentPeakLevel)
        g.FillRectangle(Brushes.Black, picVuMeter.ClientRectangle)
        If width > 0 Then
            Using b As New Drawing2D.LinearGradientBrush(New Rectangle(0, 0, width, picVuMeter.Height), Color.Lime, Color.Red, 0.0F)
                g.FillRectangle(b, 0, 0, width, picVuMeter.Height)
            End Using
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.Close()
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        btnStop.PerformClick()
    End Sub

    Private Sub Window_MouseDown(sender As Object, e As MouseEventArgs) Handles MyBase.MouseDown, pnlMain.MouseDown
        If e.Button = MouseButtons.Left Then
            ReleaseCapture()
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0)
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.WindowState = FormWindowState.Minimized
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If isRecording Then
            recSeconds += 1
            Dim ts As TimeSpan = TimeSpan.FromSeconds(recSeconds)
            Label3.Text = "REC: " & ts.ToString("mm\:ss")
        End If
    End Sub
End Class