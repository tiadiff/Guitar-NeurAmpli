<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class LooperForm
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
        Me.knobVol = New NeurAmpli.RockKnob()
        Me.lblState = New System.Windows.Forms.Label()
        Me.btnState = New System.Windows.Forms.PictureBox()
        Me.btnClear = New NeurAmpli.ModernButton()
        Me.lblHelp = New System.Windows.Forms.Label()
        CType(Me.btnState, System.ComponentModel.ISupportInitialize).BeginInit()
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
        Me.lblTitle.Size = New System.Drawing.Size(80, 25)
        Me.lblTitle.TabIndex = 0
        Me.lblTitle.Text = "LOOPER"
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
        'lblState
        '
        Me.lblState.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold)
        Me.lblState.ForeColor = System.Drawing.Color.Gray
        Me.lblState.Location = New System.Drawing.Point(0, 45)
        Me.lblState.Name = "lblState"
        Me.lblState.Size = New System.Drawing.Size(276, 30)
        Me.lblState.TabIndex = 3
        Me.lblState.Text = "EMPTY"
        Me.lblState.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'btnState
        '
        Me.btnState.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnState.Location = New System.Drawing.Point(78, 85)
        Me.btnState.Name = "btnState"
        Me.btnState.Size = New System.Drawing.Size(120, 120)
        Me.btnState.TabIndex = 8
        Me.btnState.TabStop = False
        '
        'btnClear
        '
        Me.btnClear.BackColor = System.Drawing.Color.FromArgb(37, 37, 41)
        Me.btnClear.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnClear.ForeColor = System.Drawing.Color.White
        Me.btnClear.Location = New System.Drawing.Point(28, 225)
        Me.btnClear.Name = "btnClear"
        Me.btnClear.Size = New System.Drawing.Size(80, 30)
        Me.btnClear.TabIndex = 4
        Me.btnClear.Text = "CLEAR"
        '
        'knobVol
        '
        Me.knobVol.AccentColor = System.Drawing.Color.DeepSkyBlue
        Me.knobVol.BackColor = System.Drawing.Color.FromArgb(14, 14, 16)
        Me.knobVol.ForeColor = System.Drawing.Color.White
        Me.knobVol.KnobText = "VOL"
        Me.knobVol.Location = New System.Drawing.Point(150, 215)
        Me.knobVol.Maximum = 100
        Me.knobVol.Minimum = 0
        Me.knobVol.Name = "knobVol"
        Me.knobVol.Size = New System.Drawing.Size(80, 100)
        Me.knobVol.TabIndex = 7
        Me.knobVol.Value = 50
        '
        'lblHelp
        '
        Me.lblHelp.Font = New System.Drawing.Font("Segoe UI", 8.0!)
        Me.lblHelp.ForeColor = System.Drawing.Color.FromArgb(100, 100, 110)
        Me.lblHelp.Location = New System.Drawing.Point(0, 310)
        Me.lblHelp.Name = "lblHelp"
        Me.lblHelp.Size = New System.Drawing.Size(276, 20)
        Me.lblHelp.TabIndex = 9
        Me.lblHelp.Text = "Press Spacebar to Rec/Play/Stop"
        Me.lblHelp.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'LooperForm
        '
        Me.BackColor = System.Drawing.Color.FromArgb(14, 14, 16)
        Me.ClientSize = New System.Drawing.Size(276, 335)
        Me.ControlBox = False
        Me.Controls.Add(Me.lblHelp)
        Me.Controls.Add(Me.btnState)
        Me.Controls.Add(Me.knobVol)
        Me.Controls.Add(Me.btnClear)
        Me.Controls.Add(Me.lblState)
        Me.Controls.Add(Me.btnClose)
        Me.Controls.Add(Me.lblTitle)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "LooperForm"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        CType(Me.btnState, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents tmrUI As System.Windows.Forms.Timer
    Friend WithEvents lblTitle As System.Windows.Forms.Label
    Friend WithEvents btnClose As NeurAmpli.ModernButton
    Friend WithEvents knobVol As NeurAmpli.RockKnob
    Friend WithEvents lblState As System.Windows.Forms.Label
    Friend WithEvents btnState As System.Windows.Forms.PictureBox
    Friend WithEvents btnClear As NeurAmpli.ModernButton
    Friend WithEvents lblHelp As System.Windows.Forms.Label
End Class
