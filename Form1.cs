using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
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
            string path = Path.Combine(Application.StartupPath, "hero-home.webp");
            if (!File.Exists(path)) return;
            try
            {
                using var webp = SixLabors.ImageSharp.Image.Load(path);
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
            string path = Path.Combine(Application.StartupPath, "logo.png");
            if (!File.Exists(path)) return;
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
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
            string localVersion = File.Exists(localVersionFile)
                ? File.ReadAllText(localVersionFile).Trim()
                : "";

            using HttpClient http = new HttpClient();
            // Prevent cached version.txt so we always see the latest version
            http.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
            http.DefaultRequestHeaders.Add("Pragma", "no-cache");

            // Always fetch remote version from GitHub Pages (cache-bust with query to avoid CDN cache)
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

            // Parse "1.0.0 <download_url>" or just "1.0.0" with URL on next line
            string[] parts = remoteContent.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                MessageBox.Show("Invalid version info on server (expected version and download URL).");
                return false;
            }

            string remoteVersion = parts[0].Trim().Trim('\uFEFF'); // trim and remove BOM if present
            string clientZipUrl = parts[1].Trim();

            // If client folder exists and local version matches remote, skip download
            if (Directory.Exists(clientFolder) && string.Equals(localVersion, remoteVersion, StringComparison.OrdinalIgnoreCase))
            {
                lblStatus.Text = "Client up-to-date.";
                return true;
            }

            bool isUpgrade = Directory.Exists(clientFolder);

            // Version different or no client: remove old client if present, then download & extract
            if (isUpgrade)
            {
                SetLoadingState("Downloading...");
                lblStatus.Text = "New version found. Downloading update...";
                Directory.Delete(clientFolder, true);
            }
            else
            {
                SetLoadingState("Downloading...");
                lblStatus.Text = "Downloading game for the first time...";
            }

            try
            {
                await DownloadFileWithProgress(http, clientZipUrl, "client.zip", isUpgrade ? "Downloading update" : "Downloading game");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Download failed: " + ex.Message);
                return false;
            }

            SetLoadingState("Extracting...");
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Maximum = 100;
            progressBar.Value = 0;
            lblStatus.Text = "Extracting game...";
            lblStatus.Refresh();
            Application.DoEvents();

            string tempExtract = "temp_extract";
            if (Directory.Exists(tempExtract))
                Directory.Delete(tempExtract, true);
            Directory.CreateDirectory(tempExtract);

            try
            {
                await Task.Run(() => ExtractZipWithProgress("client.zip", tempExtract));
            }
            finally
            {
                if (File.Exists("client.zip"))
                    File.Delete("client.zip");
            }

            // Move extracted folder to Client
            var entries = Directory.GetDirectories(tempExtract);
            if (entries.Length == 1)
            {
                Directory.Move(entries[0], clientFolder);
                Directory.Delete(tempExtract, true);
            }
            else
            {
                Directory.Move(tempExtract, clientFolder);
            }

            File.WriteAllText(localVersionFile, remoteVersion);
            lblStatus.Text = "Ready.";
            lblStatus.Refresh();
            return true;
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
