using System;
using System.Diagnostics;
using System.Management;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  ĐIỂM KHÔI PHỤC HỆ THỐNG (Feature nhóm 1 #3)
    //  Tạo System Restore Point trước khi gỡ AV để có đường lùi.
    // ──────────────────────────────────────────────────────────────
    internal static class SystemRestore
    {
        public static bool TryCreate(string description)
        {
            try
            {
                // SystemRestore nằm trong root\default. Chỉ chạy được nếu
                // System Protection đang bật — thất bại thì bỏ qua, không chặn luồng.
                var scope = new ManagementScope(@"\\.\root\default");
                scope.Connect();

                using (var cls = new ManagementClass(scope,
                    new ManagementPath("SystemRestore"), null))
                using (var inParams = cls.GetMethodParameters("CreateRestorePoint"))
                {
                    inParams["Description"] = description;
                    inParams["RestorePointType"] = 12;   // APPLICATION_UNINSTALL
                    inParams["EventType"] = 100;          // BEGIN_SYSTEM_CHANGE

                    using (var outParams = cls.InvokeMethod("CreateRestorePoint", inParams, null))
                    {
                        uint ret = outParams != null ? Convert.ToUInt32(outParams["ReturnValue"]) : 1;
                        Log.Write($"SystemRestore.CreateRestorePoint returned {ret}");
                        return ret == 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write($"SystemRestore failed: {ex.Message}");
                return false;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  WINDOWS DEFENDER (Feature nhóm 1 #4)
    //  Sau khi gỡ AV bên thứ ba, kiểm tra Defender đã bật lại chưa.
    // ──────────────────────────────────────────────────────────────
    internal static class Defender
    {
        public enum State { Unknown, Enabled, Disabled }

        public static State GetRealtimeState()
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Defender");
                scope.Connect();

                using (var searcher = new ManagementObjectSearcher(scope,
                    new ObjectQuery("SELECT * FROM MSFT_MpComputerStatus")))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        using (mo)
                        {
                            object rtp = mo["RealTimeProtectionEnabled"];
                            object av = mo["AntivirusEnabled"];
                            bool enabled = (rtp != null && (bool)rtp) || (av != null && (bool)av);
                            return enabled ? State.Enabled : State.Disabled;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write($"Defender.GetRealtimeState failed: {ex.Message}");
            }
            return State.Unknown;
        }

        /// <summary>
        /// Cố bật lại realtime protection của Defender qua PowerShell Set-MpPreference.
        /// </summary>
        public static bool TryEnableRealtime()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe")
                {
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command " +
                                "\"Set-MpPreference -DisableRealtimeMonitoring $false\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                using (var proc = Process.Start(psi))
                {
                    proc.StandardError.ReadToEnd();
                    proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(15000);
                    Log.Write($"Defender.TryEnableRealtime exit {proc.ExitCode}");
                    return proc.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Log.Write($"Defender.TryEnableRealtime failed: {ex.Message}");
                return false;
            }
        }
    }
}
