<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then components.Dispose()
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.cmbInput = New System.Windows.Forms.ComboBox()
        Me.btnStart = New System.Windows.Forms.Button()
        Me.btnStop = New System.Windows.Forms.Button()
        Me.btnRec = New System.Windows.Forms.Button()
        Me.picVuMeter = New System.Windows.Forms.PictureBox()
        Me.tmrVisuals = New System.Windows.Forms.Timer(Me.components)
        Me.pnlMain = New System.Windows.Forms.Panel()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.btnClean = New System.Windows.Forms.Button()
        Me.btnCrunch = New System.Windows.Forms.Button()
        Me.btnMetal = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.ToolStripContainer1 = New System.Windows.Forms.ToolStripContainer()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.swComp = New NeurAmpli.RockSwitch()
        Me.swChorus = New NeurAmpli.RockSwitch()
        Me.knobGate = New NeurAmpli.RockKnob()
        Me.swDelay = New NeurAmpli.RockSwitch()
        Me.knobDrive = New NeurAmpli.RockKnob()
        Me.swTremolo = New NeurAmpli.RockSwitch()
        Me.swReverb = New NeurAmpli.RockSwitch()
        Me.knobBass = New NeurAmpli.RockKnob()
        Me.knobMid = New NeurAmpli.RockKnob()
        Me.knobTreble = New NeurAmpli.RockKnob()
        Me.knobVol = New NeurAmpli.RockKnob()
        Me.swCabSim = New NeurAmpli.RockSwitch()
        Me.swExclusive = New NeurAmpli.RockSwitch()
        CType(Me.picVuMeter, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.pnlMain.SuspendLayout()
        Me.ToolStripContainer1.ContentPanel.SuspendLayout()
        Me.ToolStripContainer1.SuspendLayout()
        Me.SuspendLayout()
        '
        'cmbInput
        '
        Me.cmbInput.BackColor = System.Drawing.Color.FromArgb(CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.cmbInput.ForeColor = System.Drawing.Color.White
        Me.cmbInput.FormattingEnabled = True
        Me.cmbInput.Location = New System.Drawing.Point(411, 232)
        Me.cmbInput.Name = "cmbInput"
        Me.cmbInput.Size = New System.Drawing.Size(170, 21)
        Me.cmbInput.TabIndex = 1
        '
        'btnStart
        '
        Me.btnStart.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnStart.ForeColor = System.Drawing.Color.Lime
        Me.btnStart.Location = New System.Drawing.Point(307, 191)
        Me.btnStart.Name = "btnStart"
        Me.btnStart.Size = New System.Drawing.Size(122, 30)
        Me.btnStart.TabIndex = 2
        Me.btnStart.Text = "ON"
        Me.btnStart.UseVisualStyleBackColor = False
        '
        'btnStop
        '
        Me.btnStop.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnStop.ForeColor = System.Drawing.Color.Red
        Me.btnStop.Location = New System.Drawing.Point(307, 157)
        Me.btnStop.Name = "btnStop"
        Me.btnStop.Size = New System.Drawing.Size(54, 30)
        Me.btnStop.TabIndex = 3
        Me.btnStop.Text = "OFF"
        Me.btnStop.UseVisualStyleBackColor = False
        '
        'btnRec
        '
        Me.btnRec.BackColor = System.Drawing.Color.Orange
        Me.btnRec.ForeColor = System.Drawing.Color.White
        Me.btnRec.Location = New System.Drawing.Point(366, 331)
        Me.btnRec.Name = "btnRec"
        Me.btnRec.Size = New System.Drawing.Size(20, 20)
        Me.btnRec.TabIndex = 4
        Me.btnRec.UseVisualStyleBackColor = False
        '
        'picVuMeter
        '
        Me.picVuMeter.BackColor = System.Drawing.Color.Black
        Me.picVuMeter.Location = New System.Drawing.Point(11, 263)
        Me.picVuMeter.Name = "picVuMeter"
        Me.picVuMeter.Size = New System.Drawing.Size(574, 30)
        Me.picVuMeter.TabIndex = 2
        Me.picVuMeter.TabStop = False
        '
        'tmrVisuals
        '
        Me.tmrVisuals.Enabled = True
        Me.tmrVisuals.Interval = 30
        '
        'pnlMain
        '
        Me.pnlMain.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.pnlMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pnlMain.Controls.Add(Me.swComp)
        Me.pnlMain.Controls.Add(Me.swChorus)
        Me.pnlMain.Controls.Add(Me.knobGate)
        Me.pnlMain.Controls.Add(Me.swDelay)
        Me.pnlMain.Controls.Add(Me.knobDrive)
        Me.pnlMain.Controls.Add(Me.swTremolo)
        Me.pnlMain.Controls.Add(Me.swReverb)
        Me.pnlMain.Controls.Add(Me.knobBass)
        Me.pnlMain.Controls.Add(Me.knobMid)
        Me.pnlMain.Controls.Add(Me.knobTreble)
        Me.pnlMain.Controls.Add(Me.cmbInput)
        Me.pnlMain.Controls.Add(Me.knobVol)
        Me.pnlMain.Controls.Add(Me.btnStart)
        Me.pnlMain.Controls.Add(Me.btnStop)
        Me.pnlMain.Controls.Add(Me.picVuMeter)
        Me.pnlMain.Controls.Add(Me.swCabSim)
        Me.pnlMain.Controls.Add(Me.Label2)
        Me.pnlMain.Controls.Add(Me.Label1)
        Me.pnlMain.Controls.Add(Me.swExclusive)
        Me.pnlMain.Location = New System.Drawing.Point(12, 12)
        Me.pnlMain.Name = "pnlMain"
        Me.pnlMain.Size = New System.Drawing.Size(597, 307)
        Me.pnlMain.TabIndex = 0
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.BackColor = System.Drawing.Color.Transparent
        Me.Label2.Font = New System.Drawing.Font("Sitka Small", 11.25!, CType((System.Drawing.FontStyle.Bold Or System.Drawing.FontStyle.Italic), System.Drawing.FontStyle), System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.ForeColor = System.Drawing.Color.Orange
        Me.Label2.Location = New System.Drawing.Point(205, 7)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(58, 21)
        Me.Label2.TabIndex = 10
        Me.Label2.Text = "v2.4.0"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Virgil 3 YOFF", 26.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.ForeColor = System.Drawing.Color.Salmon
        Me.Label1.Location = New System.Drawing.Point(7, 8)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(209, 44)
        Me.Label1.TabIndex = 9
        Me.Label1.Text = "NeurAMPLI"
        '
        'btnClean
        '
        Me.btnClean.BackColor = System.Drawing.Color.OliveDrab
        Me.btnClean.ForeColor = System.Drawing.Color.White
        Me.btnClean.Location = New System.Drawing.Point(13, 327)
        Me.btnClean.Name = "btnClean"
        Me.btnClean.Size = New System.Drawing.Size(100, 28)
        Me.btnClean.TabIndex = 0
        Me.btnClean.Text = "Clean Tone"
        Me.btnClean.UseVisualStyleBackColor = False
        '
        'btnCrunch
        '
        Me.btnCrunch.BackColor = System.Drawing.Color.DarkGoldenrod
        Me.btnCrunch.ForeColor = System.Drawing.Color.White
        Me.btnCrunch.Location = New System.Drawing.Point(113, 327)
        Me.btnCrunch.Name = "btnCrunch"
        Me.btnCrunch.Size = New System.Drawing.Size(100, 28)
        Me.btnCrunch.TabIndex = 1
        Me.btnCrunch.Text = "Crunchy Heaven"
        Me.btnCrunch.UseVisualStyleBackColor = False
        '
        'btnMetal
        '
        Me.btnMetal.BackColor = System.Drawing.Color.Red
        Me.btnMetal.ForeColor = System.Drawing.Color.White
        Me.btnMetal.Location = New System.Drawing.Point(213, 327)
        Me.btnMetal.Name = "btnMetal"
        Me.btnMetal.Size = New System.Drawing.Size(100, 28)
        Me.btnMetal.TabIndex = 2
        Me.btnMetal.Text = "HEAVY METAL"
        Me.btnMetal.UseVisualStyleBackColor = False
        '
        'Button2
        '
        Me.Button2.BackColor = System.Drawing.Color.DodgerBlue
        Me.Button2.ForeColor = System.Drawing.Color.White
        Me.Button2.Location = New System.Drawing.Point(548, 327)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(31, 28)
        Me.Button2.TabIndex = 8
        Me.Button2.Text = "_"
        Me.Button2.UseVisualStyleBackColor = False
        '
        'Button1
        '
        Me.Button1.BackColor = System.Drawing.Color.Red
        Me.Button1.ForeColor = System.Drawing.Color.White
        Me.Button1.Location = New System.Drawing.Point(578, 327)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(31, 28)
        Me.Button1.TabIndex = 7
        Me.Button1.Text = "X"
        Me.Button1.UseVisualStyleBackColor = False
        '
        'ToolStripContainer1
        '
        Me.ToolStripContainer1.BottomToolStripPanelVisible = False
        '
        'ToolStripContainer1.ContentPanel
        '
        Me.ToolStripContainer1.ContentPanel.AutoScroll = True
        Me.ToolStripContainer1.ContentPanel.Controls.Add(Me.Label3)
        Me.ToolStripContainer1.ContentPanel.Controls.Add(Me.Button2)
        Me.ToolStripContainer1.ContentPanel.Controls.Add(Me.pnlMain)
        Me.ToolStripContainer1.ContentPanel.Controls.Add(Me.Button1)
        Me.ToolStripContainer1.ContentPanel.Controls.Add(Me.btnRec)
        Me.ToolStripContainer1.ContentPanel.Controls.Add(Me.btnClean)
        Me.ToolStripContainer1.ContentPanel.Controls.Add(Me.btnCrunch)
        Me.ToolStripContainer1.ContentPanel.Controls.Add(Me.btnMetal)
        Me.ToolStripContainer1.ContentPanel.Size = New System.Drawing.Size(623, 365)
        Me.ToolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ToolStripContainer1.LeftToolStripPanelVisible = False
        Me.ToolStripContainer1.Location = New System.Drawing.Point(0, 0)
        Me.ToolStripContainer1.Name = "ToolStripContainer1"
        Me.ToolStripContainer1.RightToolStripPanelVisible = False
        Me.ToolStripContainer1.Size = New System.Drawing.Size(623, 365)
        Me.ToolStripContainer1.TabIndex = 1
        Me.ToolStripContainer1.Text = "ToolStripContainer1"
        Me.ToolStripContainer1.TopToolStripPanelVisible = False
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Font = New System.Drawing.Font("TT2020Base", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label3.Location = New System.Drawing.Point(391, 333)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(108, 17)
        Me.Label3.TabIndex = 9
        Me.Label3.Text = "REC: 00:00"
        '
        'Timer1
        '
        Me.Timer1.Interval = 1000
        '
        'swComp
        '
        Me.swComp.Checked = False
        Me.swComp.CheckedColor = System.Drawing.Color.Orange
        Me.swComp.Cursor = System.Windows.Forms.Cursors.Hand
        Me.swComp.LabelText = "COMP"
        Me.swComp.Location = New System.Drawing.Point(435, 51)
        Me.swComp.Name = "swComp"
        Me.swComp.Size = New System.Drawing.Size(150, 30)
        Me.swComp.TabIndex = 0
        '
        'swChorus
        '
        Me.swChorus.Checked = False
        Me.swChorus.CheckedColor = System.Drawing.Color.Magenta
        Me.swChorus.Cursor = System.Windows.Forms.Cursors.Hand
        Me.swChorus.LabelText = "CHORUS"
        Me.swChorus.Location = New System.Drawing.Point(435, 86)
        Me.swChorus.Name = "swChorus"
        Me.swChorus.Size = New System.Drawing.Size(150, 30)
        Me.swChorus.TabIndex = 1
        '
        'knobGate
        '
        Me.knobGate.AccentColor = System.Drawing.Color.Gray
        Me.knobGate.ForeColor = System.Drawing.Color.White
        Me.knobGate.KnobText = "GATE"
        Me.knobGate.Location = New System.Drawing.Point(111, 49)
        Me.knobGate.Maximum = 20
        Me.knobGate.Minimum = 0
        Me.knobGate.Name = "knobGate"
        Me.knobGate.Size = New System.Drawing.Size(80, 100)
        Me.knobGate.TabIndex = 0
        Me.knobGate.Value = 0
        '
        'swDelay
        '
        Me.swDelay.Checked = False
        Me.swDelay.CheckedColor = System.Drawing.Color.DeepSkyBlue
        Me.swDelay.Cursor = System.Windows.Forms.Cursors.Hand
        Me.swDelay.LabelText = "DELAY"
        Me.swDelay.Location = New System.Drawing.Point(435, 121)
        Me.swDelay.Name = "swDelay"
        Me.swDelay.Size = New System.Drawing.Size(150, 30)
        Me.swDelay.TabIndex = 2
        '
        'knobDrive
        '
        Me.knobDrive.AccentColor = System.Drawing.Color.OrangeRed
        Me.knobDrive.ForeColor = System.Drawing.Color.White
        Me.knobDrive.KnobText = "GAIN"
        Me.knobDrive.Location = New System.Drawing.Point(211, 49)
        Me.knobDrive.Maximum = 10
        Me.knobDrive.Minimum = 0
        Me.knobDrive.Name = "knobDrive"
        Me.knobDrive.Size = New System.Drawing.Size(80, 100)
        Me.knobDrive.TabIndex = 1
        Me.knobDrive.Value = 0
        '
        'swTremolo
        '
        Me.swTremolo.Checked = False
        Me.swTremolo.CheckedColor = System.Drawing.Color.Lime
        Me.swTremolo.Cursor = System.Windows.Forms.Cursors.Hand
        Me.swTremolo.LabelText = "TREMOLO"
        Me.swTremolo.Location = New System.Drawing.Point(435, 156)
        Me.swTremolo.Name = "swTremolo"
        Me.swTremolo.Size = New System.Drawing.Size(150, 30)
        Me.swTremolo.TabIndex = 3
        '
        'swReverb
        '
        Me.swReverb.Checked = False
        Me.swReverb.CheckedColor = System.Drawing.Color.Cyan
        Me.swReverb.Cursor = System.Windows.Forms.Cursors.Hand
        Me.swReverb.LabelText = "REVERB"
        Me.swReverb.Location = New System.Drawing.Point(435, 191)
        Me.swReverb.Name = "swReverb"
        Me.swReverb.Size = New System.Drawing.Size(150, 30)
        Me.swReverb.TabIndex = 4
        '
        'knobBass
        '
        Me.knobBass.AccentColor = System.Drawing.Color.Gold
        Me.knobBass.ForeColor = System.Drawing.Color.White
        Me.knobBass.KnobText = "BASS"
        Me.knobBass.Location = New System.Drawing.Point(11, 157)
        Me.knobBass.Maximum = 10
        Me.knobBass.Minimum = -10
        Me.knobBass.Name = "knobBass"
        Me.knobBass.Size = New System.Drawing.Size(80, 100)
        Me.knobBass.TabIndex = 2
        Me.knobBass.Value = 0
        '
        'knobMid
        '
        Me.knobMid.AccentColor = System.Drawing.Color.Gold
        Me.knobMid.ForeColor = System.Drawing.Color.White
        Me.knobMid.KnobText = "MID"
        Me.knobMid.Location = New System.Drawing.Point(111, 157)
        Me.knobMid.Maximum = 10
        Me.knobMid.Minimum = -10
        Me.knobMid.Name = "knobMid"
        Me.knobMid.Size = New System.Drawing.Size(80, 100)
        Me.knobMid.TabIndex = 3
        Me.knobMid.Value = 0
        '
        'knobTreble
        '
        Me.knobTreble.AccentColor = System.Drawing.Color.Gold
        Me.knobTreble.ForeColor = System.Drawing.Color.White
        Me.knobTreble.KnobText = "HIGH"
        Me.knobTreble.Location = New System.Drawing.Point(211, 157)
        Me.knobTreble.Maximum = 10
        Me.knobTreble.Minimum = -10
        Me.knobTreble.Name = "knobTreble"
        Me.knobTreble.Size = New System.Drawing.Size(80, 100)
        Me.knobTreble.TabIndex = 4
        Me.knobTreble.Value = 0
        '
        'knobVol
        '
        Me.knobVol.AccentColor = System.Drawing.Color.White
        Me.knobVol.ForeColor = System.Drawing.Color.White
        Me.knobVol.KnobText = "MASTER"
        Me.knobVol.Location = New System.Drawing.Point(11, 49)
        Me.knobVol.Maximum = 20
        Me.knobVol.Minimum = 0
        Me.knobVol.Name = "knobVol"
        Me.knobVol.Size = New System.Drawing.Size(80, 100)
        Me.knobVol.TabIndex = 5
        Me.knobVol.Value = 0
        '
        'swCabSim
        '
        Me.swCabSim.Checked = True
        Me.swCabSim.CheckedColor = System.Drawing.Color.Gold
        Me.swCabSim.Cursor = System.Windows.Forms.Cursors.Hand
        Me.swCabSim.LabelText = "CAB SIM"
        Me.swCabSim.Location = New System.Drawing.Point(307, 228)
        Me.swCabSim.Name = "swCabSim"
        Me.swCabSim.Size = New System.Drawing.Size(278, 30)
        Me.swCabSim.TabIndex = 5
        '
        'swExclusive
        '
        Me.swExclusive.Checked = False
        Me.swExclusive.CheckedColor = System.Drawing.Color.Red
        Me.swExclusive.Cursor = System.Windows.Forms.Cursors.Hand
        Me.swExclusive.LabelText = "EXCLUSIVE"
        Me.swExclusive.Location = New System.Drawing.Point(307, 121)
        Me.swExclusive.Name = "swExclusive"
        Me.swExclusive.Size = New System.Drawing.Size(122, 30)
        Me.swExclusive.TabIndex = 6
        '
        'Form1
        '
        Me.BackColor = System.Drawing.Color.Tomato
        Me.ClientSize = New System.Drawing.Size(623, 365)
        Me.ControlBox = False
        Me.Controls.Add(Me.ToolStripContainer1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "Form1"
        Me.ShowIcon = False
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.TopMost = True
        CType(Me.picVuMeter, System.ComponentModel.ISupportInitialize).EndInit()
        Me.pnlMain.ResumeLayout(False)
        Me.pnlMain.PerformLayout()
        Me.ToolStripContainer1.ContentPanel.ResumeLayout(False)
        Me.ToolStripContainer1.ContentPanel.PerformLayout()
        Me.ToolStripContainer1.ResumeLayout(False)
        Me.ToolStripContainer1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents pnlMain As System.Windows.Forms.Panel
    Friend WithEvents cmbInput As System.Windows.Forms.ComboBox
    Friend WithEvents btnStart As System.Windows.Forms.Button
    Friend WithEvents btnStop As System.Windows.Forms.Button
    Friend WithEvents btnRec As System.Windows.Forms.Button
    Friend WithEvents picVuMeter As System.Windows.Forms.PictureBox

    ' Controlli Custom
    Friend WithEvents knobDrive As Global.NeurAmpli.RockKnob
    Friend WithEvents knobBass As Global.NeurAmpli.RockKnob
    Friend WithEvents knobMid As Global.NeurAmpli.RockKnob
    Friend WithEvents knobTreble As Global.NeurAmpli.RockKnob
    Friend WithEvents knobVol As Global.NeurAmpli.RockKnob
    Friend WithEvents knobGate As Global.NeurAmpli.RockKnob

    Friend WithEvents swComp As Global.NeurAmpli.RockSwitch
    Friend WithEvents swChorus As Global.NeurAmpli.RockSwitch
    Friend WithEvents swDelay As Global.NeurAmpli.RockSwitch
    Friend WithEvents swTremolo As Global.NeurAmpli.RockSwitch
    Friend WithEvents swReverb As Global.NeurAmpli.RockSwitch
    Friend WithEvents swCabSim As Global.NeurAmpli.RockSwitch
    Friend WithEvents swExclusive As Global.NeurAmpli.RockSwitch

    Friend WithEvents btnMetal As System.Windows.Forms.Button
    Friend WithEvents btnCrunch As System.Windows.Forms.Button
    Friend WithEvents btnClean As System.Windows.Forms.Button
    Friend WithEvents tmrVisuals As System.Windows.Forms.Timer
    Friend WithEvents Button1 As Button
    Friend WithEvents Button2 As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents ToolStripContainer1 As ToolStripContainer
    Friend WithEvents Label3 As Label
    Friend WithEvents Timer1 As Timer
End Class