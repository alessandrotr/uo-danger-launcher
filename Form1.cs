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

using System.Linq;
using System.Text.Json;

namespace UoDangerLauncher
{
    public partial class Form1 : Form
    {
        string remoteVersionUrl = "https://alessandrotr.github.io/uo-danger-client/version.txt";
        string clientFolder = "Client";

        const string ServerIP = "51.68.191.126";
        const string ServerPort = "2593";
        const string DiscordInviteUrl = "https://discord.gg/9zsZDuMK6c";
        const string WebsiteUrl = "https://uo-danger-web.vercel.app/";

        const string LauncherVersionFallback = "1.0.0";
        string remoteLauncherVersionUrl = "https://alessandrotr.github.io/uo-danger-client/launcher_version.txt";

        static readonly System.Drawing.Color ButtonColorNormal = System.Drawing.Color.FromArgb(201, 162, 39);
        static readonly System.Drawing.Color ButtonColorDisabled = System.Drawing.Color.FromArgb(160, 130, 35);
        string _btnPlayDefaultText = "Play";

        System.Drawing.Image? _backgroundImage;
        const int OverlayAlpha = 210;

        bool _musicPlaying;
        bool _musicMuted;
        string? _musicTempPath;

        // ═══════════════════════════════════════════════════════════════
        //  Unified settings (settings.json)
        // ═══════════════════════════════════════════════════════════════

        static readonly string SettingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "launcher_settings.json");

        class LauncherSettings
        {
            public bool MusicMuted { get; set; }
            public string LauncherVersion { get; set; } = LauncherVersionFallback;
            public string ClientVersion { get; set; } = "";
        }

        LauncherSettings _settings = new();

