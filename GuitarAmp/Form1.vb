Imports NAudio.Wave
Imports NAudio.CoreAudioApi
Imports NAudio.Dsp
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO

Public Class Form1
    Private audioInput As WaveInEvent
    Private audioOutput As WasapiOut
    Private bufferedProvider As BufferedWaveProvider
    Public guitarEffect As GuitarAmpEffect
    Private currentPeakLevel As Single = 0.0F
    Private peakHoldLevel As Single = 0.0F
    Private peakHoldFrames As Integer = 0
    Private isRecording As Boolean = False
    Private isUpdatingPreset As Boolean = False
    Private recSeconds As Integer = 0

    ' --- MIXER ---
    Public masterMixer As SampleProviders.MixingSampleProvider

    ' --- FX TWEAKER ---
    Private currentSelectedFX As String = ""

    ' --- METRONOME ---
    Public isMetronomeON As Boolean = False
    Public MetronomeBpm As Single = 120.0F
    Public MetronomeVol As Single = 50.0F
    Private metronomeForm As MetronomeForm

    ' --- LOOPER ---
    Public LooperVol As Single = 100.0F
    Private looperForm As LooperForm

    ' --- SIGNAL CHAIN ---
    Private chainForm As ChainForm
    Public globalSignalChain() As GuitarAmpEffect.FXType = {
        GuitarAmpEffect.FXType.Compressor,
        GuitarAmpEffect.FXType.Drive,
        GuitarAmpEffect.FXType.AmpCab,
        GuitarAmpEffect.FXType.Chorus,
        GuitarAmpEffect.FXType.Tremolo,
        GuitarAmpEffect.FXType.Delay,
        GuitarAmpEffect.FXType.Reverb
    }

    ' --- BACKING TRACK ---
    Private backTrackForm As BackTrackForm

    ' --- FX STATE VARIABLES (Source of Truth) ---
    Private fxChorusRate As Single = 1.0F
    Private fxChorusDepth As Single = 0.5F
    Private fxDelayTime As Single = 350.0F
    Private fxDelayFeedback As Single = 0.45F
    Private fxDelayMix As Single = 0.40F
    Private fxTremoloRate As Single = 5.0F
    Private fxTremoloDepth As Single = 0.4F
    Private fxReverbMix As Single = 0.35F
    Private fxReverbDecay As Single = 0.75F
    Private fxCompThreshold As Single = 0.5F
    Private fxCompRatio As Single = 4.0F

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
        ' Collega gli eventi SelectClicked degli switch FX (non gestibili dal Designer)
        AddHandler swComp.SelectClicked, AddressOf FX_SelectClicked
        AddHandler swChorus.SelectClicked, AddressOf FX_SelectClicked
        AddHandler swDelay.SelectClicked, AddressOf FX_SelectClicked
        AddHandler swTremolo.SelectClicked, AddressOf FX_SelectClicked
        AddHandler swReverb.SelectClicked, AddressOf FX_SelectClicked

        ' Right-click sui pulsanti USR = salva preset
        AddHandler btnUsr1.MouseDown, AddressOf UsrPreset_MouseDown
        AddHandler btnUsr2.MouseDown, AddressOf UsrPreset_MouseDown
        AddHandler btnUsr3.MouseDown, AddressOf UsrPreset_MouseDown

        ' Aggiorna testo pulsanti se il preset esiste già
        UpdateUsrButtonLabels()

        For i As Integer = 0 To WaveIn.DeviceCount - 1
            cmbInput.Items.Add(WaveIn.GetCapabilities(i).ProductName)
        Next
        If cmbInput.Items.Count > 0 Then cmbInput.SelectedIndex = 0
        LoadPresetClean()

        ' Forza il riposizionamento dell'intero "rack" verso sinistra per far stare il Tuner a 1080p
        ' Larghezza totale rack ~1740. StartX per centrare ~90px.
        Me.StartPosition = FormStartPosition.Manual
        Dim screenArea = Screen.PrimaryScreen.WorkingArea
        Dim rackStartX = (screenArea.Width - 1740) \ 2 ' Circa 90px su monitor 1080p
        ' Alziamo la Y (togliendo l'offset positivo) per non toccare la taskbar in basso
        Me.Location = New Point(rackStartX + 276 + 8, (screenArea.Height - Me.Height) \ 2 - 20)
    End Sub

    Private Sub FX_SelectClicked(sender As Object, e As EventArgs)
        Dim sw = DirectCast(sender, RockSwitch)
        
        ' 1. Spegni le selezioni di tutti gli altri
        swComp.IsSelected = (sw Is swComp)
        swChorus.IsSelected = (sw Is swChorus)
        swDelay.IsSelected = (sw Is swDelay)
        swTremolo.IsSelected = (sw Is swTremolo)
        swReverb.IsSelected = (sw Is swReverb)

        ' 2. Aggiorna il pannello
        currentSelectedFX = sw.Name
        UpdateFXPanel(currentSelectedFX)
    End Sub

    Private Sub UpdateFXPanel(fxName As String)
        If knobFX1 Is Nothing Then Return

        knobFX1.Visible = False : knobFX2.Visible = False : knobFX3.Visible = False
        lblFXTitle.Text = ""

        Select Case fxName
            Case "swChorus"
                lblFXTitle.Text = "CHORUS SETTINGS"
                lblFXTitle.ForeColor = swChorus.CheckedColor
                knobFX1.AccentColor = swChorus.CheckedColor
                knobFX2.AccentColor = swChorus.CheckedColor
                knobFX1.KnobText = "RATE"
                knobFX1.Minimum = 1 : knobFX1.Maximum = 50 ' 0.1Hz - 5Hz
                knobFX1.Value = CInt(fxChorusRate * 10)
                knobFX1.Visible = True
                knobFX2.KnobText = "DEPTH"
                knobFX2.Minimum = 0 : knobFX2.Maximum = 100 ' 0 - 1.0
                knobFX2.Value = CInt(fxChorusDepth * 100)
                knobFX2.Visible = True

            Case "swDelay"
                lblFXTitle.Text = "TAPE DELAY"
                lblFXTitle.ForeColor = swDelay.CheckedColor
                knobFX1.AccentColor = swDelay.CheckedColor
                knobFX2.AccentColor = swDelay.CheckedColor
                knobFX3.AccentColor = swDelay.CheckedColor
                knobFX1.KnobText = "TIME"
                knobFX1.Minimum = 50 : knobFX1.Maximum = 1000 ' 50ms - 1000ms
                knobFX1.Value = CInt(fxDelayTime)
                knobFX1.Visible = True
                knobFX2.KnobText = "FEEDBACK"
                knobFX2.Minimum = 0 : knobFX2.Maximum = 95
                knobFX2.Value = CInt(fxDelayFeedback * 100)
                knobFX2.Visible = True
                knobFX3.KnobText = "MIX"
                knobFX3.Minimum = 0 : knobFX3.Maximum = 100
                knobFX3.Value = CInt(fxDelayMix * 100)
                knobFX3.Visible = True

            Case "swTremolo"
                lblFXTitle.Text = "TREMOLO OPTIC"
                lblFXTitle.ForeColor = swTremolo.CheckedColor
                knobFX1.AccentColor = swTremolo.CheckedColor
                knobFX2.AccentColor = swTremolo.CheckedColor
                knobFX1.KnobText = "RATE"
                knobFX1.Minimum = 10 : knobFX1.Maximum = 150 ' 1.0Hz - 15Hz
                knobFX1.Value = CInt(fxTremoloRate * 10)
                knobFX1.Visible = True
                knobFX2.KnobText = "DEPTH"
                knobFX2.Minimum = 0 : knobFX2.Maximum = 100
                knobFX2.Value = CInt(fxTremoloDepth * 100)
                knobFX2.Visible = True

            Case "swReverb"
                lblFXTitle.Text = "ROOM REVERB"
                lblFXTitle.ForeColor = swReverb.CheckedColor
                knobFX1.AccentColor = swReverb.CheckedColor
                knobFX2.AccentColor = swReverb.CheckedColor
                knobFX1.KnobText = "MIX"
                knobFX1.Minimum = 0 : knobFX1.Maximum = 100
                knobFX1.Value = CInt(fxReverbMix * 100)
                knobFX1.Visible = True
                knobFX2.KnobText = "DECAY"
                knobFX2.Minimum = 0 : knobFX2.Maximum = 95
                knobFX2.Value = CInt(fxReverbDecay * 100)
                knobFX2.Visible = True

            Case "swComp"
                lblFXTitle.Text = "COMPRESSOR"
                lblFXTitle.ForeColor = swComp.CheckedColor
                knobFX1.AccentColor = swComp.CheckedColor
                knobFX2.AccentColor = swComp.CheckedColor
                knobFX1.KnobText = "THRESH"
                knobFX1.Minimum = 0 : knobFX1.Maximum = 100
                knobFX1.Value = CInt(fxCompThreshold * 100)
                knobFX1.Visible = True
                knobFX2.KnobText = "RATIO"
                knobFX2.Minimum = 10 : knobFX2.Maximum = 100 ' 1:1 - 10:1
                knobFX2.Value = CInt(fxCompRatio * 10)
                knobFX2.Visible = True
        End Select
    End Sub

    ' Propaga le modifiche al motore DSP (Evita loop di binding ignorando se aggiorniamo da codice)
    Private Sub ActiveKnob_ValueChanged(sender As Object, e As EventArgs) Handles knobFX1.ValueChanged, knobFX2.ValueChanged, knobFX3.ValueChanged
        If isUpdatingPreset Then Return
        
        Select Case currentSelectedFX
            Case "swChorus"
                fxChorusRate = knobFX1.Value / 10.0F
                fxChorusDepth = knobFX2.Value / 100.0F
                If guitarEffect IsNot Nothing Then
                    guitarEffect.ChorusRate = fxChorusRate
                    guitarEffect.ChorusDepth = fxChorusDepth
                End If
            Case "swDelay"
                fxDelayTime = knobFX1.Value
                fxDelayFeedback = knobFX2.Value / 100.0F
                fxDelayMix = knobFX3.Value / 100.0F
                If guitarEffect IsNot Nothing Then
                    guitarEffect.DelayTimeMs = fxDelayTime
                    guitarEffect.DelayFeedback = fxDelayFeedback
                    guitarEffect.DelayMix = fxDelayMix
                End If
            Case "swTremolo"
                fxTremoloRate = knobFX1.Value / 10.0F
                fxTremoloDepth = knobFX2.Value / 100.0F
                If guitarEffect IsNot Nothing Then
                    guitarEffect.TremoloRate = fxTremoloRate
                    guitarEffect.TremoloDepth = fxTremoloDepth
                End If
            Case "swReverb"
                fxReverbMix = knobFX1.Value / 100.0F
                fxReverbDecay = knobFX2.Value / 100.0F
                If guitarEffect IsNot Nothing Then
                    guitarEffect.ReverbMix = fxReverbMix
                    guitarEffect.ReverbDecay = fxReverbDecay
                End If
            Case "swComp"
                fxCompThreshold = knobFX1.Value / 100.0F
                fxCompRatio = knobFX2.Value / 10.0F
                If guitarEffect IsNot Nothing Then
                    guitarEffect.CompThreshold = fxCompThreshold
                    guitarEffect.CompRatio = fxCompRatio
                End If
        End Select
    End Sub

    ' --- AUDIO ENGINE (Ottimizzato Bassa Latenza) ---
    Private Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        Try
            If cmbInput.SelectedIndex < 0 Then Return
            audioInput = New WaveInEvent()
            audioInput.DeviceNumber = cmbInput.SelectedIndex
            audioInput.WaveFormat = New WaveFormat(192000, 24, 1)
            audioInput.BufferMilliseconds = 20
            audioInput.NumberOfBuffers = 3
            
            ' ReadFully = True è CRITICO per WasapiOut, altrimenti lo stream muore.
            ' BufferDuration limita la latenza massima; DiscardOnBufferOverflow gestisce l'overflow
            ' senza distruggere il buffer (NO ClearBuffer che causava click/pop/note sovrapposte).
            bufferedProvider = New BufferedWaveProvider(audioInput.WaveFormat) With {
                .DiscardOnBufferOverflow = True,
                .ReadFully = True,
                .BufferDuration = TimeSpan.FromMilliseconds(50)
            }

            AddHandler audioInput.DataAvailable, Sub(s, a)
                                                     bufferedProvider.AddSamples(a.Buffer, 0, a.BytesRecorded)
                                                 End Sub

            guitarEffect = New GuitarAmpEffect(bufferedProvider.ToSampleProvider())
            ApplySettings()

            ' --- BACKING TRACK MIXER (MASTER BUS) ---
            masterMixer = New SampleProviders.MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(192000, 1))
            masterMixer.ReadFully = True
            masterMixer.AddMixerInput(guitarEffect)

            ' Scelta tra WASAPI Esclusivo (latenza 10ms) e Condiviso (15ms, permette audio app)
            Dim shareMode = If(swExclusive.Checked, AudioClientShareMode.Exclusive, AudioClientShareMode.Shared)
            Dim latencyMode = If(swExclusive.Checked, 10, 15)
            audioOutput = New WasapiOut(shareMode, latencyMode)
            audioOutput.Init(masterMixer)
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
	    guitarEffect = Nothing : masterMixer = Nothing
        btnStart.Enabled = True : btnStop.Enabled = False
        picVuMeter.Invalidate()
    End Sub

    Private Sub btnRec_Click(sender As Object, e As EventArgs) Handles btnRec.Click
        If guitarEffect Is Nothing Then Return
        If Not isRecording Then
            guitarEffect.StartRecording("Rec.wav")
            isRecording = True
            btnRec.BackColor = Color.FromArgb(80, 15, 20)
            recSeconds = 0
            Timer1.Interval = 1000
            Timer1.Start()
        Else
            guitarEffect.StopRecording()
            isRecording = False
            btnRec.BackColor = Color.FromArgb(80, 40, 10)
            Timer1.Stop()
        End If
    End Sub

    ' --- PARAMETRI ---
    Private Sub ApplySettings()
        If guitarEffect Is Nothing Then Return

        ' Input Gain: boost per pickup passivi (Ibanez GSA60, ecc.)
        guitarEffect.InputGain = 3.0F

        ' Gate Esponenziale Bilanciato (con soglia minima per eliminare il noise floor).
        ' Anche a knob=0, il gate mantiene una soglia minima per sopprimere il rumore
        ' della scheda audio integrata (Realtek, ecc.) quando non si suona.
        Dim gateVal = CSng(knobGate.Value)
        Dim gateMin As Single = 0.005F   ' ~-46dB: sopprime il noise floor della scheda audio
        guitarEffect.GateThreshold = Math.Max(gateMin, (gateVal * gateVal) / 2000.0F)
        
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

        ' Send all state variables to DSP
        guitarEffect.ChorusRate = fxChorusRate
        guitarEffect.ChorusDepth = fxChorusDepth
        guitarEffect.DelayTimeMs = fxDelayTime
        guitarEffect.DelayFeedback = fxDelayFeedback
        guitarEffect.DelayMix = fxDelayMix
        guitarEffect.TremoloRate = fxTremoloRate
        guitarEffect.TremoloDepth = fxTremoloDepth
        guitarEffect.ReverbMix = fxReverbMix
        guitarEffect.ReverbDecay = fxReverbDecay
        guitarEffect.CompThreshold = fxCompThreshold
        guitarEffect.CompRatio = fxCompRatio
        
        ' Metronome
        guitarEffect.MetronomeEnabled = isMetronomeON
        guitarEffect.MetronomeBPM = MetronomeBpm
        guitarEffect.MetronomeVolume = MetronomeVol / 100.0F

        ' Looper (vol 100 = 2.5x multiplier to avoid masking)
        guitarEffect.LooperVolume = (LooperVol / 100.0F) * 2.5F

        guitarEffect.SignalChain = CType(globalSignalChain.Clone(), GuitarAmpEffect.FXType())

        guitarEffect.UpdateFilters()
    End Sub

    Public Sub ApplySettingsPublic()
        ApplySettings()
    End Sub

    ' Collega gli eventi dei nuovi controlli custom
    Private Sub Controls_Changed(sender As Object, e As EventArgs) Handles _
        knobGate.ValueChanged, knobVol.ValueChanged, knobDrive.ValueChanged,
        knobBass.ValueChanged, knobMid.ValueChanged, knobTreble.ValueChanged,
        swComp.CheckedChanged, swChorus.CheckedChanged, swDelay.CheckedChanged,
        swTremolo.CheckedChanged, swReverb.CheckedChanged, swCabSim.CheckedChanged

        If Not isUpdatingPreset Then
            ApplySettings()
            
            ' Auto-select the fx module if it was just turned ON
            If TypeOf sender Is RockSwitch Then
                Dim sw = DirectCast(sender, RockSwitch)
                If sw.Checked Then FX_SelectClicked(sw, EventArgs.Empty)
            End If
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

    ' --- USER PRESETS (Salva/Carica su file) ---
    Private Function GetPresetPath(slot As Integer) As String
        Return Path.Combine(Application.StartupPath, "UserPreset" & slot & ".cfg")
    End Function

    Private Sub UsrPreset_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Right Then
            ' Click Destro = Salva
            Dim btn = DirectCast(sender, ModernButton)
            Dim slot = 0
            If btn Is btnUsr1 Then slot = 1
            If btn Is btnUsr2 Then slot = 2
            If btn Is btnUsr3 Then slot = 3
            If slot > 0 Then SaveUserPreset(slot)
        End If
    End Sub

    Private Sub btnUsr1_Click(sender As Object, e As EventArgs) Handles btnUsr1.Click
        LoadUserPreset(1)
    End Sub
    Private Sub btnUsr2_Click(sender As Object, e As EventArgs) Handles btnUsr2.Click
        LoadUserPreset(2)
    End Sub
    Private Sub btnUsr3_Click(sender As Object, e As EventArgs) Handles btnUsr3.Click
        LoadUserPreset(3)
    End Sub

    Private Sub SaveUserPreset(slot As Integer)
        Try
            Dim lines As New List(Of String)
            ' Knobs
            lines.Add("vol=" & knobVol.Value)
            lines.Add("gate=" & knobGate.Value)
            lines.Add("drive=" & knobDrive.Value)
            lines.Add("bass=" & knobBass.Value)
            lines.Add("mid=" & knobMid.Value)
            lines.Add("treble=" & knobTreble.Value)
            ' Switches
            lines.Add("comp=" & If(swComp.Checked, 1, 0))
            lines.Add("chorus=" & If(swChorus.Checked, 1, 0))
            lines.Add("delay=" & If(swDelay.Checked, 1, 0))
            lines.Add("tremolo=" & If(swTremolo.Checked, 1, 0))
            lines.Add("reverb=" & If(swReverb.Checked, 1, 0))
            lines.Add("cabsim=" & If(swCabSim.Checked, 1, 0))
            ' FX Parameters
            lines.Add("chorusRate=" & fxChorusRate.ToString(Globalization.CultureInfo.InvariantCulture))
            lines.Add("chorusDepth=" & fxChorusDepth.ToString(Globalization.CultureInfo.InvariantCulture))
            lines.Add("delayTime=" & fxDelayTime.ToString(Globalization.CultureInfo.InvariantCulture))
            lines.Add("delayFeedback=" & fxDelayFeedback.ToString(Globalization.CultureInfo.InvariantCulture))
            lines.Add("delayMix=" & fxDelayMix.ToString(Globalization.CultureInfo.InvariantCulture))
            lines.Add("tremoloRate=" & fxTremoloRate.ToString(Globalization.CultureInfo.InvariantCulture))
            lines.Add("tremoloDepth=" & fxTremoloDepth.ToString(Globalization.CultureInfo.InvariantCulture))
            lines.Add("reverbMix=" & fxReverbMix.ToString(Globalization.CultureInfo.InvariantCulture))
            lines.Add("reverbDecay=" & fxReverbDecay.ToString(Globalization.CultureInfo.InvariantCulture))
            lines.Add("compThreshold=" & fxCompThreshold.ToString(Globalization.CultureInfo.InvariantCulture))
            lines.Add("compRatio=" & fxCompRatio.ToString(Globalization.CultureInfo.InvariantCulture))

            File.WriteAllLines(GetPresetPath(slot), lines)
            UpdateUsrButtonLabels()

            ' Flash feedback visivo
            Dim btn = If(slot = 1, btnUsr1, If(slot = 2, btnUsr2, btnUsr3))
            Dim origFg = btn.ForeColor
            btn.ForeColor = ThemeColors.Success
            btn.Text = "SAVED!"
            Dim t As New Timer() With {.Interval = 800}
            AddHandler t.Tick, Sub(s, ev)
                                   t.Stop()
                                   btn.ForeColor = origFg
                                   UpdateUsrButtonLabels()
                                   t.Dispose()
                               End Sub
            t.Start()
        Catch ex As Exception
            MessageBox.Show("Errore salvataggio preset: " & ex.Message)
        End Try
    End Sub

    Private Sub LoadUserPreset(slot As Integer)
        Dim path = GetPresetPath(slot)
        If Not File.Exists(path) Then
            MessageBox.Show("Slot " & slot & " vuoto." & vbCrLf & "Click destro per salvare.", "NeurAMPLI", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Try
            Dim dict As New Dictionary(Of String, String)
            For Each line In File.ReadAllLines(path)
                Dim parts = line.Split(New Char() {"="c}, 2)
                If parts.Length = 2 Then dict(parts(0).Trim()) = parts(1).Trim()
            Next

            isUpdatingPreset = True

            ' Knobs
            If dict.ContainsKey("vol") Then knobVol.Value = CInt(dict("vol"))
            If dict.ContainsKey("gate") Then knobGate.Value = CInt(dict("gate"))
            If dict.ContainsKey("drive") Then knobDrive.Value = CInt(dict("drive"))
            If dict.ContainsKey("bass") Then knobBass.Value = CInt(dict("bass"))
            If dict.ContainsKey("mid") Then knobMid.Value = CInt(dict("mid"))
            If dict.ContainsKey("treble") Then knobTreble.Value = CInt(dict("treble"))
            ' Switches
            If dict.ContainsKey("comp") Then swComp.Checked = (dict("comp") = "1")
            If dict.ContainsKey("chorus") Then swChorus.Checked = (dict("chorus") = "1")
            If dict.ContainsKey("delay") Then swDelay.Checked = (dict("delay") = "1")
            If dict.ContainsKey("tremolo") Then swTremolo.Checked = (dict("tremolo") = "1")
            If dict.ContainsKey("reverb") Then swReverb.Checked = (dict("reverb") = "1")
            If dict.ContainsKey("cabsim") Then swCabSim.Checked = (dict("cabsim") = "1")
            ' FX Parameters
            Dim ci = Globalization.CultureInfo.InvariantCulture
            If dict.ContainsKey("chorusRate") Then fxChorusRate = Single.Parse(dict("chorusRate"), ci)
            If dict.ContainsKey("chorusDepth") Then fxChorusDepth = Single.Parse(dict("chorusDepth"), ci)
            If dict.ContainsKey("delayTime") Then fxDelayTime = Single.Parse(dict("delayTime"), ci)
            If dict.ContainsKey("delayFeedback") Then fxDelayFeedback = Single.Parse(dict("delayFeedback"), ci)
            If dict.ContainsKey("delayMix") Then fxDelayMix = Single.Parse(dict("delayMix"), ci)
            If dict.ContainsKey("tremoloRate") Then fxTremoloRate = Single.Parse(dict("tremoloRate"), ci)
            If dict.ContainsKey("tremoloDepth") Then fxTremoloDepth = Single.Parse(dict("tremoloDepth"), ci)
            If dict.ContainsKey("reverbMix") Then fxReverbMix = Single.Parse(dict("reverbMix"), ci)
            If dict.ContainsKey("reverbDecay") Then fxReverbDecay = Single.Parse(dict("reverbDecay"), ci)
            If dict.ContainsKey("compThreshold") Then fxCompThreshold = Single.Parse(dict("compThreshold"), ci)
            If dict.ContainsKey("compRatio") Then fxCompRatio = Single.Parse(dict("compRatio"), ci)

            isUpdatingPreset = False
            ApplySettings()

            ' Aggiorna il pannello FX se c'è un effetto selezionato
            If currentSelectedFX <> "" Then UpdateFXPanel(currentSelectedFX)
        Catch ex As Exception
            isUpdatingPreset = False
            MessageBox.Show("Errore caricamento preset: " & ex.Message)
        End Try
    End Sub

    Private Sub UpdateUsrButtonLabels()
        ' Mostra "USR X" se lo slot è vuoto, "USR X ●" se contiene un preset
        btnUsr1.Text = If(File.Exists(GetPresetPath(1)), "USR 1 ●", "USR 1")
        btnUsr2.Text = If(File.Exists(GetPresetPath(2)), "USR 2 ●", "USR 2")
        btnUsr3.Text = If(File.Exists(GetPresetPath(3)), "USR 3 ●", "USR 3")
    End Sub

    ' --- VISUALS ---
    Private Sub tmrVisuals_Tick(sender As Object, e As EventArgs) Handles tmrVisuals.Tick
        If guitarEffect IsNot Nothing Then
            Dim target = guitarEffect.CurrentPeak
            currentPeakLevel = If(target > currentPeakLevel, target, currentPeakLevel - 0.1F)
            If currentPeakLevel < 0 Then currentPeakLevel = 0
            
            ' Peak Hold logic
            If currentPeakLevel >= peakHoldLevel Then
                peakHoldLevel = currentPeakLevel
                peakHoldFrames = 30 ' Hold for 30 ticks
            Else
                If peakHoldFrames > 0 Then
                    peakHoldFrames -= 1
                Else
                    peakHoldLevel -= 0.05F ' Drop down
                    If peakHoldLevel < 0 Then peakHoldLevel = 0
                End If
            End If
            
            ' Noise Gate visual LED
            If guitarEffect.GateActive AndAlso guitarEffect.EnableGate Then
                picGateLED.BackColor = Color.FromArgb(200, 40, 40)
            Else
                picGateLED.BackColor = Color.FromArgb(20, 20, 24)
            End If

            picVuMeter.Invalidate()
        End If
    End Sub

    Private Sub picVuMeter_Paint(sender As Object, e As PaintEventArgs) Handles picVuMeter.Paint
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim w = picVuMeter.Width
        Dim h = picVuMeter.Height

        ' Dark background flat
        g.FillRectangle(New SolidBrush(Color.FromArgb(20, 20, 24)), picVuMeter.ClientRectangle)
        Using pen As New Pen(Color.FromArgb(40, 40, 48), 1.0F)
            g.DrawRectangle(pen, 0, 0, w - 1, h - 1)
        End Using

        ' Segmented bars FLAT
        Dim totalBars = 60
        Dim barWidth = (w - 8) / totalBars
        Dim barGap = 2
        Dim filledBars = CInt(totalBars * currentPeakLevel)
        Dim peakBarIdx = CInt(totalBars * peakHoldLevel)

        For i = 0 To totalBars - 1
            Dim x = 4 + CInt(i * barWidth)
            Dim bw = CInt(barWidth) - barGap
            If bw < 1 Then bw = 1

            If i < filledBars Then
                ' Active flat segments
                Dim percent = CSng(i) / totalBars
                Dim barColor As Color
                If percent < 0.55 Then
                    barColor = ThemeColors.Success
                ElseIf percent < 0.8 Then
                    barColor = ThemeColors.AccentAmber
                Else
                    barColor = ThemeColors.Danger
                End If

                Using brush As New SolidBrush(barColor)
                    g.FillRectangle(brush, x, 3, bw, h - 6)
                End Using
            Else
                ' Dim flat segments
                Using brush As New SolidBrush(Color.FromArgb(32, 32, 40))
                    g.FillRectangle(brush, x, 4, bw, h - 8)
                End Using
            End If

            ' Draw Peak Hold
            If i = peakBarIdx AndAlso i > 0 AndAlso i < totalBars Then
                Using brush As New SolidBrush(Color.White)
                    g.FillRectangle(brush, x, 3, bw, h - 6)
                End Using
            End If
        Next
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.Close()
    End Sub

    ' --- TUNER ---
    Private tunerForm As TunerForm

    Private Sub btnTuner_Click(sender As Object, e As EventArgs) Handles btnTuner.Click
        If tunerForm IsNot Nothing AndAlso Not tunerForm.IsDisposed Then
            tunerForm.StartPosition = FormStartPosition.Manual
            tunerForm.Location = New Point(Me.Right + 4, Me.Top - 204)
            tunerForm.BringToFront()
            Return
        End If
        tunerForm = New TunerForm(cmbInput.SelectedIndex)
        tunerForm.StartPosition = FormStartPosition.Manual
        tunerForm.Location = New Point(Me.Right + 4, Me.Top - 204)
        tunerForm.Show(Me)
    End Sub

    Private Sub btnMetronome_Click(sender As Object, e As EventArgs) Handles btnMetronome.Click
        If metronomeForm Is Nothing OrElse metronomeForm.IsDisposed Then
            metronomeForm = New MetronomeForm()
        End If
        metronomeForm.StartPosition = FormStartPosition.Manual
        ' Posizionato sotto il Looper (335px + gap)
        metronomeForm.Location = New Point(Me.Left - metronomeForm.Width - 4, Me.Top + 135)
        metronomeForm.Visible = False
        metronomeForm.Show(Me)
    End Sub

    Private Sub btnLooper_Click(sender As Object, e As EventArgs) Handles btnLooper.Click
        If looperForm Is Nothing OrElse looperForm.IsDisposed Then
            looperForm = New LooperForm()
        End If
        looperForm.StartPosition = FormStartPosition.Manual
        looperForm.Location = New Point(Me.Left - looperForm.Width - 4, Me.Top - 204)
        looperForm.Visible = False
        looperForm.Show(Me)
    End Sub

    Private Sub btnChain_Click(sender As Object, e As EventArgs) Handles btnChain.Click
        If chainForm Is Nothing OrElse chainForm.IsDisposed Then
            chainForm = New ChainForm()
        End If
        chainForm.StartPosition = FormStartPosition.Manual
        chainForm.Location = New Point(Me.Left, Me.Top - chainForm.Height - 4)
        chainForm.Visible = False
        chainForm.Show(Me)
    End Sub

    Private Sub btnBacking_Click(sender As Object, e As EventArgs) Handles btnBacking.Click
        If backTrackForm Is Nothing OrElse backTrackForm.IsDisposed Then
            backTrackForm = New BackTrackForm()
        End If
        backTrackForm.StartPosition = FormStartPosition.Manual
        backTrackForm.Location = New Point(Me.Left, Me.Bottom + 4)
        backTrackForm.Visible = False
        backTrackForm.Show(Me)
    End Sub

    Public Sub ToggleLooperState()
        If guitarEffect Is Nothing Then Return
        
        Dim s = guitarEffect.LooperState
        If s = GuitarAmpEffect.LooperStates.Stopped Then
            guitarEffect.LooperState = GuitarAmpEffect.LooperStates.Recording
            guitarEffect.currentLooperPos = 0
            guitarEffect.looperLength = 0
        ElseIf s = GuitarAmpEffect.LooperStates.Recording Then
            guitarEffect.looperLength = guitarEffect.currentLooperPos
            guitarEffect.currentLooperPos = 0
            guitarEffect.LooperState = GuitarAmpEffect.LooperStates.Playing
        ElseIf s = GuitarAmpEffect.LooperStates.Playing Then
            guitarEffect.LooperState = GuitarAmpEffect.LooperStates.Stopped
            ' Manteniamo la lunghezza e la posizione = 0, così al prossimo play riparte
            guitarEffect.currentLooperPos = 0
        End If
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Space Then
            ToggleLooperState()
            Return True ' handled
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

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

    Private Sub pnlMain_Paint(sender As Object, e As PaintEventArgs) Handles pnlMain.Paint

    End Sub
End Class