using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
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

        static readonly System.Drawing.Color ButtonColorNormal = System.Drawing.Color.FromArgb(201, 162, 39);
        static readonly System.Drawing.Color ButtonColorDisabled = System.Drawing.Color.FromArgb(160, 130, 35);
        string _btnPlayDefaultText = "Play";

        System.Drawing.Image? _backgroundImage;
        const int OverlayAlpha = 210; // 0–255; higher = darker overlay, image barely visible

        public Form1()
        {
            InitializeComponent();
            Resize += (s, e) => CenterLayout();
        }

        const int LogoMaxHeight = 260;
        const int LogoTopMargin = 50;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            TrySetBlackTitleBarAndBorders(Handle);
            LoadBackground();
            LoadLogo();
            CenterLayout();
            _ = SetButtonTextFromVersionAsync();
        }

        static void TrySetBlackTitleBarAndBorders(IntPtr hwnd)
        {
            try
            {
                if (hwnd == IntPtr.Zero) return;
                // Windows 11: caption and border color (0xFF000000 = black)
                const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
                const int DWMWA_CAPTION_COLOR = 35;
                const int DWMWA_BORDER_COLOR = 34;
                uint black = 0xFF_00_00_00;
                _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref black, sizeof(uint));
                _ = DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref black, sizeof(uint));
                _ = DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref black, sizeof(uint));
            }
            catch { /* ignore on older Windows */ }
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
            catch { /* ignore */ }
        }

        async Task SetButtonTextFromVersionAsync()
        {
            string localVersion = File.Exists(localVersionFile) ? File.ReadAllText(localVersionFile).Trim() : "";
            if (!Directory.Exists(clientFolder))
            {
                _btnPlayDefaultText = "Play";
                btnPlay.Text = "Play";
                return;
            }
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
            catch { /* ignore */ }
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
                btnPlay.Location = new Point(
                    Math.Max(0, (cw - btnPlay.Width) / 2),
                    Math.Max(0, (ch - btnPlay.Height) / 2));
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

            // Format: "<version> <client_zip_url> <update_zip_url>"
            string[] parts = remoteContent.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                MessageBox.Show("Invalid version info on server (expected: version client_url update_url).");
                return false;
            }

            string remoteVersion = parts[0].Trim().Trim('\uFEFF');
            string clientZipUrl = parts[1].Trim();
            string updateZipUrl = parts[2].Trim();

            // Already up-to-date
            if (clientExists && hasLocalVersion && string.Equals(localVersion, remoteVersion, StringComparison.OrdinalIgnoreCase))
            {
                lblStatus.Text = "Client up-to-date.";
                return true;
            }

            bool isFreshInstall = !hasLocalVersion || !clientExists;

            if (isFreshInstall)
            {
                // ── Fresh install: download and extract the full client.zip ──
                SetLoadingState("Downloading...");
                lblStatus.Text = "Downloading game for the first time...";

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
            }
            else
            {
                // ── Incremental update: replace Client\ClassicUO\Data except Profiles ──
                SetLoadingState("Downloading...");
                lblStatus.Text = "New version found. Downloading update...";

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
                // Normalize separators to forward slash for consistent matching
                string fullName = entry.FullName.Replace('\\', '/');

                // Find "ClassicUO/Data/" anywhere in the path and use everything after it
                int idx = fullName.IndexOf(dataMarker, StringComparison.OrdinalIgnoreCase);
                string relativePath;
                if (idx >= 0)
                {
                    relativePath = fullName.Substring(idx + dataMarker.Length);
                }
                else
                {
                    // No ClassicUO/Data/ prefix — strip the top-level wrapper folder (e.g. "update/")
                    int slash = fullName.IndexOf('/');
                    relativePath = slash >= 0 ? fullName.Substring(slash + 1) : fullName;
                }

                if (string.IsNullOrEmpty(relativePath)) { current++; continue; }

                // Skip Profiles folder
                string firstSegment = relativePath.Split('/')[0];
                if (string.Equals(firstSegment, "Profiles", StringComparison.OrdinalIgnoreCase)) { current++; continue; }

                string destPath = Path.GetFullPath(Path.Combine(dataFolder, relativePath.Replace('/', Path.DirectorySeparatorChar)));

                // Security: ensure destination is inside dataFolder
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
                try
                {
                    progressBar.Invoke(() =>
                    {
                        progressBar.Value = Math.Min(100, pct);
                        progressBar.Refresh();
                    });
                }
                catch { /* form closing */ }
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
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                if (canReportProgress)
                {
                    int percent = (int)((totalRead * 100L) / totalBytes);
                    progressBar.Value = Math.Min(100, percent);
                    lblStatus.Text = $"{statusPrefix}... {percent}%";
                    progressBar.Refresh();
                    Application.DoEvents();
                }
            }
        }

        void LaunchGame()
        {
            string clientExePath = Path.Combine(clientFolder, "ClassicUOLauncher.exe");

            if (!File.Exists(clientExePath))
            {
                MessageBox.Show("Client executable not found!");
                SetButtonIdle();
                return;
            }

            SetLoadingState("Launching...");
            lblStatus.Text = "Launching game...";
            lblStatus.Refresh();
            Application.DoEvents();
            Process.Start(new ProcessStartInfo
            {
                FileName = clientExePath,
                UseShellExecute = true
            });
            Environment.Exit(0);
        }
    }
}
