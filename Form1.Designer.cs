namespace UoDangerLauncher;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Panel panelHeader;
    private System.Windows.Forms.PictureBox picLogo;
    private System.Windows.Forms.Panel panelCenter;
    private GoldButton btnPlay;
    private GoldProgressBar progressBar;
    private System.Windows.Forms.Panel panelFooter;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.Label lblMessage;
    private System.Windows.Forms.Label lblVersion;
    private System.Windows.Forms.LinkLabel lnkDiscord;
    private System.Windows.Forms.Label btnClose;
    private System.Windows.Forms.Label btnMinimize;
    private System.Windows.Forms.Label lblServerStatus;
    private System.Windows.Forms.Label lblMute;

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
        btnPlay = new GoldButton();
        progressBar = new GoldProgressBar();
        panelFooter = new Panel();
        lblStatus = new Label();
        lblVersion = new Label();
        lnkDiscord = new LinkLabel();
        btnClose = new Label();
        btnMinimize = new Label();
        lblServerStatus = new Label();
        lblMute = new Label();

        panelHeader.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
        panelCenter.SuspendLayout();
        panelFooter.SuspendLayout();
        SuspendLayout();

        // —— Header (logo + window buttons) ——
        panelHeader.BackColor = Color.Transparent;
        panelHeader.Controls.Add(btnClose);
        panelHeader.Controls.Add(btnMinimize);
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

        // Window close button
        btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClose.AutoSize = false;
        btnClose.BackColor = Color.Transparent;
        btnClose.Cursor = Cursors.Hand;
        btnClose.Font = new Font("Segoe UI", 12F);
        btnClose.ForeColor = Color.FromArgb(160, 160, 165);
        btnClose.Location = new Point(762, 4);
        btnClose.Size = new Size(32, 26);
        btnClose.Text = "\u2715";
        btnClose.TextAlign = ContentAlignment.MiddleCenter;
        btnClose.Click += btnClose_Click;

        // Window minimize button
        btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnMinimize.AutoSize = false;
        btnMinimize.BackColor = Color.Transparent;
        btnMinimize.Cursor = Cursors.Hand;
        btnMinimize.Font = new Font("Segoe UI", 12F);
        btnMinimize.ForeColor = Color.FromArgb(160, 160, 165);
        btnMinimize.Location = new Point(730, 4);
        btnMinimize.Size = new Size(32, 26);
        btnMinimize.Text = "\u2014";
        btnMinimize.TextAlign = ContentAlignment.MiddleCenter;
        btnMinimize.Click += btnMinimize_Click;

        // —— Center (button + message, centered in code) ——
        lblMessage = new Label();
        panelCenter.BackColor = Color.Transparent;
        panelCenter.Controls.Add(btnPlay);
        panelCenter.Controls.Add(lblMessage);
        panelCenter.Dock = DockStyle.Fill;
        panelCenter.Size = new Size(800, 262);
        panelCenter.MinimumSize = new Size(400, 200);

        lblMessage.Anchor = AnchorStyles.None;
        lblMessage.AutoSize = false;
        lblMessage.Font = new Font("Segoe UI", 10F);
        lblMessage.ForeColor = Color.FromArgb(180, 180, 185);
        lblMessage.BackColor = Color.Transparent;
        lblMessage.Size = new Size(600, 60);
        lblMessage.TextAlign = ContentAlignment.TopCenter;
        lblMessage.Text = "";
        lblMessage.Visible = false;

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

        // —— Footer ——
        panelFooter.BackColor = Color.Transparent;
        panelFooter.Dock = DockStyle.Bottom;
        panelFooter.Size = new Size(800, 82);
        panelFooter.MinimumSize = new Size(400, 82);
        panelFooter.Padding = new Padding(24, 6, 24, 10);

        progressBar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        progressBar.Location = new Point(24, 6);
        progressBar.Size = new Size(752, 16);
        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.Value = 0;
        progressBar.Visible = true;
        panelFooter.Controls.Add(progressBar);

        // Server status indicator (bottom-right, same row as Discord)
        lblServerStatus.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        lblServerStatus.AutoSize = true;
        lblServerStatus.Font = new Font("Segoe UI", 9F);
        lblServerStatus.ForeColor = Color.FromArgb(100, 100, 105);
        lblServerStatus.BackColor = Color.Transparent;
        lblServerStatus.Location = new Point(680, 54);
        lblServerStatus.Text = "";
        panelFooter.Controls.Add(lblServerStatus);

        lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        lblStatus.AutoSize = true;
        lblStatus.Font = new Font("Segoe UI", 10F);
        lblStatus.ForeColor = Color.FromArgb(140, 140, 145);
        lblStatus.Location = new Point(24, 28);
        lblStatus.Text = "Ready";
        lblStatus.BackColor = Color.Transparent;
        panelFooter.Controls.Add(lblStatus);

        lblVersion.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        lblVersion.AutoSize = true;
        lblVersion.Font = new Font("Segoe UI", 9F);
        lblVersion.ForeColor = Color.FromArgb(90, 90, 95);
        lblVersion.Location = new Point(700, 30);
        lblVersion.Text = "";
        lblVersion.BackColor = Color.Transparent;
        panelFooter.Controls.Add(lblVersion);

        lnkDiscord.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        lnkDiscord.AutoSize = true;
        lnkDiscord.Font = new Font("Segoe UI", 9F);
        lnkDiscord.LinkColor = Color.FromArgb(114, 137, 218);
        lnkDiscord.ActiveLinkColor = Color.FromArgb(134, 157, 238);
        lnkDiscord.VisitedLinkColor = Color.FromArgb(114, 137, 218);
        lnkDiscord.BackColor = Color.Transparent;
        lnkDiscord.Location = new Point(24, 54);
        lnkDiscord.Text = "Join our Discord";
        lnkDiscord.LinkClicked += lnkDiscord_LinkClicked;
        panelFooter.Controls.Add(lnkDiscord);

        // Music mute toggle (top-left)
        lblMute.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        lblMute.AutoSize = true;
        lblMute.Font = new Font("Segoe UI", 9F);
        lblMute.ForeColor = Color.FromArgb(140, 140, 145);
        lblMute.BackColor = Color.Transparent;
        lblMute.Cursor = Cursors.Hand;
        lblMute.Location = new Point(10, 8);
        lblMute.Text = "";
        lblMute.Visible = false;
        lblMute.Click += lblMute_Click;
        panelHeader.Controls.Add(lblMute);

        // —— Form ——
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.Black;
        ClientSize = new Size(800, 600);
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None;
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
