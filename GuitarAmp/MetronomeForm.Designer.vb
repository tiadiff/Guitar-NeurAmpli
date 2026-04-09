<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MetronomeForm
    Inherits System.Windows.Forms.Form

    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then components.Dispose()
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.tmrUI = New System.Windows.Forms.Timer(Me.components)
        Me.lblTitle = New System.Windows.Forms.Label()
        Me.btnClose = New NeurAmpli.ModernButton()
        Me.swMetronome = New NeurAmpli.RockSwitch()
        Me.knobBPM = New NeurAmpli.RockKnob()
        Me.knobVol = New NeurAmpli.RockKnob()
        Me.btnBpmDown = New NeurAmpli.ModernButton()
        Me.btnBpmUp = New NeurAmpli.ModernButton()
        Me.lblBPMDisplay = New System.Windows.Forms.Label()
        Me.picBeat = New System.Windows.Forms.PictureBox()
        CType(Me.picBeat, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'tmrUI
        '
        Me.tmrUI.Interval = 30
        '
        'lblTitle
        '
        Me.lblTitle.AutoSize = True
        Me.lblTitle.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblTitle.ForeColor = System.Drawing.Color.FromArgb(255, 170, 0)
        Me.lblTitle.Location = New System.Drawing.Point(12, 14)
        Me.lblTitle.Name = "lblTitle"
        Me.lblTitle.Size = New System.Drawing.Size(130, 25)
        Me.lblTitle.TabIndex = 0
        Me.lblTitle.Text = "METRONOME"
        '
        'btnClose
        '
        Me.btnClose.BackColor = System.Drawing.Color.FromArgb(60, 20, 25)
        Me.btnClose.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnClose.ForeColor = System.Drawing.Color.FromArgb(255, 75, 95)
        Me.btnClose.Location = New System.Drawing.Point(238, 10)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(28, 28)
        Me.btnClose.TabIndex = 1
        Me.btnClose.Text = "X"
        '
        'swMetronome
        '
        Me.swMetronome.Checked = False
        Me.swMetronome.CheckedColor = System.Drawing.Color.Orange
        Me.swMetronome.Cursor = System.Windows.Forms.Cursors.Hand
        Me.swMetronome.IsSelected = False
        Me.swMetronome.LabelText = "POWER"
        Me.swMetronome.Location = New System.Drawing.Point(78, 55)
        Me.swMetronome.Name = "swMetronome"
        Me.swMetronome.Size = New System.Drawing.Size(120, 30)
        Me.swMetronome.TabIndex = 2
        '
        'lblBPMDisplay
        '
        Me.lblBPMDisplay.Font = New System.Drawing.Font("Segoe UI", 36.0!, System.Drawing.FontStyle.Bold)
        Me.lblBPMDisplay.ForeColor = System.Drawing.Color.White
        Me.lblBPMDisplay.Location = New System.Drawing.Point(50, 95)
        Me.lblBPMDisplay.Name = "lblBPMDisplay"
        Me.lblBPMDisplay.Size = New System.Drawing.Size(176, 60)
        Me.lblBPMDisplay.TabIndex = 3
        Me.lblBPMDisplay.Text = "120"
        Me.lblBPMDisplay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'btnBpmDown
        '
        Me.btnBpmDown.BackColor = System.Drawing.Color.FromArgb(37, 37, 41)
        Me.btnBpmDown.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnBpmDown.ForeColor = System.Drawing.Color.White
        Me.btnBpmDown.Location = New System.Drawing.Point(18, 110)
        Me.btnBpmDown.Name = "btnBpmDown"
        Me.btnBpmDown.Size = New System.Drawing.Size(30, 30)
        Me.btnBpmDown.TabIndex = 4
        Me.btnBpmDown.Text = "−"
        '
        'btnBpmUp
        '
        Me.btnBpmUp.BackColor = System.Drawing.Color.FromArgb(37, 37, 41)
        Me.btnBpmUp.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnBpmUp.ForeColor = System.Drawing.Color.White
        Me.btnBpmUp.Location = New System.Drawing.Point(228, 110)
        Me.btnBpmUp.Name = "btnBpmUp"
        Me.btnBpmUp.Size = New System.Drawing.Size(30, 30)
        Me.btnBpmUp.TabIndex = 5
        Me.btnBpmUp.Text = "+"
        '
        'knobBPM
        '
        Me.knobBPM.AccentColor = System.Drawing.Color.Orange
        Me.knobBPM.BackColor = System.Drawing.Color.FromArgb(14, 14, 16)
        Me.knobBPM.ForeColor = System.Drawing.Color.White
        Me.knobBPM.KnobText = "BPM"
        Me.knobBPM.Location = New System.Drawing.Point(40, 165)
        Me.knobBPM.Maximum = 240
        Me.knobBPM.Minimum = 40
        Me.knobBPM.Name = "knobBPM"
        Me.knobBPM.Size = New System.Drawing.Size(80, 100)
        Me.knobBPM.TabIndex = 6
        Me.knobBPM.Value = 120
        '
        'knobVol
        '
        Me.knobVol.AccentColor = System.Drawing.Color.DeepSkyBlue
        Me.knobVol.BackColor = System.Drawing.Color.FromArgb(14, 14, 16)
        Me.knobVol.ForeColor = System.Drawing.Color.White
        Me.knobVol.KnobText = "MIX"
        Me.knobVol.Location = New System.Drawing.Point(156, 165)
        Me.knobVol.Maximum = 100
        Me.knobVol.Minimum = 0
        Me.knobVol.Name = "knobVol"
        Me.knobVol.Size = New System.Drawing.Size(80, 100)
        Me.knobVol.TabIndex = 7
        Me.knobVol.Value = 50
        '
        'picBeat
        '
        Me.picBeat.Location = New System.Drawing.Point(128, 275)
        Me.picBeat.Name = "picBeat"
        Me.picBeat.Size = New System.Drawing.Size(20, 20)
        Me.picBeat.TabIndex = 8
        Me.picBeat.TabStop = False
        '
        'MetronomeForm
        '
        Me.BackColor = System.Drawing.Color.FromArgb(14, 14, 16)
        Me.ClientSize = New System.Drawing.Size(276, 310)
        Me.ControlBox = False
        Me.Controls.Add(Me.picBeat)
        Me.Controls.Add(Me.knobVol)
        Me.Controls.Add(Me.knobBPM)
        Me.Controls.Add(Me.btnBpmUp)
        Me.Controls.Add(Me.btnBpmDown)
        Me.Controls.Add(Me.lblBPMDisplay)
        Me.Controls.Add(Me.swMetronome)
        Me.Controls.Add(Me.btnClose)
        Me.Controls.Add(Me.lblTitle)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "MetronomeForm"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        CType(Me.picBeat, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents tmrUI As System.Windows.Forms.Timer
    Friend WithEvents lblTitle As System.Windows.Forms.Label
    Friend WithEvents btnClose As NeurAmpli.ModernButton
    Friend WithEvents swMetronome As NeurAmpli.RockSwitch
    Friend WithEvents knobBPM As NeurAmpli.RockKnob
    Friend WithEvents knobVol As NeurAmpli.RockKnob
    Friend WithEvents btnBpmDown As NeurAmpli.ModernButton
    Friend WithEvents btnBpmUp As NeurAmpli.ModernButton
    Friend WithEvents lblBPMDisplay As System.Windows.Forms.Label
    Friend WithEvents picBeat As System.Windows.Forms.PictureBox
End Class
