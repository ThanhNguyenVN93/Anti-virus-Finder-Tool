using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace frmavfinnder
{
    public partial class Form2 : Form
    {
        private string _uninstallerPath;

        public Form2(string uninstallerPath)
        {
            InitializeComponent();
            _uninstallerPath = uninstallerPath;
        }

        // ────────────────────────────────────────────────────
        //  FORM LOAD
        // ────────────────────────────────────────────────────
        private void Form2_Load(object sender, EventArgs e)
        {
            lblstatus.Text = "Checking Safe Mode status...";
            lbldetails.Text = "Initializing system...";
            lblstep.Text = "";

            if (IsInSafeMode())
            {
                lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                lblstatus.Text = "✓ Currently in Safe Mode";
                lblmode.Text = "Safe Mode Minimal detected";
                lbldetails.Text = "Ready to run uninstaller";
                lblstep.Text = "Step 1/2: Running uninstaller...";

                // Đã trong Safe Mode → chạy uninstaller ngay
                RunUninstaller();
            }
            else
            {
                lblstatus.ForeColor = System.Drawing.Color.FromArgb(200, 120, 0);
                lblstatus.Text = "⚠️ Not in Safe Mode";
                lblmode.Text = "Rebooting into Safe Mode Minimal...";
                lbldetails.Text = "This will restart your computer";
                lblstep.Text = "Step 1/3: Configuring Safe Mode...";

                // Chưa trong Safe Mode → boot vào Safe Mode ngay
                BootIntoSafeMode();
            }
        }

        // ────────────────────────────────────────────────────
        //  KIỂM TRA SAFE MODE
        // ────────────────────────────────────────────────────
        private bool IsInSafeMode()
        {
            // Cách đáng tin cậy nhất: dùng SystemInformation.BootMode
            return System.Windows.Forms.SystemInformation.BootMode != BootMode.Normal;
        }

        // ────────────────────────────────────────────────────
        //  THƯ MỤC SYSTEM32 THỰC (tránh WoW64 File System Redirection)
        // ────────────────────────────────────────────────────
        private string GetSystemDirectory()
        {
            // Redirection chỉ xảy ra khi process 32-bit chạy trên OS 64-bit.
            // Nếu bản thân OS là 32-bit thì không có Sysnative, phải dùng System32 như cũ.
            bool isWow64 = !Environment.Is64BitProcess && Environment.Is64BitOperatingSystem;

            return isWow64
                ? Path.Combine(Environment.GetEnvironmentVariable("windir"), "Sysnative")
                : Environment.GetFolderPath(Environment.SpecialFolder.System);
        }

        // ────────────────────────────────────────────────────
        //  BOOT VÀO SAFE MODE MINIMAL
        // ────────────────────────────────────────────────────
        private void BootIntoSafeMode()
        {
            try
            {
                lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
                lblstatus.Text = "Configuring...";
                lblmode.Text = "Setting Safe Mode boot configuration";
                lbldetails.Text = "Executing bcdedit command...";
                lblstep.Text = "Step 2/3: Executing bcdedit...";

                string sysDir = GetSystemDirectory();
                string bcdeditPath = Path.Combine(sysDir, "bcdedit.exe");
                string shutdownPath = Path.Combine(sysDir, "shutdown.exe");

                if (!File.Exists(bcdeditPath) || !File.Exists(shutdownPath))
                {
                    MessageBox.Show(
                        $"Không tìm thấy bcdedit.exe / shutdown.exe tại:\n{sysDir}",
                        "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Bước 1: Set safeboot minimal
                ProcessStartInfo bcdeditPsi = new ProcessStartInfo(bcdeditPath)
                {
                    Arguments = "/set {current} safeboot minimal",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                string errorOutput;
                int exitCode;
                using (var proc = Process.Start(bcdeditPsi))
                {
                    errorOutput = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();
                    exitCode = proc.ExitCode;
                }

                if (exitCode != 0)
                {
                    MessageBox.Show(
                        $"bcdedit failed (exit code {exitCode}).\n{errorOutput}",
                        "bcdedit Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Bước 2: bcdedit đã ghi xong → restart ngay
                lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                lblstatus.Text = "✓ Configuration Complete";
                lblmode.Text = "System restarting now";
                lbldetails.Text = "Please wait while your computer restarts...";
                lblstep.Text = "Step 3/3: Restarting system...";

                Process.Start(shutdownPath, "/r /t 0");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nPlease restart manually into Safe Mode.",
                    "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        // ────────────────────────────────────────────────────
        //  CHẠY UNINSTALLER
        // ────────────────────────────────────────────────────
        private async void RunUninstaller()
        {
            try
            {
                if (!File.Exists(_uninstallerPath))
                {
                    MessageBox.Show($"Uninstaller not found: {_uninstallerPath}",
                        "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.Close();
                    return;
                }

                lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
                lblstatus.Text = "Launching uninstaller...";
                lblmode.Text = $"Running: {Path.GetFileName(_uninstallerPath)}";
                lbldetails.Text = "Please wait while the uninstaller completes...";

                // Chạy uninstaller và đợi nó kết thúc
                Process uninstallerProc = Process.Start(_uninstallerPath);

                // Đợi uninstaller thoát trên background thread để UI không đơ
                await Task.Run(() => uninstallerProc.WaitForExit());

                lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                lblstatus.Text = "✓ Uninstaller finished";
                lblmode.Text = "Cleaning up...";
                lbldetails.Text = "Removing Safe Mode configuration...";
                lblstep.Text = "Step 2/2: Cleaning up...";

                // Xóa safeboot flag khỏi BCD → lần restart tiếp sẽ vào Normal Mode
                BootBackToNormal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running uninstaller: {ex.Message}",
                    "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        // ────────────────────────────────────────────────────
        //  XÓA SAFEBOOT FLAG VÀ RESTART VỀ NORMAL MODE
        // ────────────────────────────────────────────────────
        private void BootBackToNormal()
        {
            try
            {
                string sysDir = GetSystemDirectory();
                string bcdeditPath = Path.Combine(sysDir, "bcdedit.exe");
                string shutdownPath = Path.Combine(sysDir, "shutdown.exe");

                lbldetails.Text = "Executing bcdedit delete command...";
                lblstep.Text = "Final: Removing Safe Mode flag...";

                // Xóa safeboot khỏi BCD → boot tiếp sẽ vào Normal Mode
                ProcessStartInfo bcdeditPsi = new ProcessStartInfo(bcdeditPath)
                {
                    Arguments = "/deletevalue {current} safeboot",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                int exitCode;
                using (var proc = Process.Start(bcdeditPsi))
                {
                    proc.StandardError.ReadToEnd();
                    proc.WaitForExit();
                    exitCode = proc.ExitCode;
                }

                if (exitCode != 0)
                {
                    MessageBox.Show(
                        "Could not remove Safe Mode boot flag.\nAfter restart, manually run:\nbcdedit /deletevalue {current} safeboot",
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                lblstatus.Text = "✓ Success!";
                lblmode.Text = "Antivirus removed successfully";
                lbldetails.Text = "Restarting to Normal Mode...";
                lblstep.Text = "✓ All steps completed!";
                lblstep.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
                progressBar1.Value = 100;

                MessageBox.Show(
                    "Antivirus has been uninstalled!\n\nSystem will restart into Normal Mode.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Restart về Normal Mode
                Process.Start(shutdownPath, "/r /t 0");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nPlease manually run:\nbcdedit /deletevalue {{current}} safeboot\nThen restart your PC.",
                    "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }
    }
}
