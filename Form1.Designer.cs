namespace UoDangerLauncher;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Panel panelHeader;
    private System.Windows.Forms.PictureBox picLogo;
    private System.Windows.Forms.Panel panelCenter;
    private System.Windows.Forms.Button btnPlay;
    private System.Windows.Forms.ProgressBar progressBar;
    private System.Windows.Forms.Panel panelFooter;
    private System.Windows.Forms.Label lblStatus;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        panelHeader = new Panel();
        picLogo = new PictureBox();
        panelCenter = new Panel();
        btnPlay = new Button();
        progressBar = new ProgressBar();
        panelFooter = new Panel();
        lblStatus = new Label();

        panelHeader.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
        panelCenter.SuspendLayout();
        panelFooter.SuspendLayout();
        SuspendLayout();

        // —— Header (logo only, no border, centered in code) ——
        panelHeader.BackColor = Color.Transparent;
        panelHeader.Controls.Add(picLogo);
        panelHeader.Dock = DockStyle.Top;
        panelHeader.Size = new Size(800, 320);
        panelHeader.MinimumSize = new Size(400, 320);

        picLogo.BackColor = Color.Transparent;
        picLogo.Location = new Point(0, 50);
        picLogo.SizeMode = PictureBoxSizeMode.Zoom;
        picLogo.Size = new Size(200, 100);
        picLogo.TabIndex = 0;
        picLogo.TabStop = false;

        // —— Center (button centered in code) ——
        panelCenter.BackColor = Color.Transparent;
        panelCenter.Controls.Add(btnPlay);
        panelCenter.Dock = DockStyle.Fill;
        panelCenter.Size = new Size(800, 262);
        panelCenter.MinimumSize = new Size(400, 200);

        btnPlay.Anchor = AnchorStyles.None;
        btnPlay.AutoSize = false;
        btnPlay.BackColor = Color.FromArgb(201, 162, 39);
        btnPlay.Cursor = Cursors.Hand;
        btnPlay.FlatAppearance.BorderSize = 0;
        btnPlay.FlatAppearance.MouseOverBackColor = Color.FromArgb(212, 175, 55);
        btnPlay.FlatStyle = FlatStyle.Flat;
        btnPlay.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        btnPlay.ForeColor = Color.FromArgb(20, 20, 20);
        btnPlay.Size = new Size(240, 56);
        btnPlay.TabIndex = 0;
        btnPlay.TabStop = false;
        btnPlay.Text = "PLAY";
        btnPlay.UseVisualStyleBackColor = false;
        btnPlay.Click += btnPlay_Click;

        // —— Footer (progress bar on row 0, status text on row 1) ——
        panelFooter.BackColor = Color.Transparent;
        panelFooter.Dock = DockStyle.Bottom;
        panelFooter.Size = new Size(800, 72);
        panelFooter.MinimumSize = new Size(400, 72);
        panelFooter.Padding = new Padding(24, 10, 24, 10);

        progressBar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        progressBar.Location = new Point(24, 10);
        progressBar.Size = new Size(752, 16);
        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.Value = 0;
        progressBar.Visible = true;
        panelFooter.Controls.Add(progressBar);

        lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        lblStatus.AutoSize = true;
        lblStatus.Font = new Font("Segoe UI", 10F);
        lblStatus.ForeColor = Color.FromArgb(140, 140, 145);
        lblStatus.Location = new Point(24, 34);
        lblStatus.Text = "Ready";
        lblStatus.BackColor = Color.Transparent;
        panelFooter.Controls.Add(lblStatus);

        // —— Form ——
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.Black;
        ClientSize = new Size(800, 600);
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimumSize = new Size(816, 639);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "UO Danger — Launcher";

        Controls.Add(panelCenter);
        Controls.Add(panelFooter);
        Controls.Add(panelHeader);

        panelHeader.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
        panelCenter.ResumeLayout(false);
        panelFooter.ResumeLayout(false);
        panelFooter.PerformLayout();
        ResumeLayout(false);
    }
}
