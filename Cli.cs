using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  CHẾ ĐỘ DÒNG LỆNH CHO KỸ THUẬT VIÊN (Feature nhóm 3 #11)
    //  --scan-only   : chỉ quét, không mở UI
    //  --json        : in kết quả JSON ra console/stdout
    //  --report      : ghi báo cáo ra %ProgramData%\AVFinder\report-*.txt
    //  --help        : hướng dẫn
    // ──────────────────────────────────────────────────────────────
    internal static class Cli
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        /// <summary>
        /// Xử lý CLI. Trả true nếu đã xử lý (app nên thoát, không mở UI).
        /// </summary>
        public static bool TryHandle(string[] args, out int exitCode)
        {
            exitCode = 0;
            if (args == null || args.Length == 0) return false;

            var set = new System.Collections.Generic.HashSet<string>(
                args.Select(a => a.ToLowerInvariant()));

            bool scanOnly = set.Contains("--scan-only") || set.Contains("/scan-only");
            bool wantJson = set.Contains("--json") || set.Contains("/json");
            bool wantReport = set.Contains("--report") || set.Contains("/report");
            bool silent = set.Contains("--silent") || set.Contains("/silent");
            bool noReboot = set.Contains("--no-reboot") || set.Contains("/no-reboot");
            bool help = set.Contains("--help") || set.Contains("/?") || set.Contains("-h");
            string removeName = GetArgValue(args, "--remove");

            if (!scanOnly && !wantReport && !help && !silent) return false;

            AttachConsole(ATTACH_PARENT_PROCESS);

            if (help)
            {
                Print(HelpText());
                return true;
            }

            if (silent)
            {
                exitCode = SilentRemove(removeName, noReboot);
                return true;
            }

            var all = Detection.DetectAll();

            if (wantJson)
            {
                Print(ToJson(all));
            }
            else if (!wantReport)
            {
                // scan-only không json → in gọn.
                if (all.Count == 0) Print("No third-party antivirus detected.");
                foreach (var r in all)
                {
                    var entry = AvCatalog.FindByDisplayName(r.DisplayName);
                    Print($"{r.DisplayName}\t[{r.Source}]\tremover={(entry != null && entry.HasRemover ? "yes" : "no")}");
                }
            }

            if (wantReport)
            {
                string path = WriteReport(all);
                string html = WriteHtmlReport(all);
                Print($"Report written: {path}");
                Print($"HTML report: {html}");
            }

            // Exit code: 0 = sạch (không AV bên thứ ba), 1 = có AV bên thứ ba.
            exitCode = all.Count > 0 ? 1 : 0;
            return true;
        }

        private static string ToJson(System.Collections.Generic.List<DetectionResult> all)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < all.Count; i++)
            {
                var r = all[i];
                var entry = AvCatalog.FindByDisplayName(r.DisplayName);
                sb.Append("{");
                sb.Append($"\"displayName\":\"{Esc(r.DisplayName)}\",");
                sb.Append($"\"source\":\"{Esc(r.Source)}\",");
                sb.Append($"\"fileVerified\":{(r.FileVerified ? "true" : "false")},");
                sb.Append($"\"catalogName\":\"{Esc(entry?.Name ?? "")}\",");
                sb.Append($"\"hasRemover\":{(entry != null && entry.HasRemover ? "true" : "false")},");
                sb.Append($"\"requiresSafeMode\":{(entry != null && entry.RequiresSafeMode ? "true" : "false")}");
                sb.Append("}");
                if (i < all.Count - 1) sb.Append(",");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string WriteReport(System.Collections.Generic.List<DetectionResult> all)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Anti-Virus Finder Tool — Report ===");
            sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Machine: {Environment.MachineName}");
            sb.AppendLine($"OS: {Environment.OSVersion}");
            sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"Defender realtime: {Defender.GetRealtimeState()}");
            sb.AppendLine();
            sb.AppendLine("Detected third-party antivirus:");
            if (all.Count == 0) sb.AppendLine("  (none)");
            foreach (var r in all)
            {
                var entry = AvCatalog.FindByDisplayName(r.DisplayName);
                sb.AppendLine($"  - {r.DisplayName}  [source={r.Source}, verified={r.FileVerified}, " +
                              $"remover={(entry != null && entry.HasRemover ? "yes" : "no")}]");
            }

            string path = Path.Combine(AppPaths.DataDir, $"report-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        // Feature nhóm 3: báo cáo HTML dễ đọc/gửi hỗ trợ.
        private static string WriteHtmlReport(System.Collections.Generic.List<DetectionResult> all)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html><html><head><meta charset='utf-8'>");
            sb.AppendLine("<title>AVFinder Report</title><style>");
            sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222}");
            sb.AppendLine("h1{color:#0078D7}table{border-collapse:collapse;margin-top:12px}");
            sb.AppendLine("td,th{border:1px solid #ccc;padding:6px 10px;text-align:left}");
            sb.AppendLine("th{background:#f0f5fa}.muted{color:#777}</style></head><body>");
            sb.AppendLine("<h1>🛡️ Anti-Virus Finder — Report</h1>");
            sb.AppendLine($"<p class='muted'>{WebEsc(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))} · {WebEsc(Environment.MachineName)} · {WebEsc(Environment.OSVersion.ToString())}</p>");
            sb.AppendLine($"<p><b>Defender real-time:</b> {WebEsc(Defender.GetRealtimeState().ToString())}</p>");
            sb.AppendLine("<table><tr><th>AV</th><th>Source</th><th>Verified</th><th>Remover</th><th>Safe Mode</th></tr>");
            if (all.Count == 0)
                sb.AppendLine("<tr><td colspan='5' class='muted'>No third-party antivirus detected.</td></tr>");
            foreach (var r in all)
            {
                var e = AvCatalog.FindByDisplayName(r.DisplayName);
                sb.AppendLine("<tr>" +
                    $"<td>{WebEsc(r.DisplayName)}</td>" +
                    $"<td>{WebEsc(r.Source)}</td>" +
                    $"<td>{r.FileVerified}</td>" +
                    $"<td>{(e != null && e.HasRemover ? "yes" : "no")}</td>" +
                    $"<td>{(e != null && e.RequiresSafeMode ? "yes" : "no")}</td></tr>");
            }
            sb.AppendLine("</table></body></html>");

            string path = Path.Combine(AppPaths.DataDir, $"report-{DateTime.Now:yyyyMMdd-HHmmss}.html");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        private static string WebEsc(string s) =>
            (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        // Feature round3 #8: gỡ không giao diện cho triển khai hàng loạt.
        private static int SilentRemove(string nameArg, bool noReboot)
        {
            try
            {
                AvCatalog.TryRefreshFromOnline();

                string target = nameArg;
                if (string.IsNullOrEmpty(target))
                {
                    var first = Detection.DetectFirst(out _);
                    target = first?.DisplayName;
                }
                if (string.IsNullOrEmpty(target))
                {
                    Print("silent: no third-party AV detected.");
                    return 0;
                }

                var entry = AvCatalog.FindByDisplayName(target);
                if (entry == null || !entry.HasRemover)
                {
                    Print($"silent: no official remover for '{target}'.");
                    return 2;
                }

                if (!Policy.Current.IsAvAllowed(entry.Name))
                {
                    Print($"silent: policy blocks removing '{entry.Name}'.");
                    return 3;
                }

                if (entry.RequiresSafeMode && noReboot)
                {
                    Print($"silent: '{entry.Name}' needs Safe Mode but --no-reboot was given. Aborting.");
                    return 4;
                }

                string finalPath = AppPaths.DownloadPath(entry.ResolveFileName());
                string partPath = finalPath + ".part";
                Print($"silent: downloading {entry.Name} ...");

                var outcome = Downloader.DownloadAsync(entry.Url, partPath, null, System.Threading.CancellationToken.None)
                    .GetAwaiter().GetResult();
                if (!outcome.Success)
                {
                    Print($"silent: download failed — {outcome.Message}");
                    return 5;
                }

                bool isExe = finalPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
                if (isExe)
                {
                    var v = SignatureVerifier.Verify(partPath, entry.ExpectedSigner);
                    if (!v.IsTrusted)
                    {
                        Print($"silent: signature check failed — {v.Message}");
                        try { File.Delete(partPath); } catch { }
                        return 6;
                    }
                    Print($"silent: verified signer {v.SignerSubject}");
                }

                try { if (File.Exists(finalPath)) File.Delete(finalPath); File.Move(partPath, finalPath); } catch { }

                if (!isExe)
                {
                    Print($"silent: downloaded non-exe to {finalPath} (manual run needed).");
                    return 0;
                }

                Print($"silent: launching remover {finalPath}");
                System.Diagnostics.Process.Start(finalPath);
                return 0;
            }
            catch (Exception ex)
            {
                Print($"silent: error {ex.Message}");
                return 9;
            }
        }

        private static string GetArgValue(string[] args, string key)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    return args[i + 1];
                if (args[i].StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    return args[i].Substring(key.Length + 1);
            }
            return null;
        }

        private static string HelpText()
        {
            return
                "Anti-Virus Finder Tool — CLI\n" +
                "  --scan-only            Quét AV, không mở giao diện\n" +
                "  --json                 In kết quả dạng JSON (kèm --scan-only)\n" +
                "  --report               Ghi báo cáo ra %ProgramData%\\AVFinder\\report-*.txt\n" +
                "  --silent [--remove X]  Gỡ không giao diện (X = tên AV; bỏ trống = tự phát hiện)\n" +
                "  --no-reboot            Không vào Safe Mode/không tự khởi động lại\n" +
                "  --help                 Hiển thị trợ giúp\n" +
                "Exit code (scan): 0 = sạch, 1 = có AV. (silent): 0 = ok, >0 = lỗi.";
        }

        private static void Print(string s)
        {
            try { Console.Out.WriteLine(s); } catch { }
        }

        private static string Esc(string s) =>
            (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
