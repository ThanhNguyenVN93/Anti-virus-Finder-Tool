using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace frmavfinnder
{
    public partial class Form1 : Form
    {
        // ── Fix #3, #4: Fields dùng chung giữa các method ──
        private WebClient _webClient;
        private string _lastSavePath;

        // ── Fix #7: Đo tốc độ download thực tế ──
        private readonly Stopwatch _downloadStopwatch = new Stopwatch();
        private long _lastBytesReceived = 0;

        // ── Auto download flag ──
        private bool _autoDownloadTriggered = false;

        // ── Detected AV name ──
        private string _detectedAvName = "";

        public Form1()
        {
            InitializeComponent();
        }

        // ────────────────────────────────────────────────────
        //  TIMER – cập nhật tiêu đề cửa sổ theo giờ thực
        // ────────────────────────────────────────────────────
        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Text = $"ANTI-VIRUS FINDER TOOL V1.1 - {DateTime.Now:HH:mm:ss}";
        }

        // ────────────────────────────────────────────────────
        //  FORM LOAD
        // ────────────────────────────────────────────────────
        private void Form1_Load(object sender, EventArgs e)
        {
            // Fix #6: lblavstatus bị đặt Visible = false trong Designer → bật lại ở đây
            lblavstatus.Visible = true;
            lblavstatus.Text = "";

            // ── SAFE MODE CHECK ──
            // Nếu đang ở Safe Mode (sau khi reboot từ Form2),
            // nhảy thẳng vào Form2 để chạy uninstaller hoặc boot về Normal.
            if (SystemInformation.BootMode != BootMode.Normal)
            {
                string uninstallerPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads", "uninstaller.exe");

                if (File.Exists(uninstallerPath))
                {
                    LaunchForm2(uninstallerPath);
                    return;  // Không chạy tiếp phần dưới
                }
            }

            // Phát hiện hệ điều hành qua WMI.
            // Bọc try/catch: máy đang gặp sự cố AV (đúng đối tượng app này nhắm tới)
            // có thể khiến dịch vụ WMI không phản hồi được → không để app crash ngay lúc mở.
            string r = "";
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        using (obj)
                        {
                            r = obj["Caption"] + " - " + obj["OSArchitecture"];
                        }
                    }
                }
                r = r.Replace("NT 5.1.2600", "XP").Replace("NT 5.2.3790", "Server 2003");
            }
            catch (Exception ex)
            {
                r = $"Không thể xác định hệ điều hành ({ex.Message})";
            }
            lblos.Text = r;

            // Populate AV Support List
            PopulateAvSupportList();

            btnavcheck.PerformClick();
        }

        // ────────────────────────────────────────────────────
        //  POPULATE AV SUPPORT LIST
        // ────────────────────────────────────────────────────
        private void PopulateAvSupportList()
        {
            lbAvSupport.Items.Clear();
            lbAvSupport.Items.Add("✓ Kaspersky");
            lbAvSupport.Items.Add("✓ Avast");
            lbAvSupport.Items.Add("✓ AVG");
            lbAvSupport.Items.Add("✓ Avira");
            lbAvSupport.Items.Add("✓ Bitdefender");
            lbAvSupport.Items.Add("✓ ESET (Safe Mode)");
            lbAvSupport.Items.Add("✓ McAfee");
            lbAvSupport.Items.Add("✓ Norton");
            lbAvSupport.Items.Add("✓ Trend Micro");
        }

        // ────────────────────────────────────────────────────
        //  PHÁT HIỆN ANTIVIRUS (chạy trên background thread)
        // ────────────────────────────────────────────────────
        private string DetectAntivirus()
        {
            try
            {
                // SecurityCenter2 thường giữ lại entry "rác" của các bản AV cũ sau khi
                // update/gỡ cài đặt (WMI không tự dọn). Gom hết ứng viên hợp lệ rồi mới
                // chọn, ưu tiên entry có file cài đặt còn tồn tại trên đĩa.
                string fallbackName = null;

                // Fix #5: Bọc ManagementObjectSearcher trong using tránh memory leak
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    @"ROOT\SecurityCenter2",
                    "SELECT * FROM AntivirusProduct"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        // Fix #5: Bọc từng ManagementObject trong using
                        using (mo)
                        {
                            string displayName = mo["displayName"]?.ToString();
                            if (string.IsNullOrEmpty(displayName) ||
                                displayName.ToLower().Contains("windows defender"))
                            {
                                continue;
                            }

                            string productExePath = mo["pathToSignedProductExe"]?.ToString();

                            // Entry có file thực sự tồn tại → đây mới là bản AV đang cài,
                            // nhận ngay thay vì chờ duyệt hết danh sách.
                            if (!string.IsNullOrEmpty(productExePath) && File.Exists(productExePath))
                            {
                                return displayName;
                            }

                            // Không xác minh được file (entry rác hoặc thiếu path) →
                            // giữ lại làm phương án dự phòng nếu không tìm được entry nào tốt hơn.
                            if (fallbackName == null)
                            {
                                fallbackName = displayName;
                            }
                        }
                    }
                }

                return fallbackName;
            }
            catch (Exception ex)
            {
                // Fix #8: MessageBox phải chạy trên UI thread
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show(
                        $"Antivirus Detection Error: {ex.Message}",
                        "WMI ERRORS",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }));
            }
            return null;
        }

        // ────────────────────────────────────────────────────
        //  PHÁT HIỆN PHIÊN BẢN WINDOWS
        // ────────────────────────────────────────────────────
        private bool IsWindows7()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        using (obj)
                        {
                            string version = obj["Version"]?.ToString() ?? "";
                            // ProductType: 1 = Workstation, 2/3 = Domain Controller/Server.
                            // Version 6.1.x khớp cả Windows 7 lẫn Windows Server 2008 R2,
                            // nên phải lọc thêm ProductType để không nhận nhầm bản server.
                            uint productType = (uint)(obj["ProductType"] ?? 0u);
                            return version.StartsWith("6.1") && productType == 1;
                        }
                    }
                }
            }
            catch
            {
                // Nếu lỗi, coi như không phải Windows 7
            }
            return false;
        }

        // ────────────────────────────────────────────────────
        //  NÚT ANTI-VIRUS CHECK
        // ────────────────────────────────────────────────────
        private async void btnavcheck_Click_1(object sender, EventArgs e)
        {
            // Chuẩn bị UI
            lblavstatus.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            lblavstatus.Text = "Scanning...";
            txtfoundav.Text = "";
            txtDirectory.Text = "";
            btnavcheck.Enabled = false;

            // Fix #9: Disable nút download trong khi quét
            btndownload.Enabled = false;

            // Quét trong background thread để UI không bị đơ
            string detectedAvName = await Task.Run(() => DetectAntivirus());

            if (!string.IsNullOrEmpty(detectedAvName))
            {
                _detectedAvName = detectedAvName;  // Lưu tên AV
                lblavstatus.ForeColor = System.Drawing.Color.FromArgb(200, 120, 0);
                lblavstatus.Text = "Found:";
                txtfoundav.Text = detectedAvName;

                string link = GetDownloadLink(detectedAvName);
                string n = detectedAvName.ToLower();
                txtDirectory.Text = link;

                // Fix #9: Chỉ bật nút download nếu link là URL hợp lệ tuyệt đối
                btndownload.Enabled = Uri.TryCreate(link, UriKind.Absolute, out _);

                // Auto download nếu phát hiện antivirus và đây là lần đầu
                if (!_autoDownloadTriggered && btndownload.Enabled)
                {
                    _autoDownloadTriggered = true;

                    // Nếu là ESET, kiểm tra xem file đã tồn tại chưa
                    if (n.Contains("eset"))
                    {
                        string existingFile = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "Downloads", "uninstaller.exe");

                        if (File.Exists(existingFile))
                        {
                            // File đã tồn tại → bỏ qua download, mở Form2 luôn
                            LaunchForm2(existingFile);
                            return;
                        }
                    }

                    btnavcheck.Enabled = false;  // Disable nút check khi bắt đầu download

                    // Tự động click nút download sau 1 giây
                    await Task.Delay(1000);
                    btndownload_Click(btndownload, EventArgs.Empty);
                }
                else
                {
                    // Nếu không tự động download thì bật lại nút check
                    btnavcheck.Enabled = true;
                }
            }
            else
            {
                // Không tìm thấy AV bên thứ ba
                // → phân biệt theo phiên bản Windows
                bool isWin7 = await Task.Run(() => IsWindows7());

                if (isWin7)
                {
                    // Windows 7 không có Defender mạnh → khuyến cáo
                    lblavstatus.ForeColor = System.Drawing.Color.FromArgb(200, 0, 0);
                    lblavstatus.Text = "⚠️ Not Protected:";
                    txtfoundav.Text = "Windows 7 - No antivirus found";
                    txtDirectory.Text = "Recommend: Use a lightweight AV (Avast/AVG) or update to Windows 10";
                }
                else
                {
                    // Windows 10+ luôn có Windows Defender tích hợp
                    lblavstatus.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                    lblavstatus.Text = "Protected:";
                    txtfoundav.Text = "Windows Defender is active";
                    txtDirectory.Text = "System is protected by Windows Defender";
                }

                btndownload.Enabled = false;
                btnavcheck.Enabled = true;
            }
        }

        // ────────────────────────────────────────────────────
        //  MỞ FORM2 VÀ ẨN FORM1
        // ────────────────────────────────────────────────────
        private void LaunchForm2(string uninstallerPath)
        {
            Form2 safeModeFrm = new Form2(uninstallerPath);
            safeModeFrm.FormClosed += (s, args) => Application.Exit();
            safeModeFrm.Show();

            // Ẩn Form1 thay vì Close — vì Form1 là main form,
            // Close sẽ kết thúc Application.Run() và giết Form2.
            this.Hide();
        }

        // ────────────────────────────────────────────────────
        //  LẤY LINK DOWNLOAD THEO TÊN AV
        // ────────────────────────────────────────────────────
        private string GetDownloadLink(string avName)
        {
            string n = avName.ToLower();

            if (n.Contains("kaspersky"))
                return "https://media.kaspersky.com/utilities/ConsumerUtilities/kavremvr.exe";
            if (n.Contains("avast"))
                return "https://honzik.avcdn.net/setup/avast-av/release/avast_av_clear.exe";
            if (n.Contains("avg"))
                return "https://honzik.avcdn.net/setup/avg-av/release/avg_av_clear.exe?alt=en-ww";
            if (n.Contains("avira"))
                return "https://github.com/ThanhNguyenVN93/Avira-Uninstaller-Support-Tool/releases/download/avira/Avira.Spotlight.Uninstaller.Tool.exe";
            if (n.Contains("bitdefender"))
                return "https://www.bitdefender.com/files/KnowledgeBase/file/Bitdefender_2023_Uninstall_Tool.exe";
            if (n.Contains("eset"))
                return "https://download.eset.com/com/eset/tools/installers/eset_apps_remover/latest/uninstaller.exe";
            if (n.Contains("mcafee"))
                return "https://download.mcafee.com/molbin/iss-loc/SupportTools/MCPR/MCPR.exe";
            if (n.Contains("norton"))
                return "https://honzik.avcdn.net/setup/norton-suite/release/norton_360_remover.exe";
            if (n.Contains("trend micro"))
                return "https://github.com/ThanhNguyenVN93/Trendmicro-Uninstaller-Support-Tool/releases/download/v1.0.0/Trendmicro.Uninstaller.Support.Tool.zip";

            return "Contact for feedback and support.";
        }

        // ────────────────────────────────────────────────────
        //  NÚT DOWNLOAD
        // ────────────────────────────────────────────────────
        private void btndownload_Click(object sender, EventArgs e)
        {
            string downloadUrl = txtDirectory.Text;

            if (string.IsNullOrEmpty(downloadUrl) ||
                !Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri uriResult))
            {
                MessageBox.Show("Invalid download link.", "ERRORS",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                lblstatus.Text = "Starting...";
                lblspeed.Text = "0 KB/s";
                progressBarDownload.Value = 0;
                btndownload.Enabled = false;

                // Fix #4: Dispose WebClient cũ nếu tồn tại trước khi tạo mới
                _webClient?.Dispose();
                _webClient = new WebClient();

                string fileName = Path.GetFileName(uriResult.LocalPath);

                // Fix #3: Lưu đường dẫn vào field để WebClient_DownloadFileCompleted dùng lại
                _lastSavePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    fileName);

                _webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                _webClient.DownloadFileCompleted   += WebClient_DownloadFileCompleted;

                // Fix #7: Reset Stopwatch trước khi bắt đầu tải
                _lastBytesReceived = 0;
                _downloadStopwatch.Restart();

                _webClient.DownloadFileAsync(uriResult, _lastSavePath);
            }
            catch (Exception ex)
            {
                lblstatus.Text = "Download Initialization Error.";
                MessageBox.Show($"Download Error: {ex.Message}", "ERRORS",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btndownload.Enabled = true;
            }
        }

        // ────────────────────────────────────────────────────
        //  THEO DÕI TIẾN TRÌNH DOWNLOAD
        // ────────────────────────────────────────────────────
        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            // Một số server (vd: các link mirror AV) không trả Content-Length →
            // TotalBytesToReceive = -1 và ProgressPercentage có thể ra ngoài [0,100],
            // gán thẳng vào ProgressBar.Value sẽ ném ArgumentOutOfRangeException.
            int percent = Math.Max(0, Math.Min(100, e.ProgressPercentage));
            progressBarDownload.Value = percent;
            lblstatus.Text = e.TotalBytesToReceive > 0
                ? $"Downloading... ({percent}%)"
                : "Downloading...";

            double receivedMB = (double)e.BytesReceived / 1_048_576;

            // Fix #7: Tốc độ thực = (bytes mới nhận) / (thời gian thực trôi qua)
            double elapsedSec = _downloadStopwatch.Elapsed.TotalSeconds;
            double speedKBps  = elapsedSec > 0
                ? (e.BytesReceived - _lastBytesReceived) / elapsedSec / 1024.0
                : 0;

            _lastBytesReceived = e.BytesReceived;
            _downloadStopwatch.Restart();

            string totalText = e.TotalBytesToReceive > 0
                ? $"{(double)e.TotalBytesToReceive / 1_048_576:N2} MB"
                : "? MB";
            lblspeed.Text = $"{receivedMB:N2} MB / {totalText}  |  {speedKBps:N0} KB/s";
        }

        // ────────────────────────────────────────────────────
        //  DOWNLOAD HOÀN THÀNH
        // ────────────────────────────────────────────────────
        private async void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            _downloadStopwatch.Stop();

            // Fix #4: Dispose WebClient sau khi hoàn tất
            _webClient?.Dispose();
            _webClient = null;

            btndownload.Enabled = true;
            btnavcheck.Enabled = true;  // Bật lại nút check sau download

            if (e.Error != null)
            {
                lblstatus.Text = "Download Failed!";
                MessageBox.Show($"Download Unsuccessful: {e.Error.Message}",
                    "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (e.Cancelled)
            {
                lblstatus.Text = "Download Canceled.";
            }
            else
            {
                lblstatus.Text = "Complete!";
                MessageBox.Show(
                    "Download complete! Processing file...",
                    "Success!!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                try
                {
                    // Nếu là ESET → chạy Form2 (Safe Mode Uninstaller)
                    if (!string.IsNullOrEmpty(_detectedAvName) && 
                        _detectedAvName.ToLower().Contains("eset"))
                    {
                        // Kiểm tra file được tải xuống
                        if (string.IsNullOrEmpty(_lastSavePath) || !File.Exists(_lastSavePath))
                        {
                            MessageBox.Show("Uninstaller file not found at expected location.",
                                "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        LaunchForm2(_lastSavePath);
                    }
                    else
                    {
                        // Các antivirus khác → chạy exe ngay lập tức
                        if (File.Exists(_lastSavePath))
                        {
                            if (_lastSavePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                // Chạy file exe vừa tải
                                Process.Start(_lastSavePath);

                                // Chờ 1 giây rồi thoát app
                                await Task.Delay(1000);
                                Application.Exit();
                            }
                            else
                            {
                                // File không phải .exe (vd: Trend Micro tải về là .zip)
                                // → không thể auto-run, mở thư mục chứa file cho người dùng tự giải nén/chạy.
                                MessageBox.Show(
                                    $"File đã tải về nhưng không thể tự chạy (không phải .exe):\n{_lastSavePath}\n\nVui lòng giải nén/chạy thủ công.",
                                    "Manual Action Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                Process.Start("explorer.exe", $"/select,\"{_lastSavePath}\"");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Downloaded file not found.",
                                "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}",
                        "System Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}

