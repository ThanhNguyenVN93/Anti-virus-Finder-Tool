using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace frmavfinnder
{
    public partial class Form2 : Form
    {
        private readonly string _uninstallerPath;
        private readonly string _avName;
        private readonly AppState _state;

        // Đếm ngược reboot (Feature #2).
        private Timer _countdownTimer;
        private int _countdownRemaining;
        private Action _countdownAction;
        private Button _btnCancelReboot;

        public Form2(string uninstallerPath, string avName, AppState state)
        {
            InitializeComponent();
            _uninstallerPath = uninstallerPath;
            _avName = avName ?? "";
            _state = state ?? AppState.Load();
            SetupCancelButton();
        }

        // Thêm nút HỦY bằng code (không đụng Designer) cho phần đếm ngược reboot.
        private void SetupCancelButton()
        {
            _btnCancelReboot = new Button
            {
                Text = "HỦY REBOOT",
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                Size = new System.Drawing.Size(130, 34),
                Location = new System.Drawing.Point(345, 205),
                FlatStyle = FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(200, 60, 60),
                ForeColor = System.Drawing.Color.White,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnCancelReboot.FlatAppearance.BorderSize = 0;
            _btnCancelReboot.Click += (s, e) => CancelCountdown();
            this.Controls.Add(_btnCancelReboot);
            _btnCancelReboot.BringToFront();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            lblstatus.Text = "Checking Safe Mode status...";
            lbldetails.Text = "Initializing system...";
            lblstep.Text = "";

            if (SafeBoot.IsInSafeMode)
            {
                lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                lblstatus.Text = "✓ Currently in Safe Mode";
                lblmode.Text = "Safe Mode Minimal detected";
                lbldetails.Text = "Ready to run uninstaller";
                lblstep.Text = "Step 1/2: Running uninstaller...";
                RunUninstaller();
            }
            else
            {
                lblstatus.ForeColor = System.Drawing.Color.FromArgb(200, 120, 0);
                lblstatus.Text = "⚠️ Not in Safe Mode";
                lblmode.Text = "Need to reboot into Safe Mode Minimal";
                lbldetails.Text = $"AV cần gỡ: {_avName}";
                lblstep.Text = "Chuẩn bị cấu hình Safe Mode...";
                BootIntoSafeMode();
            }
        }

        // ────────────────────────────────────────────────────
        //  ĐĂNG KÝ TỰ CHẠY LẠI SAU KHI VÀO SAFE MODE
        // ────────────────────────────────────────────────────
        private void RegisterSafeModeAutoRelaunch()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", writable: true))
                {
                    key?.SetValue("*AntiVirusFinderSafeModeResume",
                        $"\"{Application.ExecutablePath}\"");
                }
            }
            catch (Exception ex)
            {
                Log.Write($"RegisterSafeModeAutoRelaunch failed: {ex.Message}");
                MessageBox.Show(
                    $"Không thể đăng ký tự khởi động lại trong Safe Mode: {ex.Message}\n\n" +
                    "Sau khi khởi động lại, vui lòng tự mở lại ứng dụng.",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ────────────────────────────────────────────────────
        //  CẤU HÌNH + REBOOT VÀO SAFE MODE
        // ────────────────────────────────────────────────────
        private void BootIntoSafeMode()
        {
            try
            {
                // Xác nhận file hợp lệ trước khi đụng tới cấu hình boot.
                if (!File.Exists(_uninstallerPath))
                {
                    MessageBox.Show($"Uninstaller not found: {_uninstallerPath}",
                        "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.Close();
                    return;
                }

                lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
                lblstatus.Text = "Configuring...";
                lblmode.Text = "Setting Safe Mode boot configuration";
                lbldetails.Text = "Executing bcdedit command...";
                lblstep.Text = "Đang chạy bcdedit...";

                if (!File.Exists(SafeBoot.BcdeditPath) || !File.Exists(SafeBoot.ShutdownPath))
                {
                    MessageBox.Show(
                        $"Không tìm thấy bcdedit.exe / shutdown.exe tại:\n{SafeBoot.GetSystemDirectory()}",
                        "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                int exitCode = SafeBoot.RunBcdedit("/set {current} safeboot minimal", out string err);
                if (exitCode != 0)
                {
                    MessageBox.Show(
                        $"bcdedit failed (exit code {exitCode}).\n{err}",
                        "bcdedit Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                RegisterSafeModeAutoRelaunch();

                // Feature round3 #1: lưới an toàn — nếu app không tự dọn được,
                // task này sẽ xóa cờ + reboot sau 20 phút.
                Watchdog.Arm(20);

                lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                lblstatus.Text = "✓ Configuration Complete";
                lblmode.Text = "System will restart into Safe Mode";

                // Feature #2: đếm ngược + cho hủy. Nếu hủy thì rollback cờ safeboot.
                StartCountdown(10, "khởi động lại vào Safe Mode",
                    onFire: () => SafeBoot.Restart(),
                    onCancel: () =>
                    {
                        SafeBoot.ClearSafeBootFlag();   // rollback để không bị kẹt
                        _state.Reset();
                        lblstatus.ForeColor = System.Drawing.Color.FromArgb(200, 120, 0);
                        lblstatus.Text = "Đã hủy";
                        lblmode.Text = "Đã rollback cấu hình Safe Mode";
                        lbldetails.Text = "Không có thay đổi nào với hệ thống.";
                        lblstep.Text = "";
                    });
            }
            catch (Exception ex)
            {
                Log.Write($"BootIntoSafeMode error: {ex.Message}");
                // Nếu đã lỡ set safeboot thì cố xóa để không kẹt.
                SafeBoot.ClearSafeBootFlag();
                MessageBox.Show($"Error: {ex.Message}\n\nĐã cố khôi phục cấu hình boot.",
                    "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        // ────────────────────────────────────────────────────
        //  CHẠY UNINSTALLER (đang ở Safe Mode)
        // ────────────────────────────────────────────────────
        private async void RunUninstaller()
        {
            try
            {
                // Fix C2: trong Safe Mode, bất kỳ lỗi nào cũng phải xóa safeboot
                // trước khi thoát, nếu không máy kẹt ở Safe Mode.
                if (!File.Exists(_uninstallerPath))
                {
                    FailInSafeMode($"Uninstaller not found: {_uninstallerPath}");
                    return;
                }

                // Xác minh chữ ký lần nữa (phòng file bị tráo giữa 2 lần boot) — H1.
                if (_uninstallerPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    var verify = SignatureVerifier.Verify(_uninstallerPath, null);
                    if (!verify.IsTrusted)
                    {
                        FailInSafeMode($"Xác minh chữ ký uninstaller thất bại: {verify.Message}");
                        return;
                    }
                }

                lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
                lblstatus.Text = "Launching uninstaller...";
                lblmode.Text = $"Running: {Path.GetFileName(_uninstallerPath)}";
                lbldetails.Text = "Please wait while the uninstaller completes...";

                Process uninstallerProc = Process.Start(_uninstallerPath);

                int exitCode = 0;
                if (uninstallerProc != null)
                {
                    // Feature round3 #5: timeout để uninstaller treo không làm kẹt vĩnh viễn.
                    const int timeoutMs = 10 * 60 * 1000; // 10 phút
                    bool exited = await Task.Run(() => uninstallerProc.WaitForExit(timeoutMs));
                    while (!exited)
                    {
                        var keep = MessageBox.Show(
                            "Uninstaller đã chạy hơn 10 phút và chưa kết thúc.\n\n" +
                            "[Yes] Chờ thêm   •   [No] Dừng chờ và dọn cấu hình Safe Mode",
                            "Uninstaller chạy lâu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (keep == DialogResult.Yes)
                        {
                            exited = await Task.Run(() => uninstallerProc.WaitForExit(timeoutMs));
                        }
                        else
                        {
                            FailInSafeMode("Người dùng dừng chờ uninstaller (chạy quá lâu).");
                            return;
                        }
                    }
                    exitCode = uninstallerProc.ExitCode;
                    Log.Write($"Uninstaller exit code: {exitCode}");
                }

                // Fix H2: báo đúng kết quả theo exit code, không mặc định "thành công".
                if (uninstallerProc != null && exitCode != 0)
                {
                    var choice = MessageBox.Show(
                        $"Uninstaller kết thúc với mã lỗi {exitCode} (có thể chưa gỡ xong hoặc bị hủy).\n\n" +
                        "Vẫn xóa cấu hình Safe Mode và khởi động về Normal Mode?",
                        "Uninstaller Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (choice != DialogResult.Yes)
                    {
                        // Người dùng muốn thử lại → không xóa safeboot, giữ nguyên để chạy lại.
                        lblstatus.Text = "Đang chờ thao tác thủ công trong Safe Mode";
                        lbldetails.Text = "Cấu hình Safe Mode vẫn giữ. Đóng app khi xong để tự xóa cờ.";
                        return;
                    }
                    // else rơi xuống dọn dẹp bên dưới.
                }

                lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                lblstatus.Text = "✓ Uninstaller finished";
                lblmode.Text = "Cleaning up...";
                lbldetails.Text = "Removing Safe Mode configuration...";
                lblstep.Text = "Step 2/2: Cleaning up...";

                // Ghi nhận đã xử lý xong AV này.
                _state.LastResultAv = _avName;
                _state.Phase = "completed";
                _state.DownloadVerified = false;
                _state.DownloadedFile = "";
                _state.Save();

                BootBackToNormal();
            }
            catch (Exception ex)
            {
                Log.Write($"RunUninstaller error: {ex.Message}");
                FailInSafeMode($"Error running uninstaller: {ex.Message}");
            }
        }

        // Dọn an toàn khi gặp lỗi trong Safe Mode: xóa cờ để không kẹt.
        private void FailInSafeMode(string reason)
        {
            Log.Write($"FailInSafeMode: {reason}");
            bool cleared = SafeBoot.ClearSafeBootFlag();
            MessageBox.Show(
                $"{reason}\n\n" +
                (cleared
                    ? "Đã xóa cấu hình Safe Mode. Vui lòng khởi động lại về Normal Mode."
                    : "Không tự xóa được cấu hình Safe Mode. Hãy chạy thủ công:\nbcdedit /deletevalue {current} safeboot\nrồi khởi động lại."),
                "Safe Mode Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.Close();
        }

        // ────────────────────────────────────────────────────
        //  XÓA SAFEBOOT + REBOOT VỀ NORMAL
        // ────────────────────────────────────────────────────
        private void BootBackToNormal()
        {
            try
            {
                lbldetails.Text = "Executing bcdedit delete command...";
                lblstep.Text = "Removing Safe Mode flag...";

                int exitCode = SafeBoot.RunBcdedit("/deletevalue {current} safeboot", out string err);
                Watchdog.Disarm();   // đã dọn xong → gỡ lưới an toàn
                if (exitCode != 0)
                {
                    Log.Write($"BootBackToNormal: delete failed {exitCode} {err}");
                    MessageBox.Show(
                        "Could not remove Safe Mode boot flag.\nAfter restart, manually run:\n" +
                        "bcdedit /deletevalue {current} safeboot",
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                lblstatus.Text = "✓ Success!";
                lblmode.Text = $"{_avName} removed";
                lbldetails.Text = "Restarting to Normal Mode...";
                lblstep.Text = "✓ All steps completed!";
                lblstep.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                progressBar1.Style = ProgressBarStyle.Continuous;
                progressBar1.Value = 100;

                StartCountdown(10, "khởi động lại về Normal Mode",
                    onFire: () => SafeBoot.Restart(),
                    onCancel: () =>
                    {
                        lblmode.Text = "Đã hủy tự restart.";
                        lbldetails.Text = "Cờ Safe Mode đã được xóa. Hãy tự khởi động lại khi sẵn sàng.";
                        lblstep.Text = "";
                    });
            }
            catch (Exception ex)
            {
                Log.Write($"BootBackToNormal error: {ex.Message}");
                MessageBox.Show(
                    $"Error: {ex.Message}\n\nPlease manually run:\n" +
                    "bcdedit /deletevalue {current} safeboot\nThen restart your PC.",
                    "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        // ────────────────────────────────────────────────────
        //  ĐẾM NGƯỢC REBOOT CÓ NÚT HỦY (Feature #2)
        // ────────────────────────────────────────────────────
        private void StartCountdown(int seconds, string whatHappens, Action onFire, Action onCancel)
        {
            _countdownRemaining = seconds;
            _countdownAction = onFire;

            _btnCancelReboot.Visible = true;
            _btnCancelReboot.BringToFront();

            _countdownTimer?.Stop();
            _countdownTimer?.Dispose();
            _countdownTimer = new Timer { Interval = 1000 };

            Action pendingCancel = onCancel;

            UpdateCountdownLabel(whatHappens);

            _countdownTimer.Tick += (s, e) =>
            {
                _countdownRemaining--;
                if (_countdownRemaining <= 0)
                {
                    _countdownTimer.Stop();
                    _btnCancelReboot.Visible = false;
                    _countdownAction?.Invoke();
                }
                else
                {
                    UpdateCountdownLabel(whatHappens);
                }
            };

            // Lưu lại hành động hủy để nút dùng.
            _cancelHandler = () =>
            {
                _countdownTimer?.Stop();
                _btnCancelReboot.Visible = false;
                pendingCancel?.Invoke();
            };

            _countdownTimer.Start();
        }

        private Action _cancelHandler;

        private void CancelCountdown()
        {
            _cancelHandler?.Invoke();
        }

        private void UpdateCountdownLabel(string whatHappens)
        {
            lblstep.Text = $"Sẽ {whatHappens} sau {_countdownRemaining}s... (bấm HỦY để dừng)";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Nếu đóng app khi đang ở Safe Mode mà cờ còn → xóa để không kẹt.
            try
            {
                _countdownTimer?.Stop();
                if (SafeBoot.IsInSafeMode)
                {
                    SafeBoot.ClearSafeBootFlag();
                    Log.Write("Form2 closing in Safe Mode — cleared safeboot flag as precaution.");
                }
            }
            catch { }
            base.OnFormClosing(e);
        }
    }
}
