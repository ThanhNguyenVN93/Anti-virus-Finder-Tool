using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Threading;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  KIỂM TRA QUYỀN ELEVATE THỰC SỰ (Feature round3 #2)
    // ──────────────────────────────────────────────────────────────
    internal static class Elevation
    {
        public static bool IsElevated()
        {
            try
            {
                using (var id = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(id);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  KIỂM TRA TRƯỚC KHI GỠ (Feature round3 #4)
    //  - Tamper Protection của Defender còn bật?
    //  - Có installer khác (MSI) đang chạy?
    // ──────────────────────────────────────────────────────────────
    internal static class PreflightChecks
    {
        /// <summary>Defender Tamper Protection đang bật? (có thể chặn một số remover)</summary>
        public static bool? IsDefenderTamperProtectionOn()
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Defender");
                scope.Connect();
                using (var searcher = new ManagementObjectSearcher(scope,
                    new ObjectQuery("SELECT IsTamperProtected FROM MSFT_MpComputerStatus")))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        using (mo)
                        {
                            object v = mo["IsTamperProtected"];
                            if (v != null) return (bool)v;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write($"IsDefenderTamperProtectionOn failed: {ex.Message}");
            }
            return null; // không xác định
        }

        /// <summary>Có Windows Installer (MSI) đang chạy không — tránh xung đột khi gỡ.</summary>
        public static bool IsAnotherInstallerRunning()
        {
            // _MSIExecute mutex giữ khi một giao dịch MSI đang chạy.
            try
            {
                using (var m = Mutex.OpenExisting("Global\\_MSIExecute"))
                {
                    // Mở được nghĩa là mutex tồn tại → có MSI đang chạy.
                    return true;
                }
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false; // không tồn tại → không có MSI
            }
            catch (Exception ex)
            {
                Log.Write($"IsAnotherInstallerRunning check: {ex.Message}");
                return false;
            }
        }

        /// <summary>Pin đủ cao để an toàn vào Safe Mode? (null nếu không phải laptop/không đọc được)</summary>
        public static int? GetBatteryPercent()
        {
            try
            {
                var status = System.Windows.Forms.SystemInformation.PowerStatus;
                if (status.BatteryChargeStatus == System.Windows.Forms.BatteryChargeStatus.NoSystemBattery ||
                    status.BatteryChargeStatus == System.Windows.Forms.BatteryChargeStatus.Unknown)
                    return null;
                float life = status.BatteryLifePercent; // 0..1
                if (life < 0 || life > 1) return null;
                return (int)Math.Round(life * 100);
            }
            catch
            {
                return null;
            }
        }

        public static bool IsOnAcPower()
        {
            try
            {
                return System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus
                    == System.Windows.Forms.PowerLineStatus.Online;
            }
            catch
            {
                return true;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  CHÍNH SÁCH HOST CHO LINK TẢI (Feature round3 #3)
    //  Chỉ cho phép HTTPS và host nằm trong danh sách tin cậy; chặn
    //  redirect sang host lạ.
    // ──────────────────────────────────────────────────────────────
    internal static class DownloadPolicy
    {
        // Hậu tố host được phép (khớp theo đuôi, không phân biệt hoa thường).
        private static readonly string[] AllowedHostSuffixes =
        {
            "kaspersky.com",
            "avcdn.net",            // Avast/AVG/Norton CDN
            "bitdefender.com",
            "eset.com",
            "mcafee.com",
            "malwarebytes.com",
            "f-secure.com",
            "sophos.com",
            "github.com",
            "githubusercontent.com"
        };

        public static bool IsAllowed(Uri uri)
        {
            if (uri == null) return false;
            if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)) return false;

            string host = uri.Host.ToLowerInvariant();
            return AllowedHostSuffixes.Any(suf =>
                host == suf || host.EndsWith("." + suf));
        }

        public static string Describe(Uri uri)
        {
            return uri == null ? "(null)" : $"{uri.Scheme}://{uri.Host}";
        }
    }
}
