using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  THAO TÁC SAFE MODE / BCD DÙNG CHUNG
    //  Gom về một chỗ để mọi đường thoát lỗi (Fix C2, C3) đều có thể
    //  xóa cờ safeboot, không để máy kẹt ở Safe Mode.
    // ──────────────────────────────────────────────────────────────
    internal static class SafeBoot
    {
        public static bool IsInSafeMode =>
            SystemInformation.BootMode != BootMode.Normal;

        // Thư mục System32 thực (tránh WoW64 redirection khi 32-bit trên OS 64-bit).
        public static string GetSystemDirectory()
        {
            bool isWow64 = !Environment.Is64BitProcess && Environment.Is64BitOperatingSystem;
            return isWow64
                ? Path.Combine(Environment.GetEnvironmentVariable("windir"), "Sysnative")
                : Environment.GetFolderPath(Environment.SpecialFolder.System);
        }

        public static string BcdeditPath => Path.Combine(GetSystemDirectory(), "bcdedit.exe");
        public static string ShutdownPath => Path.Combine(GetSystemDirectory(), "shutdown.exe");

        /// <summary>Chạy bcdedit với tham số cho trước. Trả exitCode, xuất stderr.</summary>
        public static int RunBcdedit(string arguments, out string stdErr)
        {
            stdErr = "";
            if (!File.Exists(BcdeditPath))
            {
                stdErr = $"Không tìm thấy bcdedit.exe tại {GetSystemDirectory()}";
                return -1;
            }

            var psi = new ProcessStartInfo(BcdeditPath)
            {
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var proc = Process.Start(psi))
            {
                stdErr = proc.StandardError.ReadToEnd();
                proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                return proc.ExitCode;
            }
        }

        /// <summary>
        /// Xóa cờ safeboot. Dùng ở mọi đường thoát lỗi trong Safe Mode.
        /// Không ném exception — chỉ trả true/false + log.
        /// </summary>
        public static bool ClearSafeBootFlag()
        {
            try
            {
                int code = RunBcdedit("/deletevalue {current} safeboot", out string err);
                if (code == 0)
                {
                    Log.Write("SafeBoot: cleared safeboot flag.");
                    // Đã thoát an toàn → gỡ watchdog (Feature round3 #1).
                    Watchdog.Disarm();
                    return true;
                }
                Log.Write($"SafeBoot: clear failed (exit {code}) {err}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Write($"SafeBoot: clear exception {ex.Message}");
                return false;
            }
        }

        public static void Restart()
        {
            try
            {
                if (File.Exists(ShutdownPath))
                    Process.Start(ShutdownPath, "/r /t 0");
            }
            catch (Exception ex)
            {
                Log.Write($"SafeBoot: restart failed {ex.Message}");
            }
        }

        /// <summary>
        /// Gỡ kẹt: xóa cờ safeboot rồi báo người dùng. Dùng khi vào Safe Mode
        /// nhưng không có việc hợp lệ để làm (Fix C3).
        /// </summary>
        public static void RecoverFromStuckSafeMode(string reason)
        {
            Log.Write($"SafeBoot: recovering from stuck safe mode — {reason}");
            bool cleared = ClearSafeBootFlag();
            string msg = cleared
                ? $"Phát hiện máy đang ở Safe Mode nhưng không có tiến trình gỡ AV hợp lệ.\n\nLý do: {reason}\n\nĐã xóa cấu hình Safe Mode. Vui lòng khởi động lại để trở về Normal Mode."
                : $"Phát hiện máy đang ở Safe Mode nhưng không có tiến trình gỡ AV hợp lệ.\n\nKhông tự xóa được cấu hình Safe Mode. Vui lòng mở Command Prompt (Admin) và chạy:\nbcdedit /deletevalue {{current}} safeboot\nrồi khởi động lại.";

            MessageBox.Show(msg,
                cleared ? "Safe Mode - Recovered" : "Safe Mode - Manual Action Required",
                MessageBoxButtons.OK,
                cleared ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
    }
}
