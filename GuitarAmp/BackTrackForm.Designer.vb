<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class BackTrackForm
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
        Me.btnClose = New NeurAmpli.ModernButton()
        Me.lblTitle = New System.Windows.Forms.Label()
        Me.lblPath = New System.Windows.Forms.Label()
        Me.btnLoad = New NeurAmpli.ModernButton()
        Me.btnPlay = New NeurAmpli.ModernButton()
        Me.btnStop = New NeurAmpli.ModernButton()
        Me.knobVol = New NeurAmpli.RockKnob()
        Me.SuspendLayout()
        '
        'btnClose
        '
        Me.btnClose.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(20, Byte), Integer), CType(CType(25, Byte), Integer))
        Me.btnClose.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnClose.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(75, Byte), Integer), CType(CType(95, Byte), Integer))
        Me.btnClose.Location = New System.Drawing.Point(340, 10)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(28, 28)
        Me.btnClose.TabIndex = 2
        Me.btnClose.Text = "X"
        '
        'lblTitle
        '
        Me.lblTitle.AutoSize = True
        Me.lblTitle.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblTitle.ForeColor = System.Drawing.Color.Silver
        Me.lblTitle.Location = New System.Drawing.Point(12, 14)
        Me.lblTitle.Name = "lblTitle"
        Me.lblTitle.Size = New System.Drawing.Size(180, 21)
        Me.lblTitle.TabIndex = 3
        Me.lblTitle.Text = "BACKING TRACK"
        '
        'lblPath
        '
        Me.lblPath.AutoEllipsis = True
        Me.lblPath.ForeColor = System.Drawing.Color.Gray
        Me.lblPath.Location = New System.Drawing.Point(15, 60)
        Me.lblPath.Name = "lblPath"
        Me.lblPath.Size = New System.Drawing.Size(350, 20)
        Me.lblPath.TabIndex = 4
        Me.lblPath.Text = "No file loaded..."
        '
        'btnLoad
        '
        Me.btnLoad.BackColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(40, Byte), Integer), CType(CType(45, Byte), Integer))
        Me.btnLoad.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnLoad.ForeColor = System.Drawing.Color.White
        Me.btnLoad.Location = New System.Drawing.Point(18, 90)
        Me.btnLoad.Name = "btnLoad"
        Me.btnLoad.Size = New System.Drawing.Size(100, 30)
        Me.btnLoad.TabIndex = 5
        Me.btnLoad.Text = "OPEN FILE"
        '
        'btnPlay
        '
        Me.btnPlay.BackColor = System.Drawing.Color.FromArgb(CType(CType(20, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.btnPlay.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnPlay.Enabled = False
        Me.btnPlay.ForeColor = System.Drawing.Color.Lime
        Me.btnPlay.Location = New System.Drawing.Point(130, 90)
        Me.btnPlay.Name = "btnPlay"
        Me.btnPlay.Size = New System.Drawing.Size(80, 30)
        Me.btnPlay.TabIndex = 6
        Me.btnPlay.Text = "PLAY"
        '
        'btnStop
        '
        Me.btnStop.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(20, Byte), Integer), CType(CType(25, Byte), Integer))
        Me.btnStop.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnStop.Enabled = False
        Me.btnStop.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(75, Byte), Integer), CType(CType(95, Byte), Integer))
        Me.btnStop.Location = New System.Drawing.Point(220, 90)
        Me.btnStop.Name = "btnStop"
        Me.btnStop.Size = New System.Drawing.Size(80, 30)
        Me.btnStop.TabIndex = 7
        Me.btnStop.Text = "STOP"
        '
        'knobVol
        '
        Me.knobVol.AccentColor = System.Drawing.Color.Orange
        Me.knobVol.ForeColor = System.Drawing.Color.White
        Me.knobVol.KnobText = "VOLUME"
        Me.knobVol.Location = New System.Drawing.Point(150, 140)
        Me.knobVol.Maximum = 100
        Me.knobVol.Minimum = 0
        Me.knobVol.Name = "knobVol"
        Me.knobVol.Size = New System.Drawing.Size(80, 100)
        Me.knobVol.TabIndex = 8
        Me.knobVol.Value = 50
        '
        'BackTrackForm
        '
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(18, Byte), Integer), CType(CType(18, Byte), Integer), CType(CType(22, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(380, 260)
        Me.ControlBox = False
        Me.Controls.Add(Me.knobVol)
        Me.Controls.Add(Me.btnStop)
        Me.Controls.Add(Me.btnPlay)
        Me.Controls.Add(Me.btnLoad)
        Me.Controls.Add(Me.lblPath)
        Me.Controls.Add(Me.lblTitle)
        Me.Controls.Add(Me.btnClose)
        Me.DoubleBuffered = True
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "BackTrackForm"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents btnClose As NeurAmpli.ModernButton
    Friend WithEvents lblTitle As System.Windows.Forms.Label
    Friend WithEvents lblPath As System.Windows.Forms.Label
    Friend WithEvents btnLoad As NeurAmpli.ModernButton
    Friend WithEvents btnPlay As NeurAmpli.ModernButton
    Friend WithEvents btnStop As NeurAmpli.ModernButton
    Friend WithEvents knobVol As NeurAmpli.RockKnob
End Class
