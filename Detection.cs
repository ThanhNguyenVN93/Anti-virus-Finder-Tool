using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using Microsoft.Win32;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  KẾT QUẢ PHÁT HIỆN
    // ──────────────────────────────────────────────────────────────
    internal sealed class DetectionResult
    {
        public string DisplayName;   // Tên AV (từ Security Center / registry)
        public string Source;        // securitycenter2 | registry | service | minifilter
        public bool FileVerified;    // Có xác nhận được file cài trên đĩa không
        public bool? Enabled;        // (SecurityCenter2) real-time đang bật?
        public bool? UpToDate;       // (SecurityCenter2) định nghĩa còn mới?
    }

    // ──────────────────────────────────────────────────────────────
    //  PHÁT HIỆN AV ĐA NGUỒN (Feature nhóm 2 #5)
    //  Không chỉ dựa vào SecurityCenter2 (vốn không có trên Windows Server
    //  và hay giữ entry rác). Bổ sung registry Uninstall + service/driver.
    // ──────────────────────────────────────────────────────────────
    internal static class Detection
    {
        // Service/driver đặc trưng của một số AV → bắt được cả bản cài dở.
        private static readonly Dictionary<string, string> ServiceSignatures =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ekrn", "ESET" },
            { "egui", "ESET" },
            { "avp", "Kaspersky" },
            { "avast! Antivirus", "Avast" },
            { "aswbIDSAgent", "Avast" },
            { "AVG Antivirus", "AVG" },
            { "avgSvc", "AVG" },
            { "Avira.ServiceHost", "Avira" },
            { "AntiVirService", "Avira" },
            { "VSSERV", "Bitdefender" },
            { "bdredline", "Bitdefender" },
            { "McAPExe", "McAfee" },
            { "mfemms", "McAfee" },
            { "NortonSecurity", "Norton" },
            { "MBAMService", "Malwarebytes" },
            { "fshoster", "F-Secure" },
            { "Sophos Endpoint Defense Service", "Sophos" },
            { "SAVService", "Sophos" },
            { "BkavService", "BKAV" },
            { "CMCMonSvc", "CMC" },
        };

        /// <summary>
        /// Trả về AV bên thứ ba đầu tiên tìm thấy, ưu tiên nguồn đáng tin nhất.
        /// </summary>
        public static DetectionResult DetectFirst(out List<DetectionResult> all)
        {
            all = DetectAll();
            // Ưu tiên: file verified > securitycenter2 > registry > service
            return all
                .OrderByDescending(r => r.FileVerified)
                .ThenBy(r => SourceRank(r.Source))
                .FirstOrDefault();
        }

        private static int SourceRank(string source)
        {
            switch (source)
            {
                case "securitycenter2": return 0;
                case "registry": return 1;
                case "service": return 2;
                default: return 3;
            }
        }

        public static List<DetectionResult> DetectAll()
        {
            var results = new List<DetectionResult>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in DetectViaSecurityCenter())
                AddUnique(results, seen, r);
            foreach (var r in DetectViaRegistry())
                AddUnique(results, seen, r);
            foreach (var r in DetectViaServices())
                AddUnique(results, seen, r);
            foreach (var r in DetectViaMinifilters())
                AddUnique(results, seen, r);

            return results;
        }

        private static void AddUnique(List<DetectionResult> list, HashSet<string> seen, DetectionResult r)
        {
            if (r == null || string.IsNullOrEmpty(r.DisplayName)) return;
            // Gộp theo tên catalog để không trùng (vd Security Center + service cùng ESET).
            var entry = AvCatalog.FindByDisplayName(r.DisplayName);
            string key = entry != null ? entry.Name : r.DisplayName;
            if (seen.Add(key))
                list.Add(r);
        }

        // ── Nguồn 1: ROOT\SecurityCenter2 ──
        private static IEnumerable<DetectionResult> DetectViaSecurityCenter()
        {
            var outp = new List<DetectionResult>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    @"ROOT\SecurityCenter2", "SELECT * FROM AntivirusProduct"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        using (mo)
                        {
                            string name = mo["displayName"]?.ToString();
                            if (string.IsNullOrEmpty(name) || IsDefender(name)) continue;

                            string path = Normalize(mo["pathToSignedProductExe"]?.ToString());
                            bool verified = !string.IsNullOrEmpty(path) && System.IO.File.Exists(path);

                            bool? enabled = null, upToDate = null;
                            try
                            {
                                object ps = mo["productState"];
                                if (ps != null)
                                    DecodeProductState(Convert.ToUInt32(ps), out enabled, out upToDate);
                            }
                            catch { }

                            outp.Add(new DetectionResult
                            {
                                DisplayName = name,
                                Source = "securitycenter2",
                                FileVerified = verified,
                                Enabled = enabled,
                                UpToDate = upToDate
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write($"DetectViaSecurityCenter: {ex.Message} (có thể do Server/Safe Mode)");
            }
            return outp;
        }

        // ── Nguồn 2: registry Uninstall (cả 64-bit và 32-bit view) ──
        private static IEnumerable<DetectionResult> DetectViaRegistry()
        {
            var outp = new List<DetectionResult>();
            string[] roots =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var root in roots)
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(root))
                    {
                        if (key == null) continue;
                        foreach (var sub in key.GetSubKeyNames())
                        {
                            using (var item = key.OpenSubKey(sub))
                            {
                                string name = item?.GetValue("DisplayName") as string;
                                if (string.IsNullOrEmpty(name) || IsDefender(name)) continue;

                                // Chỉ nhận nếu khớp một AV trong catalog → tránh nhiễu.
                                if (AvCatalog.FindByDisplayName(name) != null)
                                {
                                    outp.Add(new DetectionResult
                                    {
                                        DisplayName = name,
                                        Source = "registry",
                                        FileVerified = false
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write($"DetectViaRegistry({root}): {ex.Message}");
                }
            }
            return outp;
        }

        // ── Nguồn 3: service/driver (bắt được bản cài dở) ──
        private static IEnumerable<DetectionResult> DetectViaServices()
        {
            var outp = new List<DetectionResult>();
            try
            {
                var services = ServiceController.GetServices();
                foreach (var svc in services)
                {
                    foreach (var kv in ServiceSignatures)
                    {
                        if (svc.ServiceName.Equals(kv.Key, StringComparison.OrdinalIgnoreCase) ||
                            (svc.DisplayName?.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            outp.Add(new DetectionResult
                            {
                                DisplayName = kv.Value,
                                Source = "service",
                                FileVerified = false
                            });
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write($"DetectViaServices: {ex.Message}");
            }
            return outp;
        }

        // Giải mã productState (Feature round3 #9).
        // productState là số 24-bit; byte giữa = trạng thái scanner, byte cuối = định nghĩa.
        private static void DecodeProductState(uint state, out bool? enabled, out bool? upToDate)
        {
            try
            {
                string hex = state.ToString("X6");
                int scanner = Convert.ToInt32(hex.Substring(2, 2), 16);
                int definition = Convert.ToInt32(hex.Substring(4, 2), 16);
                enabled = (scanner & 0x10) != 0;       // bit bật real-time
                upToDate = definition == 0x00;         // 0x00 = up-to-date, 0x10 = outdated
            }
            catch
            {
                enabled = null;
                upToDate = null;
            }
        }

        // ── Nguồn 4: minifilter driver qua fltmc (Feature round3 #10) ──
        private static readonly Dictionary<string, string> MinifilterSignatures =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "eamonm", "ESET" }, { "ehdrv", "ESET" },
            { "klif", "Kaspersky" }, { "klupd", "Kaspersky" },
            { "aswSP", "Avast" }, { "aswMonFlt", "Avast" },
            { "avgtp", "AVG" }, { "avgMonFlt", "AVG" },
            { "avkmgr", "Avira" },
            { "gzflt", "Bitdefender" }, { "bdfndisf", "Bitdefender" }, { "atc", "Bitdefender" },
            { "mfehidk", "McAfee" }, { "mfencbdc", "McAfee" },
            { "SymEFASI", "Norton" }, { "symevent", "Norton" },
            { "tmel", "Trend Micro" }, { "tmevtmgr", "Trend Micro" },
            { "mbamwatchdog", "Malwarebytes" }, { "mbae64", "Malwarebytes" },
            { "fsatp", "F-Secure" }, { "fsgk", "F-Secure" },
            { "SophosED", "Sophos" }, { "hmpalert", "Sophos" },
        };

        private static IEnumerable<DetectionResult> DetectViaMinifilters()
        {
            var outp = new List<DetectionResult>();
            try
            {
                string fltmc = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.System), "fltmc.exe");
                if (!System.IO.File.Exists(fltmc)) return outp;

                var psi = new System.Diagnostics.ProcessStartInfo(fltmc, "filters")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                string output;
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    output = p.StandardOutput.ReadToEnd();
                    p.StandardError.ReadToEnd();
                    p.WaitForExit(5000);
                }

                foreach (var kv in MinifilterSignatures)
                {
                    if (output.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        outp.Add(new DetectionResult
                        {
                            DisplayName = kv.Value,
                            Source = "minifilter",
                            FileVerified = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write($"DetectViaMinifilters: {ex.Message}");
            }
            return outp;
        }

        private static bool IsDefender(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("windows defender")
                || n.Contains("microsoft defender")
                || n.Contains("defender antivirus");
        }

        private static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            path = path.Trim().Trim('"').Trim();
            try { path = Environment.ExpandEnvironmentVariables(path); } catch { }
            return path;
        }
    }
}
