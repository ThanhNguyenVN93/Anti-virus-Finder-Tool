using System;
using System.Diagnostics;
using System.IO;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  WATCHDOG CHỐNG KẸT SAFE MODE (Feature round3 #1)
    //  Lớp an toàn phụ: trước khi reboot vào Safe Mode, tạo một
    //  Scheduled Task hẹn giờ. Nếu vì lý do gì app không tự xóa được
    //  cờ safeboot (crash, v.v.), task này sẽ xóa cờ + reboot về Normal.
    //
    //  LƯU Ý trung thực: Task Scheduler thường KHÔNG chạy trong
    //  "Safe Mode Minimal". Watchdog vì thế là lưới an toàn cho các
    //  tình huống máy trở lại trạng thái có Task Scheduler (vd Safe Mode
    //  with Networking, hoặc đã về Normal nhưng cờ còn sót), KHÔNG thay
    //  thế cơ chế tự-xóa-cờ ngay trong app (vốn là bảo đảm chính).
    // ──────────────────────────────────────────────────────────────
    internal static class Watchdog
    {
        private const string TaskName = "AVFinderSafeModeWatchdog";

        private static string Schtasks => Path.Combine(SafeBoot.GetSystemDirectory(), "schtasks.exe");
        private static string ScriptPath => Path.Combine(AppPaths.DataDir, "safemode-failsafe.cmd");

        /// <summary>Tạo task hẹn giờ xóa safeboot + reboot sau delayMinutes phút.</summary>
        public static bool Arm(int delayMinutes)
        {
            try
            {
                // Script failsafe: xóa safeboot, tự gỡ task, rồi reboot.
                string script =
                    "@echo off\r\n" +
                    "bcdedit /deletevalue {current} safeboot\r\n" +
                    $"schtasks /Delete /TN \"{TaskName}\" /F\r\n" +
                    "shutdown /r /t 5\r\n";
                File.WriteAllText(ScriptPath, script);

                DateTime when = DateTime.Now.AddMinutes(Math.Max(1, delayMinutes));
                string st = when.ToString("HH:mm");
                string sd = when.ToString("MM/dd/yyyy");

                // /RU SYSTEM /RL HIGHEST để chạy với quyền cao nhất, không cần đăng nhập.
                string args =
                    $"/Create /TN \"{TaskName}\" /TR \"cmd /c \\\"{ScriptPath}\\\"\" " +
                    $"/SC ONCE /ST {st} /SD {sd} /RU SYSTEM /RL HIGHEST /F";

                int code = RunSchtasks(args, out string err);
                if (code == 0)
                {
                    Log.Write($"Watchdog armed for {when:yyyy-MM-dd HH:mm}");
                    return true;
                }
                Log.Write($"Watchdog arm failed ({code}): {err}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Write($"Watchdog.Arm exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>Gỡ task + script khi đã xử lý xong an toàn.</summary>
        public static void Disarm()
        {
            try
            {
                RunSchtasks($"/Delete /TN \"{TaskName}\" /F", out _);
                if (File.Exists(ScriptPath)) { try { File.Delete(ScriptPath); } catch { } }
                Log.Write("Watchdog disarmed.");
            }
            catch (Exception ex)
            {
                Log.Write($"Watchdog.Disarm exception: {ex.Message}");
            }
        }

        private static int RunSchtasks(string arguments, out string stdErr)
        {
            stdErr = "";
            if (!File.Exists(Schtasks))
            {
                stdErr = "schtasks.exe not found";
                return -1;
            }
            var psi = new ProcessStartInfo(Schtasks)
            {
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var p = Process.Start(psi))
            {
                stdErr = p.StandardError.ReadToEnd();
                p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return p.ExitCode;
            }
        }
    }
}
