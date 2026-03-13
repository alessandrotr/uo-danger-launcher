using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SixLabors.ImageSharp.Formats.Png;
using System.Linq;

namespace UoDangerLauncher
{
    public partial class Form1 : Form
    {
        string localVersionFile = "version.txt";
        string remoteVersionUrl = "https://alessandrotr.github.io/uo-danger-client/version.txt";
        string clientFolder = "Client";

        const string ServerIP = "51.68.191.126";
        const string ServerPort = "2593";
        const string DiscordInviteUrl = "https://discord.gg/9zsZDuMK6c";

        static readonly System.Drawing.Color ButtonColorNormal = System.Drawing.Color.FromArgb(201, 162, 39);
        static readonly System.Drawing.Color ButtonColorDisabled = System.Drawing.Color.FromArgb(160, 130, 35);
        string _btnPlayDefaultText = "Play";

        System.Drawing.Image? _backgroundImage;
        const int OverlayAlpha = 210;

        bool _musicPlaying;
        bool _musicMuted;
        string? _musicTempPath;

        public Form1()
        {
            InitializeComponent();
            Resize += (s, e) => { CenterLayout(); PositionVersionLabel(); PositionMuteLabel(); PositionServerStatus(); };
        }

        const int LogoMaxHeight = 260;
        const int LogoTopMargin = 50;
        const int TitleBarHeight = 34;

        // ═══════════════════════════════════════════════════════════════
        //  Custom window chrome
        // ═══════════════════════════════════════════════════════════════

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Add DWM shadow to borderless window
            try
            {
                var margins = new MARGINS { left = 1, right = 1, top = 1, bottom = 1 };
                DwmExtendFrameIntoClientArea(Handle, ref margins);
            }
            catch { }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MARGINS { public int left, right, top, bottom; }

        [DllImport("dwmapi.dll")]
        static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTCAPTION = 2;

            if (m.Msg == WM_NCHITTEST)
            {
                var pt = PointToClient(new Point((short)(m.LParam.ToInt32() & 0xFFFF), (short)(m.LParam.ToInt32() >> 16)));
                // Title bar drag area (top pixels, but not over close/minimize buttons)
                if (pt.Y < TitleBarHeight && pt.X < ClientSize.Width - 70)
                {
                    m.Result = (IntPtr)HTCAPTION;
                    return;
                }
            }
            base.WndProc(ref m);
        }

        void btnClose_Click(object? sender, EventArgs e) => Close();
        void btnMinimize_Click(object? sender, EventArgs e) => WindowState = FormWindowState.Minimized;

