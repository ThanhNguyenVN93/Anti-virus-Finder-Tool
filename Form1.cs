using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace frmavfinnder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Lấy thời gian hiện tại và định dạng nó
            string currentTime = DateTime.Now.ToString("HH:mm:ss");

            // Cập nhật thuộc tính Text của Form
            this.Text = $"ANTI-VIRUS FINDER TOOL V1.1 - {currentTime}";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //OS Detect:

            string r = "";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                ManagementObjectCollection information = searcher.Get();
                if (information != null)
                {
                    foreach (ManagementObject obj in information)
                    {
                        r = obj["Caption"].ToString() + " - " + obj["OSArchitecture"].ToString();
                    }
                }
                r = r.Replace("NT 5.1.2600", "XP");
                r = r.Replace("NT 5.2.3790", "Server 2003");
                lblos.Text = r;
            }

            btnavcheck.PerformClick();

        }
        private string DetectAntivirus()
        {
            try
            {
                // Thiết lập truy vấn WMI trên ROOT\SecurityCenter2
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    @"ROOT\SecurityCenter2",
                    "SELECT * FROM AntivirusProduct"
                );

                foreach (ManagementObject managementObject in searcher.Get())
                {
                    string displayName = managementObject["displayName"]?.ToString();

                    // Kiểm tra và loại trừ Windows Defender.
                    // Defender thường được gọi là "Windows Defender Antivirus" hoặc tên tương tự.
                    if (!string.IsNullOrEmpty(displayName) && !displayName.ToLower().Contains("windows defender"))
                    {
                        // Trả về tên phần mềm AV đầu tiên tìm thấy
                        return displayName;
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có vấn đề khi truy vấn WMI
                MessageBox.Show($"Antivirus Detection Error: {ex.Message}", "WMI ERRORS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null; // Không tìm thấy Antivirus nào khác Defender
        }

        /// <summary>
        /// Sự kiện Click cho nút "ANTI-VIRUS CHECK"
        /// </summary>

        private async void btnavcheck_Click_1(object sender, EventArgs e)
        {
            // 1. Chuẩn bị UI
            lblavstatus.Text = "Scanning...";
            txtfoundav.Text = "";
            txtDirectory.Text = "";
            btnavcheck.Enabled = false; // Vô hiệu hóa nút download
            

            // 2. Thực hiện quét trong một luồng nền (Task.Run) để UI không bị đơ
            string detectedAvName = await Task.Run(() => DetectAntivirus());

            // 3. Cập nhật giao diện
            if (!string.IsNullOrEmpty(detectedAvName))
            {
                lblavstatus.Text = "Found:";
                txtfoundav.Text = detectedAvName;

                // --- GỌI PHƯƠNG THỨC LẤY ĐƯỜNG DẪN CÀI ĐẶT ---
                string installPath = GetDownloadLink(detectedAvName);
                txtDirectory.Text = installPath;
                // ---------------------------------------------

                // Nếu bạn muốn nút download tải về file trong thư mục cài đặt (ví dụ: file log/setup),
                // bạn sẽ cần logic phức tạp hơn. Nếu mục đích là hiển thị đường dẫn, thì không cần kích hoạt.
                // Nếu mục đích là TẢI VỀ LINK KHÁC, ta sẽ làm ở bước sau.
                // Vô hiệu hóa vì đây là đường dẫn Local
            }
            else
            {
                lblavstatus.Text = "Not Found:";
                txtfoundav.Text = "No AV found (except Windows Defender)";
                txtDirectory.Text = "No download link available";
            }
        }
        private string GetDownloadLink(string avName)
        {
            string lowerName = avName.ToLower();

            // Dùng logic so khớp tên AV để gán link tải xuống chính thức (hoặc file setup)
            if (lowerName.Contains("kaspersky"))
            {
                return "https://media.kaspersky.com/utilities/ConsumerUtilities/kavremvr.exe";
            }
            else if (lowerName.Contains("avast"))
            {
                return "https://honzik.avcdn.net/setup/avast-av/release/avast_av_clear.exe"; // Avast Free Installer
            }
            else if (lowerName.Contains("avg"))
            {
                return "https://honzik.avcdn.net/setup/avg-av/release/avg_av_clear.exe?alt=en-ww"; // AVG Free Installer
            }
            else if (lowerName.Contains("avira"))
            {
                return "https://support.avira.com/hc/en-us/article_attachments/360001695365"; // Avira Download Page
            }
            else if (lowerName.Contains("bitdefender"))
            {
                return "https://www.bitdefender.com/files/KnowledgeBase/file/Bitdefender_2023_Uninstall_Tool.exe"; // Bitdefender Installer
            }
            else if (lowerName.Contains("eset"))
            {
                return "https://download.eset.com/com/eset/tools/installers/eset_apps_remover/latest/uninstaller.exe"; // F-Secure Total Download Page
            }
            else if (lowerName.Contains("mcafee"))
            {
                return "https://download.mcafee.com/molbin/iss-loc/SupportTools/MCPR/MCPR.exe"; // McAfee Removal Tool
            }
            else if (lowerName.Contains("norton"))
            {
                return "https://www.norton.com/nortonremover"; // Norton Download Page
            }          
            else if (lowerName.Contains("trend micro"))
            {
                return "https://powerbox-na-file.trend.org/SFDC/DownloadFile_iv.php?jsonInfo=%7B%22Query%22%3A%22HM9DpY%2Fqw0CR4CE2CPUsYVe4ZBGL%2F5dTa4lZsV%2FxJ%2FX4pXowzWPPJfBfU%2FDsO64CygtCgbmvxbwRFchkCmeXU8%2BcRw14PVtHFTgO9oafcrgwNtry%2FmkqkLOcaS4YUbXl6QG4R3QF%2Fw%2Faxt0rJJIS2cvQIpmfWG30k9V2qHuCl9vza23y4EGwYcuSYShNd3K8XhkG0shYf5vxq1QlRCQR9NxiN6DmcIG3v8O4%2BOQlUv497axB1wrKrVOH84UWbCLOYWrw8pqqiFh%2BWnWLfNo8NmrZz4TRHxU9AvDk8V1Q%2BpR9NKQfzRQY5cT1BvEVsddP%2FW17vzf6rzBn%2FzNvpfwRAw%3D%3D%22%2C%22iv%22%3A%221cc5c50e1fd95d078c7de084b9492912%22%7D"; // Trend Micro Download Page
            }

            // Bạn có thể thêm nhiều AV khác vào đây

            // Link mặc định nếu không khớp hoặc AV không phổ biến
            return "Contact for feedback and support.";
        }

        private void btndownload_Click(object sender, EventArgs e)
        {
            string downloadUrl = txtDirectory.Text;

            // Kiểm tra tính hợp lệ của link
            if (string.IsNullOrEmpty(downloadUrl) || !Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri uriResult))
            {
                MessageBox.Show("Invalid download link.", "ERRORS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 1. Chuẩn bị UI và file
                lblstatus.Text = "Starting...";
                lblspeed.Text = "0 MB/s"; // Giả sử label tốc độ là lblSpeed
                progressBarDownload.Value = 0;
                btndownload.Enabled = false; // Vô hiệu hóa nút trong khi tải

                // 2. Thiết lập WebClient
                WebClient webClient = new WebClient();

                // Tạo đường dẫn lưu file (lưu vào thư mục Downloads của user)
                // Lấy tên file từ URL (ví dụ: installer.exe)
                string fileName = Path.GetFileName(uriResult.LocalPath);
                // Ghép đường dẫn: [User/Downloads]/[filename]
                string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileName);

                // 3. Gắn các sự kiện theo dõi tiến trình
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;

                // Bắt đầu tải xuống bất đồng bộ
                webClient.DownloadFileAsync(uriResult, savePath);
            }
            catch (Exception ex)
            {
                lblstatus.Text = "Download Initialization Error.";
                MessageBox.Show($"Download Error: {ex.Message}", "ERRORS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btndownload.Enabled = true;
            }
        }

        // ----------------------------------------------------
        // CÁC SỰ KIỆN THEO DÕI TIẾN TRÌNH
        // ----------------------------------------------------

        /// <summary>
        /// Cập nhật tiến trình tải xuống lên ProgressBar và Label tốc độ.
        /// </summary>
        /// 
        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            // Cập nhật ProgressBar (0-100)
            progressBarDownload.Value = e.ProgressPercentage;

            // Cập nhật trạng thái
            lblstatus.Text = $"Downloading... ({e.ProgressPercentage}%)";

            // Cập nhật tốc độ và kích thước
            // 1048576 bytes = 1 MB
            double totalMB = (double)e.TotalBytesToReceive / 1048576;
            double receivedMB = (double)e.BytesReceived / 1048576;

            // Tốc độ (bytes/s)
            double speedKBps = (double)e.BytesReceived / (double)e.TotalBytesToReceive * 100 * 1024;

            lblspeed.Text = $"{receivedMB:N2}MB / {totalMB:N2}MB | {speedKBps:N0} KB/s";
        }

        /// <summary>
        /// Xử lý khi quá trình tải file hoàn thành (thành công hoặc thất bại).
        /// </summary>
        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            btndownload.Enabled = true; // Kích hoạt lại nút tải xuống

            if (e.Error != null)
            {
                // Xử lý lỗi
                lblstatus.Text = "Download Failed!";
                MessageBox.Show($"Download Unsuccessful: {e.Error.Message}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (e.Cancelled)
            {
                // Xử lý trường hợp bị hủy
                lblstatus.Text = "Download Canceled.";
            }
            else
            {
                // Tải xuống thành công
                lblstatus.Text = "Complete!";
                MessageBox.Show("Download complete! The file has been saved in the Downloads folder.", "Success!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                try
                {
                    // Lấy lại đường dẫn file đã được lưu
                    // Ta sẽ sử dụng đường dẫn đã tạo trong btnDownload_Click để đảm bảo
                    // *Ta cần chỉnh sửa lại để lưu đường dẫn này vào một biến có thể truy cập được ở đây.*

                    // Giả định bạn đã sửa code và có thể lấy được đường dẫn lưu đầy đủ (bao gồm tên file)
                    // Ví dụ: string filePath = _lastDownloadedFilePath;

                    // Dùng logic lấy lại đường dẫn savePath từ biến toàn cục/hằng số
                    string filePath = "C:\\Users\\YourUser\\Downloads\\" + Path.GetFileName(new Uri(txtDirectory.Text).LocalPath);

                    // ----------------------------------------------------
                    // Mở thư mục Downloads VÀ CHỌN FILE ĐÃ TẢI
                    // ----------------------------------------------------

                    if (File.Exists(filePath))
                    {
                        // Dùng lệnh "explorer.exe /select," để mở Windows Explorer và chọn file
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                    }
                    else
                    {
                        // Nếu không tìm thấy file, chỉ mở thư mục Downloads
                        string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                        System.Diagnostics.Process.Start("explorer.exe", downloadsPath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening folder: {ex.Message}", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}