        void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    _settings = JsonSerializer.Deserialize<LauncherSettings>(json) ?? new();
                }
            }
            catch { _settings = new(); }

            // Migrate from old files if settings.json didn't have the values
            MigrateOldFiles();
        }

        void MigrateOldFiles()
        {
            bool migrated = false;
            string oldVersionFile = "version.txt";
            string oldSettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher_settings.txt");
            string oldLauncherVersionFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher_version_local.txt");

            try
            {
                if (File.Exists(oldVersionFile) && string.IsNullOrEmpty(_settings.ClientVersion))
                {
                    _settings.ClientVersion = File.ReadAllText(oldVersionFile).Trim();
                    File.Delete(oldVersionFile);
                    migrated = true;
                }
                if (File.Exists(oldSettingsFile))
                {
                    string content = File.ReadAllText(oldSettingsFile).Trim();
                    _settings.MusicMuted = string.Equals(content, "muted", StringComparison.OrdinalIgnoreCase);
                    File.Delete(oldSettingsFile);
                    migrated = true;
                }
                if (File.Exists(oldLauncherVersionFile))
                {
                    _settings.LauncherVersion = File.ReadAllText(oldLauncherVersionFile).Trim();
                    File.Delete(oldLauncherVersionFile);
                    migrated = true;
                }
            }
            catch { }

            if (migrated) SaveSettings();
        }

        void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch { }
        }

        public Form1()
        {
            InitializeComponent();
            Resize += (s, e) => { CenterLayout(); PositionVersionLabel(); PositionMuteLabel(); PositionServerStatus(); };
        }

        const int LogoMaxHeight = 180;
        const int LogoTopMargin = 120;
        const int TitleBarHeight = 34;

        // ═══════════════════════════════════════════════════════════════
        //  Custom window chrome
        // ═══════════════════════════════════════════════════════════════

        // Block maximize + suppress background erase to prevent flicker
        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MAXIMIZE = 0xF030;
            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_MAXIMIZE)
                return;

            const int WM_ERASEBKGND = 0x0014;
            if (m.Msg == WM_ERASEBKGND)
            {
                m.Result = (IntPtr)1; // Tell Windows we handled it
                return;
            }

            base.WndProc(ref m);
        }

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();

        void DragForm(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1 /* WM_NCLBUTTONDOWN */, 2 /* HTCAPTION */, 0);
            }
        }

        void btnClose_Click(object? sender, EventArgs e) => Close();
        void btnMinimize_Click(object? sender, EventArgs e) => WindowState = FormWindowState.Minimized;

        // ═══════════════════════════════════════════════════════════════
        //  Init / Load
        // ═══════════════════════════════════════════════════════════════

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadSettings();
            LoadBackground();
            LoadLogo();
            LoadIcon();
            CenterLayout();
            SetupWindowButtonHovers();
            SetupDrag();

            lblServerStatus.Text = "\u25CF Checking...";
            lblServerStatus.ForeColor = Color.FromArgb(130, 130, 135);
            PositionServerStatus();

            _ = CheckLauncherUpdateThenInit();
        }

        void SetupDrag()
        {
            // Allow dragging from header panel and logo
            panelHeader.MouseDown += DragForm;
            picLogo.MouseDown += DragForm;
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


        void LoadBackground()
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UoDangerLauncher.hero-home.png");
                if (stream == null) return;
                _backgroundImage = System.Drawing.Image.FromStream(stream);
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

        ToolTip? _serverTooltip;

        async Task CheckServerStatusLoop()
        {
            _serverTooltip ??= new ToolTip();
            while (!IsDisposed)
            {
                var (online, pingMs) = await CheckServerOnline();
                if (IsDisposed) break;
                lblServerStatus.ForeColor = online
                    ? Color.FromArgb(80, 200, 80)
                    : Color.FromArgb(200, 80, 80);
                lblServerStatus.Text = online ? $"\u25CF Online ({pingMs}ms)" : "\u25CF Offline";
                _serverTooltip.SetToolTip(lblServerStatus, online
                    ? $"Server: {ServerIP}:{ServerPort}\nPing: {pingMs}ms"
                    : $"Server: {ServerIP}:{ServerPort}\nStatus: Unreachable");
                PositionServerStatus();
                await Task.Delay(300_000); // 5 minutes — avoid triggering IP rate limits
            }
        }

        static async Task<(bool online, long pingMs)> CheckServerOnline()
        {
            try
            {
                using var tcp = new TcpClient();
                var sw = Stopwatch.StartNew();
                var connectTask = tcp.ConnectAsync(ServerIP, int.Parse(ServerPort));
                if (await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask && tcp.Connected)
                {
                    sw.Stop();
                    return (true, sw.ElapsedMilliseconds);
                }
            }
            catch { }
            return (false, 0);
        }

        void PositionServerStatus()
        {
            if (lblServerStatus == null || panelCenter == null || !panelCenter.IsHandleCreated) return;
            var sz = TextRenderer.MeasureText(lblServerStatus.Text, lblServerStatus.Font);
            int cw = panelCenter.ClientSize.Width;
            lblServerStatus.Location = new Point(
                Math.Max(0, (cw - sz.Width) / 2),
                btnPlay.Top - sz.Height - 8);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Background music (mciSendString)
        // ═══════════════════════════════════════════════════════════════

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        static extern int mciSendString(string command, StringBuilder? buffer, int bufferSize, IntPtr callback);

        bool LoadMuteSetting() => _settings.MusicMuted;

        void SaveMuteSetting(bool muted)
        {
            _settings.MusicMuted = muted;
            SaveSettings();
        }

        void StartMusic()
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UoDangerLauncher.music.mp3");
                if (stream == null) return;

                _musicTempPath = Path.Combine(Path.GetTempPath(), "uodanger_music.mp3");
                using (var fs = new FileStream(_musicTempPath, FileMode.Create, FileAccess.Write))
                    stream.CopyTo(fs);

                bool wasMuted = LoadMuteSetting();

                mciSendString($"open \"{_musicTempPath}\" type mpegvideo alias bgmusic", null, 0, IntPtr.Zero);
                mciSendString("play bgmusic repeat", null, 0, IntPtr.Zero);
                mciSendString($"setaudio bgmusic volume to {(wasMuted ? 0 : 300)}", null, 0, IntPtr.Zero);
                _musicPlaying = true;
                _musicMuted = wasMuted;

                lblMute.Text = wasMuted ? "Sound: OFF" : "Sound: ON";
                lblMute.ForeColor = wasMuted ? Color.FromArgb(80, 80, 85) : Color.FromArgb(140, 140, 145);
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
            SaveMuteSetting(_musicMuted);
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
            // Fixed top-left position in panelHeader, no repositioning needed
        }

        // ═══════════════════════════════════════════════════════════════
        //  Launcher self-update
        // ═══════════════════════════════════════════════════════════════

        string GetLocalLauncherVersion() => _settings.LauncherVersion;

        async Task CheckLauncherUpdateThenInit()
        {
            lblStatus.Text = "Checking for launcher updates...";
            try
            {
                string localLauncherVersion = GetLocalLauncherVersion();

                using var http = new HttpClient();
                http.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
                string content = (await http.GetStringAsync(remoteLauncherVersionUrl + "?t=" + DateTime.UtcNow.Ticks)).Trim();
                // Expected format: version download_url
                var parts = content.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string remoteVersion = parts[0].Trim().Trim('\uFEFF');
                    string downloadUrl = parts[1].Trim();

                    if (!string.Equals(remoteVersion, localLauncherVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        lblStatus.Text = $"Updating launcher to v{remoteVersion}...";
                        progressBar.Style = ProgressBarStyle.Marquee;
                        await UpdateLauncher(http, downloadUrl, remoteVersion);
                        return; // App will restart
                    }
                }
            }
            catch { /* If check fails, just continue with current version */ }

            // No update needed — continue normal init
            lblStatus.Text = "Ready";
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;
            _ = SetButtonTextFromVersionAsync();
            _ = CheckServerStatusLoop();
            StartMusic();
        }

        async Task UpdateLauncher(HttpClient http, string downloadUrl, string newVersion)
        {
            string currentExe = Application.ExecutablePath;
            string tempExe = currentExe + ".update";
            string oldExe = currentExe + ".old";

            try
            {
                // Download new launcher to temp file
                using var response = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                using var stream = await response.Content.ReadAsStreamAsync();
                using (var fs = new FileStream(tempExe, FileMode.Create, FileAccess.Write))
                    await stream.CopyToAsync(fs);

                // Swap: current → .old, temp → current
                if (File.Exists(oldExe)) File.Delete(oldExe);
                File.Move(currentExe, oldExe);
                File.Move(tempExe, currentExe);

                // Save the new version locally so next launch knows it's up-to-date
                _settings.LauncherVersion = newVersion;
                SaveSettings();

                // Restart
                Process.Start(new ProcessStartInfo
                {
                    FileName = currentExe,
                    UseShellExecute = true
                });
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                // Rollback if swap failed
                try
                {
                    if (!File.Exists(currentExe) && File.Exists(oldExe))
                        File.Move(oldExe, currentExe);
                    if (File.Exists(tempExe)) File.Delete(tempExe);
                }
                catch { }

                MessageBox.Show($"Launcher update failed: {ex.Message}\nContinuing with current version.");
                lblStatus.Text = "Ready";
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
                _ = SetButtonTextFromVersionAsync();
                _ = CheckServerStatusLoop();
                StartMusic();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Version check / button state
        // ═══════════════════════════════════════════════════════════════

        async Task SetButtonTextFromVersionAsync()
        {
            string localVersion = _settings.ClientVersion;
            if (!Directory.Exists(clientFolder) || string.IsNullOrEmpty(localVersion))
            {
                _btnPlayDefaultText = "Download";
                btnPlay.Text = "Download";
                lblVersion.Text = "";
                return;
            }

            lblVersion.Text = $"Client v{localVersion}";
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
                36);
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
            string localVersion = _settings.ClientVersion;
            bool hasLocalVersion = !string.IsNullOrEmpty(localVersion);
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

            _settings.ClientVersion = remoteVersion;
            SaveSettings();
            lblVersion.Text = $"Client v{remoteVersion}";
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

        void lnkWebsite_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(WebsiteUrl) { UseShellExecute = true }); }
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