        // ═══════════════════════════════════════════════════════════════
        //  Init / Load
        // ═══════════════════════════════════════════════════════════════

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadBackground();
            LoadLogo();
            LoadIcon();
            CenterLayout();
            SetupWindowButtonHovers();
            _ = SetButtonTextFromVersionAsync();
            _ = CheckServerStatusLoop();
            StartMusic();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StopMusic();
            base.OnFormClosed(e);
        }

        void LoadIcon()
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UoDangerLauncher.logo.ico");
                if (stream == null) return;
                this.Icon = new Icon(stream);
            }
            catch { }
        }

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref uint value, int size);

        void LoadBackground()
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UoDangerLauncher.hero-home.webp");
                if (stream == null) return;
                using var webp = SixLabors.ImageSharp.Image.Load(stream);
                using var ms = new MemoryStream();
                webp.Save(ms, new PngEncoder());
                ms.Position = 0;
                _backgroundImage = System.Drawing.Image.FromStream(ms, false);
            }
            catch { }
        }

        void SetupWindowButtonHovers()
        {
            SetupHoverEffect(btnClose, Color.FromArgb(200, 60, 60));
            SetupHoverEffect(btnMinimize, Color.FromArgb(80, 80, 85));
        }

        static void SetupHoverEffect(Label lbl, Color hoverColor)
        {
            lbl.MouseEnter += (s, e) => lbl.ForeColor = Color.White;
            lbl.MouseLeave += (s, e) => lbl.ForeColor = Color.FromArgb(160, 160, 165);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Server status indicator
        // ═══════════════════════════════════════════════════════════════

        async Task CheckServerStatusLoop()
        {
            while (!IsDisposed)
            {
                bool online = await CheckServerOnline();
                if (IsDisposed) break;
                lblServerStatus.ForeColor = online
                    ? Color.FromArgb(80, 200, 80)
                    : Color.FromArgb(200, 80, 80);
                lblServerStatus.Text = online ? "\u25CF Online" : "\u25CF Offline";
                PositionServerStatus();
                await Task.Delay(30_000);
            }
        }

        static async Task<bool> CheckServerOnline()
        {
            try
            {
                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(ServerIP, int.Parse(ServerPort));
                if (await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask && tcp.Connected)
                    return true;
            }
            catch { }
            return false;
        }

        void PositionServerStatus()
        {
            if (lblServerStatus == null || panelFooter == null) return;
            var sz = TextRenderer.MeasureText(lblServerStatus.Text, lblServerStatus.Font);
            lblServerStatus.Location = new Point(
                panelFooter.ClientSize.Width - panelFooter.Padding.Right - sz.Width,
                6);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Background music (mciSendString)
        // ═══════════════════════════════════════════════════════════════

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        static extern int mciSendString(string command, StringBuilder? buffer, int bufferSize, IntPtr callback);

        void StartMusic()
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UoDangerLauncher.music.mp3");
                if (stream == null) return;

                _musicTempPath = Path.Combine(Path.GetTempPath(), "uodanger_music.mp3");
                using (var fs = new FileStream(_musicTempPath, FileMode.Create, FileAccess.Write))
                    stream.CopyTo(fs);

                mciSendString($"open \"{_musicTempPath}\" type mpegvideo alias bgmusic", null, 0, IntPtr.Zero);
                mciSendString("play bgmusic repeat", null, 0, IntPtr.Zero);
                mciSendString("setaudio bgmusic volume to 300", null, 0, IntPtr.Zero);
                _musicPlaying = true;
                _musicMuted = false;

                lblMute.Text = "Sound: ON";
                lblMute.Visible = true;
                PositionMuteLabel();
            }
            catch { }
        }

        void StopMusic()
        {
            if (!_musicPlaying) return;
            try
            {
                mciSendString("stop bgmusic", null, 0, IntPtr.Zero);
                mciSendString("close bgmusic", null, 0, IntPtr.Zero);
            }
            catch { }
            try { if (_musicTempPath != null && File.Exists(_musicTempPath)) File.Delete(_musicTempPath); }
            catch { }
        }

        void lblMute_Click(object? sender, EventArgs e)
        {
            if (!_musicPlaying) return;
            _musicMuted = !_musicMuted;
            if (_musicMuted)
            {
                mciSendString("setaudio bgmusic volume to 0", null, 0, IntPtr.Zero);
                lblMute.Text = "Sound: OFF";
                lblMute.ForeColor = Color.FromArgb(80, 80, 85);
            }
            else
            {
                mciSendString("setaudio bgmusic volume to 300", null, 0, IntPtr.Zero);
                lblMute.Text = "Sound: ON";
                lblMute.ForeColor = Color.FromArgb(120, 120, 125);
            }
            PositionMuteLabel();
        }

        void PositionMuteLabel()
        {
            if (lblMute == null || panelFooter == null) return;
            var sz = TextRenderer.MeasureText(lblMute.Text, lblMute.Font);
            lblMute.Location = new Point(
                panelFooter.ClientSize.Width - panelFooter.Padding.Right - sz.Width,
                54);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Version check / button state
        // ═══════════════════════════════════════════════════════════════

        async Task SetButtonTextFromVersionAsync()
        {
            string localVersion = File.Exists(localVersionFile) ? File.ReadAllText(localVersionFile).Trim() : "";
            if (!Directory.Exists(clientFolder) || !File.Exists(localVersionFile))
            {
                _btnPlayDefaultText = "Download";
                btnPlay.Text = "Download";
                lblVersion.Text = "";
                return;
            }

            lblVersion.Text = $"v{localVersion}";
            PositionVersionLabel();

            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
                string content = (await http.GetStringAsync(remoteVersionUrl + "?t=" + DateTime.UtcNow.Ticks)).Trim();
                var parts = content.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 1)
                {
                    string remoteVersion = parts[0].Trim().Trim('\uFEFF');
                    bool needsUpgrade = !string.Equals(localVersion, remoteVersion, StringComparison.OrdinalIgnoreCase);
                    _btnPlayDefaultText = needsUpgrade ? "Upgrade" : "Play";
                    btnPlay.Text = _btnPlayDefaultText;
                }
            }
            catch
            {
                _btnPlayDefaultText = "Play";
                btnPlay.Text = "Play";
            }
        }

        void PositionVersionLabel()
        {
            if (lblVersion == null || panelFooter == null) return;
            var sz = TextRenderer.MeasureText(lblVersion.Text, lblVersion.Font);
            lblVersion.Location = new Point(
                panelFooter.ClientSize.Width - panelFooter.Padding.Right - sz.Width,
                30);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Layout
        // ═══════════════════════════════════════════════════════════════

        void LoadLogo()
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UoDangerLauncher.logo.png");
                if (stream == null) return;
                picLogo.Image = Image.FromStream(stream);
                if (picLogo.Image != null)
                {
                    int w = picLogo.Image.Width;
                    int h = picLogo.Image.Height;
                    if (h > LogoMaxHeight)
                    {
                        w = (int)((double)w * LogoMaxHeight / h);
                        h = LogoMaxHeight;
                    }
                    picLogo.Size = new Size(w, h);
                }
            }
            catch { }
        }

        void CenterLayout()
        {
            if (panelHeader == null || !panelHeader.IsHandleCreated) return;

            int w = panelHeader.ClientSize.Width;
            picLogo.Location = new Point(Math.Max(0, (w - picLogo.Width) / 2), LogoTopMargin);

            if (panelCenter != null && panelCenter.IsHandleCreated)
            {
                int cw = panelCenter.ClientSize.Width;
                int ch = panelCenter.ClientSize.Height;
                int btnY = Math.Max(0, (ch - btnPlay.Height) / 2 - 30);
                btnPlay.Location = new Point(
                    Math.Max(0, (cw - btnPlay.Width) / 2), btnY);
                lblMessage.Location = new Point(
                    Math.Max(0, (cw - lblMessage.Width) / 2),
                    btnY + btnPlay.Height + 16);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var r = ClientRectangle;
            if (_backgroundImage != null)
            {
                e.Graphics.DrawImage(_backgroundImage, r);
                using var overlay = new SolidBrush(Color.FromArgb(OverlayAlpha, 0, 0, 0));
                e.Graphics.FillRectangle(overlay, r);
            }
            else
            {
                using var brush = new LinearGradientBrush(r, Color.FromArgb(12, 12, 15), Color.FromArgb(22, 22, 28), 90f);
                e.Graphics.FillRectangle(brush, r);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Download / Update / Launch
        // ═══════════════════════════════════════════════════════════════

        void SetLoadingState(string text)
        {
            btnPlay.Enabled = false;
            btnPlay.Text = text;
            btnPlay.BackColor = ButtonColorDisabled;
        }

        void SetButtonIdle()
        {
            btnPlay.Enabled = true;
            btnPlay.Text = _btnPlayDefaultText;
            btnPlay.BackColor = ButtonColorNormal;
            lblMessage.Visible = false;
        }

        private async void btnPlay_Click(object sender, EventArgs e)
        {
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Value = 0;
            SetLoadingState("Checking...");
            lblStatus.Text = "Checking version...";

            bool ok = await CheckAndUpdateClient();
            progressBar.Value = 0;
            if (ok)
                LaunchGame();
            else
                SetButtonIdle();
        }

        async Task<bool> CheckAndUpdateClient()
        {
            bool hasLocalVersion = File.Exists(localVersionFile);
            string localVersion = hasLocalVersion ? File.ReadAllText(localVersionFile).Trim() : "";
            bool clientExists = Directory.Exists(clientFolder);

            using HttpClient http = new HttpClient();
            http.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
            http.DefaultRequestHeaders.Add("Pragma", "no-cache");

            string versionUrl = remoteVersionUrl + "?t=" + DateTime.UtcNow.Ticks;
            string remoteContent;
            try
            {
                remoteContent = (await http.GetStringAsync(versionUrl)).Trim();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not fetch version from server: " + ex.Message);
                return false;
            }

            string[] parts = remoteContent.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                MessageBox.Show("Invalid version info on server (expected: version client_url update_url).");
                return false;
            }

            string remoteVersion = parts[0].Trim().Trim('\uFEFF');
            string clientZipUrl = parts[1].Trim();
            string updateZipUrl = parts[2].Trim();

            if (clientExists && hasLocalVersion && string.Equals(localVersion, remoteVersion, StringComparison.OrdinalIgnoreCase))
            {
                lblStatus.Text = "Client up-to-date.";
                return true;
            }

            bool isFreshInstall = !hasLocalVersion || !clientExists;

            if (isFreshInstall)
            {
                SetLoadingState("Downloading...");
                lblStatus.Text = "Downloading game for the first time...";
                lblMessage.Text = "The game is being downloaded. Once installed, new files and folders will appear next to the launcher.\nPlease do not move, rename, or delete any of them.";
                lblMessage.Visible = true;

                try { await DownloadFileWithProgress(http, clientZipUrl, "client.zip", "Downloading game"); }
                catch (Exception ex) { MessageBox.Show("Download failed: " + ex.Message); return false; }

                SetLoadingState("Extracting...");
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Maximum = 100;
                progressBar.Value = 0;
                lblStatus.Text = "Extracting game...";
                lblStatus.Refresh();
                Application.DoEvents();

                string tempExtract = "temp_extract";
                if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);
                Directory.CreateDirectory(tempExtract);

                try { await Task.Run(() => ExtractZipWithProgress("client.zip", tempExtract)); }
                finally { if (File.Exists("client.zip")) File.Delete("client.zip"); }

                if (Directory.Exists(clientFolder)) Directory.Delete(clientFolder, true);
                var topLevel = Directory.GetDirectories(tempExtract);
                if (topLevel.Length == 1 && Directory.GetFiles(tempExtract).Length == 0)
                {
                    Directory.Move(topLevel[0], clientFolder);
                    Directory.Delete(tempExtract, true);
                }
                else
                {
                    Directory.Move(tempExtract, clientFolder);
                }

                string profilesDir = Path.Combine(clientFolder, "ClassicUO", "Data", "Profiles");
                if (Directory.Exists(profilesDir))
                    Directory.Delete(profilesDir, true);
            }
            else
            {
                SetLoadingState("Downloading...");
                lblStatus.Text = "New version found. Downloading update...";
                lblMessage.Text = "A new update is being installed to improve your game experience.\nYour settings and profiles will be preserved.";
                lblMessage.Visible = true;

                try { await DownloadFileWithProgress(http, updateZipUrl, "update.zip", "Downloading update"); }
                catch (Exception ex) { MessageBox.Show("Update download failed: " + ex.Message); return false; }

                SetLoadingState("Extracting...");
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Maximum = 100;
                progressBar.Value = 0;
                lblStatus.Text = "Applying update...";
                lblStatus.Refresh();
                Application.DoEvents();

                string dataFolder = Path.Combine(clientFolder, "ClassicUO", "Data");
                Directory.CreateDirectory(dataFolder);

                try
                {
                    await Task.Run(() => ApplyUpdateZip("update.zip", dataFolder));
                }
                finally
                {
                    if (File.Exists("update.zip")) File.Delete("update.zip");
                }
            }

            File.WriteAllText(localVersionFile, remoteVersion);
            lblVersion.Text = $"v{remoteVersion}";
            PositionVersionLabel();
            lblStatus.Text = "Ready.";
            lblStatus.Refresh();
            return true;
        }

        void ApplyUpdateZip(string zipPath, string dataFolder)
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var entries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();
            int total = Math.Max(1, entries.Count);
            int current = 0;

            const string dataMarker = "ClassicUO/Data/";

            foreach (var entry in entries)
            {
                string fullName = entry.FullName.Replace('\\', '/');
                int idx = fullName.IndexOf(dataMarker, StringComparison.OrdinalIgnoreCase);
                string relativePath;
                if (idx >= 0)
                    relativePath = fullName.Substring(idx + dataMarker.Length);
                else
                {
                    int slash = fullName.IndexOf('/');
                    relativePath = slash >= 0 ? fullName.Substring(slash + 1) : fullName;
                }

                if (string.IsNullOrEmpty(relativePath)) { current++; continue; }

                string firstSegment = relativePath.Split('/')[0];
                if (string.Equals(firstSegment, "Profiles", StringComparison.OrdinalIgnoreCase)) { current++; continue; }

                string destPath = Path.GetFullPath(Path.Combine(dataFolder, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                if (!destPath.StartsWith(Path.GetFullPath(dataFolder) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                { current++; continue; }

                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                if (File.Exists(destPath))
                    File.SetAttributes(destPath, FileAttributes.Normal);
                entry.ExtractToFile(destPath, overwrite: true);

                current++;
                int pct = (int)((current * 100L) / total);
                try { progressBar.Invoke(() => { progressBar.Value = Math.Min(100, pct); progressBar.Refresh(); }); }
                catch { }
            }
        }

        void ExtractZipWithProgress(string zipPath, string extractPath)
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var entries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();
            int total = Math.Max(1, entries.Count);
            int current = 0;
            void ReportProgress()
            {
                if (progressBar.IsDisposed) return;
                int pct = (int)((current * 100L) / total);
                try { progressBar.Invoke(() => { progressBar.Value = Math.Min(100, pct); progressBar.Refresh(); }); }
                catch { }
            }
            foreach (var entry in entries)
            {
                string destPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
                if (destPath.StartsWith(Path.GetFullPath(extractPath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(entry.Name))
                        Directory.CreateDirectory(destPath);
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                        entry.ExtractToFile(destPath, true);
                    }
                }
                current++;
                ReportProgress();
            }
        }

        async Task DownloadFileWithProgress(HttpClient client, string url, string localPath, string statusPrefix = "Downloading client")
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;
            if (canReportProgress)
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Maximum = 100;
                progressBar.Value = 0;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            var speedTimer = System.Diagnostics.Stopwatch.StartNew();
            long lastSpeedBytes = 0;
            double lastSpeed = 0;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                if (canReportProgress)
                {
                    int percent = (int)((totalRead * 100L) / totalBytes);
                    progressBar.Value = Math.Min(100, percent);

                    double elapsed = speedTimer.Elapsed.TotalSeconds;
                    if (elapsed >= 0.5)
                    {
                        lastSpeed = (totalRead - lastSpeedBytes) / elapsed;
                        lastSpeedBytes = totalRead;
                        speedTimer.Restart();
                    }

                    string speedText = lastSpeed > 0 ? FormatSpeed(lastSpeed) : "";
                    string etaText = "";
                    if (lastSpeed > 0)
                    {
                        long remaining = totalBytes - totalRead;
                        int seconds = (int)(remaining / lastSpeed);
                        etaText = seconds >= 60
                            ? $" \u2014 {seconds / 60}m {seconds % 60}s left"
                            : $" \u2014 {seconds}s left";
                    }

                    lblStatus.Text = $"{statusPrefix}... {percent}%{(speedText.Length > 0 ? $"  ({speedText}{etaText})" : "")}";
                    progressBar.Refresh();
                    Application.DoEvents();
                }
            }
        }

        static string FormatSpeed(double bytesPerSec)
        {
            if (bytesPerSec >= 1_048_576)
                return $"{bytesPerSec / 1_048_576:F1} MB/s";
            if (bytesPerSec >= 1024)
                return $"{bytesPerSec / 1024:F0} KB/s";
            return $"{bytesPerSec:F0} B/s";
        }

        void lnkDiscord_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(DiscordInviteUrl) { UseShellExecute = true }); }
            catch { }
        }

        void LaunchGame()
        {
            string classicUOExe = Path.Combine(clientFolder, "ClassicUO", "ClassicUO.exe");
            string uoDataPath = Path.GetFullPath(Path.Combine(clientFolder, "ClassicUO", "Data"));

            if (!File.Exists(classicUOExe))
            {
                MessageBox.Show("ClassicUO executable not found!");
                SetButtonIdle();
                return;
            }

            SetLoadingState("Launching...");
            lblStatus.Text = "Launching game...";
            lblStatus.Refresh();
            Application.DoEvents();
            Process.Start(new ProcessStartInfo
            {
                FileName = classicUOExe,
                Arguments = $"-ip {ServerIP} -port {ServerPort} -uopath \"{uoDataPath}\"",
                UseShellExecute = true
            });
            Environment.Exit(0);
        }
    }
}
