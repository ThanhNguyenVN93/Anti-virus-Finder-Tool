using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  DỌN TÀN DƯ SAU KHI GỠ (Feature round2 #9 / round3)
    //  Luôn PREVIEW trước; việc xóa là hành động phá huỷ nên phải được
    //  người dùng xác nhận rõ ràng. Chỉ khớp theo tên thư mục/dịch vụ
    //  đặc trưng của từng hãng để tránh xoá nhầm.
    // ──────────────────────────────────────────────────────────────
    internal sealed class RemnantReport
    {
        public List<string> Folders = new List<string>();
        public List<string> Services = new List<string>();   // chỉ dịch vụ đã STOPPED
        public bool IsEmpty => Folders.Count == 0 && Services.Count == 0;

        public override string ToString()
        {
            var lines = new List<string>();
            if (Folders.Count > 0)
            {
                lines.Add("Thư mục còn sót:");
                lines.AddRange(Folders.Select(f => "  • " + f));
            }
            if (Services.Count > 0)
            {
                lines.Add("Dịch vụ đã dừng còn sót:");
                lines.AddRange(Services.Select(s => "  • " + s));
            }
            return string.Join(Environment.NewLine, lines);
        }
    }

    internal static class Cleanup
    {
        // Tên thư mục đặc trưng theo hãng (khớp chính xác tên thư mục con).
        private static readonly Dictionary<string, string[]> VendorFolders =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "ESET", new[] { "ESET" } },
            { "Kaspersky", new[] { "Kaspersky Lab", "Kaspersky Lab Setup Files" } },
            { "Avast", new[] { "Avast Software" } },
            { "AVG", new[] { "AVG" } },
            { "Avira", new[] { "Avira" } },
            { "Bitdefender", new[] { "Bitdefender", "Bitdefender Agent" } },
            { "McAfee", new[] { "McAfee", "McAfee.com" } },
            { "Norton", new[] { "Norton", "NortonInstaller" } },
            { "Trend Micro", new[] { "Trend Micro" } },
            { "Malwarebytes", new[] { "Malwarebytes" } },
            { "F-Secure / WithSecure", new[] { "F-Secure", "WithSecure" } },
            { "Sophos", new[] { "Sophos" } },
        };

        // Tiền tố tên dịch vụ theo hãng.
        // LƯU Ý an toàn: tránh tiền tố quá ngắn/chung (vd "ns", "fs", "tm")
        // vì có thể khớp nhầm dịch vụ hệ thống (nsi = Network Store Interface,
        // fsdepends, ...). Chỉ dùng token đặc trưng, đủ dài.
        private static readonly Dictionary<string, string[]> VendorServicePrefixes =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "ESET", new[] { "ekrn", "egui", "ehdrv", "EViewSvc" } },
            { "Kaspersky", new[] { "AVP", "klif", "klbg", "klnagent", "klpd" } },
            { "Avast", new[] { "avast", "aswbIDSAgent", "aswStm" } },
            { "AVG", new[] { "avgSvc", "avgMonFlt", "avgbIDSAgent" } },
            { "Avira", new[] { "Avira", "AntiVirService", "avguard" } },
            { "Bitdefender", new[] { "VSSERV", "bdredline", "BDAuxSrv", "bdintf" } },
            { "McAfee", new[] { "mfemms", "mfevtp", "mfefire", "McAPExe", "McAfee" } },
            { "Norton", new[] { "NortonSecurity", "Norton Security", "nsConsumerFramework" } },
            { "Trend Micro", new[] { "tmlisten", "tmccsf", "tmpfw", "TrendMicro", "Trend Micro" } },
            { "Malwarebytes", new[] { "MBAMService", "MBAM", "Malwarebytes" } },
            { "F-Secure / WithSecure", new[] { "fshoster", "fsulprot", "FSORSPClient", "WithSecure" } },
            { "Sophos", new[] { "SophosED", "SAVService", "SAVAdminService", "Sophos" } },
        };

        public static RemnantReport Preview(string avName)
        {
            var report = new RemnantReport();
            var entry = AvCatalog.FindByDisplayName(avName);
            string key = entry?.Name ?? avName;

            // Thư mục.
            if (VendorFolders.TryGetValue(key, out string[] folders))
            {
                string[] bases =
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                };
                foreach (var b in bases.Distinct())
                {
                    if (string.IsNullOrEmpty(b)) continue;
                    foreach (var f in folders)
                    {
                        try
                        {
                            string full = Path.Combine(b, f);
                            if (Directory.Exists(full)) report.Folders.Add(full);
                        }
                        catch { }
                    }
                }
            }

            // Dịch vụ đã dừng.
            if (VendorServicePrefixes.TryGetValue(key, out string[] prefixes))
            {
                try
                {
                    foreach (var svc in ServiceController.GetServices())
                    {
                        try
                        {
                            if (svc.Status != ServiceControllerStatus.Stopped) continue;
                            foreach (var p in prefixes)
                            {
                                if (svc.ServiceName.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                                {
                                    report.Services.Add(svc.ServiceName);
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write($"Cleanup.Preview services: {ex.Message}");
                }
            }

            return report;
        }

        /// <summary>Thực hiện xoá. CHỈ gọi sau khi người dùng đã xác nhận preview.</summary>
        public static List<string> Apply(RemnantReport report)
        {
            var errors = new List<string>();

            foreach (var folder in report.Folders)
            {
                try
                {
                    Directory.Delete(folder, recursive: true);
                    Log.Write($"Cleanup: deleted folder {folder}");
                }
                catch (Exception ex)
                {
                    errors.Add($"{folder}: {ex.Message}");
                    Log.Write($"Cleanup folder failed {folder}: {ex.Message}");
                }
            }

            foreach (var svc in report.Services)
            {
                try
                {
                    // Xoá service đã dừng qua sc.exe delete.
                    var psi = new ProcessStartInfo("sc.exe", $"delete \"{svc}\"")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using (var p = Process.Start(psi))
                    {
                        string err = p.StandardError.ReadToEnd();
                        p.StandardOutput.ReadToEnd();
                        p.WaitForExit(10000);
                        if (p.ExitCode != 0)
                            errors.Add($"service {svc}: sc delete exit {p.ExitCode} {err}");
                        else
                            Log.Write($"Cleanup: deleted service {svc}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"service {svc}: {ex.Message}");
                }
            }

            return errors;
        }
    }
}
