using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  MỘT MỤC AV TRONG CATALOG
    // ──────────────────────────────────────────────────────────────
    internal sealed class AvEntry
    {
        // Các chuỗi để nhận diện (so với displayName, không phân biệt hoa thường).
        public string[] Match { get; set; }
        // Tên hiển thị gọn.
        public string Name { get; set; }
        // Link tải công cụ gỡ chính thức. Rỗng = chỉ phát hiện, chưa có remover.
        public string Url { get; set; }
        // Chuỗi phải có trong tên người ký của file tải về (xác minh chữ ký).
        public string[] ExpectedSigner { get; set; }
        // Có cần Safe Mode để gỡ sạch không.
        public bool RequiresSafeMode { get; set; }
        // Ghi đè tên file lưu (nếu null thì lấy từ URL).
        public string FileName { get; set; }

        public string ResolveFileName()
        {
            if (!string.IsNullOrEmpty(FileName)) return FileName;
            try
            {
                var uri = new Uri(Url);
                string name = Path.GetFileName(uri.LocalPath);
                return string.IsNullOrEmpty(name) ? "uninstaller.exe" : name;
            }
            catch
            {
                return "uninstaller.exe";
            }
        }

        public bool HasRemover =>
            !string.IsNullOrEmpty(Url) &&
            Uri.TryCreate(Url, UriKind.Absolute, out _);
    }

    // ──────────────────────────────────────────────────────────────
    //  CATALOG AV — nguồn embedded + ghi đè online (Feature #6)
    // ──────────────────────────────────────────────────────────────
    internal static class AvCatalog
    {
        // Feature #6: có thể đặt file avlist.json trên GitHub để cập nhật
        // link/chữ ký mà không cần build lại app. Rỗng = bỏ qua, dùng embedded.
        private const string OnlineListUrl =
            "https://raw.githubusercontent.com/ThanhNguyenVN93/Anti-virus-Finder-Tool/master/avlist.json";

        private static List<AvEntry> _cache;

        public static List<AvEntry> Entries
        {
            get
            {
                if (_cache == null)
                    _cache = BuildEmbedded();
                return _cache;
            }
        }

        /// <summary>
        /// Thử tải catalog online, nếu thất bại giữ nguyên embedded.
        /// Gọi ở background, không chặn UI.
        /// </summary>
        public static void TryRefreshFromOnline()
        {
            if (string.IsNullOrEmpty(OnlineListUrl)) return;
            try
            {
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "AVFinder");
                    string json = wc.DownloadString(OnlineListUrl);
                    var parsed = ParseJson(json);
                    if (parsed != null && parsed.Count > 0)
                    {
                        _cache = parsed;
                        Log.Write($"Catalog: loaded {parsed.Count} entries from online list.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write($"Catalog: online refresh failed, using embedded. ({ex.Message})");
            }
        }

        public static AvEntry FindByDisplayName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return null;
            string lower = displayName.ToLowerInvariant();
            foreach (var e in Entries)
            {
                if (e.Match == null) continue;
                foreach (var m in e.Match)
                {
                    if (!string.IsNullOrEmpty(m) && lower.Contains(m.ToLowerInvariant()))
                        return e;
                }
            }
            return null;
        }

        // Danh sách embedded — dùng khi không có/không tải được avlist.json.
        private static List<AvEntry> BuildEmbedded()
        {
            return new List<AvEntry>
            {
                new AvEntry {
                    Match = new[] { "kaspersky" }, Name = "Kaspersky",
                    Url = "https://media.kaspersky.com/utilities/ConsumerUtilities/kavremvr.exe",
                    ExpectedSigner = new[] { "kaspersky" }, RequiresSafeMode = false },
                new AvEntry {
                    Match = new[] { "avast" }, Name = "Avast",
                    Url = "https://honzik.avcdn.net/setup/avast-av/release/avast_av_clear.exe",
                    ExpectedSigner = new[] { "avast", "gen digital", "norton" }, RequiresSafeMode = false },
                new AvEntry {
                    Match = new[] { "avg" }, Name = "AVG",
                    Url = "https://honzik.avcdn.net/setup/avg-av/release/avg_av_clear.exe?alt=en-ww",
                    ExpectedSigner = new[] { "avg", "avast", "gen digital" }, RequiresSafeMode = false,
                    FileName = "avg_av_clear.exe" },
                new AvEntry {
                    Match = new[] { "avira" }, Name = "Avira",
                    Url = "https://github.com/ThanhNguyenVN93/Avira-Uninstaller-Support-Tool/releases/download/avira/Avira.Spotlight.Uninstaller.Tool.exe",
                    ExpectedSigner = new[] { "avira", "gen digital" }, RequiresSafeMode = false },
                new AvEntry {
                    Match = new[] { "bitdefender" }, Name = "Bitdefender",
                    Url = "https://www.bitdefender.com/files/KnowledgeBase/file/Bitdefender_2023_Uninstall_Tool.exe",
                    ExpectedSigner = new[] { "bitdefender" }, RequiresSafeMode = false },
                new AvEntry {
                    Match = new[] { "eset" }, Name = "ESET",
                    Url = "https://download.eset.com/com/eset/tools/installers/eset_apps_remover/latest/uninstaller.exe",
                    ExpectedSigner = new[] { "eset" }, RequiresSafeMode = true },
                new AvEntry {
                    Match = new[] { "mcafee" }, Name = "McAfee",
                    Url = "https://download.mcafee.com/molbin/iss-loc/SupportTools/MCPR/MCPR.exe",
                    ExpectedSigner = new[] { "mcafee" }, RequiresSafeMode = false },
                new AvEntry {
                    Match = new[] { "norton" }, Name = "Norton",
                    Url = "https://honzik.avcdn.net/setup/norton-suite/release/norton_360_remover.exe",
                    ExpectedSigner = new[] { "norton", "gen digital", "symantec" }, RequiresSafeMode = false },
                new AvEntry {
                    Match = new[] { "trend micro", "trendmicro" }, Name = "Trend Micro",
                    Url = "https://github.com/ThanhNguyenVN93/Trendmicro-Uninstaller-Support-Tool/releases/download/v1.0.0/Trendmicro.Uninstaller.Support.Tool.zip",
                    ExpectedSigner = null, RequiresSafeMode = false },
                // ── Bổ sung (Feature #7) ──
                new AvEntry {
                    Match = new[] { "malwarebytes" }, Name = "Malwarebytes",
                    Url = "https://downloads.malwarebytes.com/file/mbst", // Support Tool (gồm MB-Clean)
                    ExpectedSigner = new[] { "malwarebytes" }, RequiresSafeMode = false,
                    FileName = "mb-support-tool.exe" },
                new AvEntry {
                    Match = new[] { "f-secure", "fsecure", "withsecure" }, Name = "F-Secure / WithSecure",
                    Url = "https://download.sp.f-secure.com/uninstallationtool/FsUninstallationTool.exe",
                    ExpectedSigner = new[] { "f-secure", "withsecure" }, RequiresSafeMode = false },
                new AvEntry {
                    Match = new[] { "sophos" }, Name = "Sophos",
                    Url = "https://download.sophos.com/tools/SophosZap.exe",
                    ExpectedSigner = new[] { "sophos" }, RequiresSafeMode = false },
                // Phát hiện-only (chưa có remover chính thức tải trực tiếp).
                new AvEntry {
                    Match = new[] { "bkav" }, Name = "BKAV",
                    Url = "", ExpectedSigner = null, RequiresSafeMode = false },
                new AvEntry {
                    Match = new[] { "cmc" }, Name = "CMC",
                    Url = "", ExpectedSigner = null, RequiresSafeMode = false },
            };
        }

        // ──────────────────────────────────────────────────────────
        //  PARSER JSON TỐI GIẢN (không thêm thư viện ngoài)
        //  Định dạng mong đợi:
        //  [ { "match":["eset"], "name":"ESET", "url":"...",
        //      "signer":["eset"], "safeMode":true, "fileName":"uninstaller.exe" }, ... ]
        // ──────────────────────────────────────────────────────────
        private static List<AvEntry> ParseJson(string json)
        {
            var list = new List<AvEntry>();
            if (string.IsNullOrWhiteSpace(json)) return list;

            foreach (var obj in SplitObjects(json))
            {
                var e = new AvEntry
                {
                    Match = GetStringArray(obj, "match"),
                    Name = GetString(obj, "name"),
                    Url = GetString(obj, "url") ?? "",
                    ExpectedSigner = GetStringArray(obj, "signer"),
                    RequiresSafeMode = GetBool(obj, "safeMode"),
                    FileName = GetString(obj, "fileName")
                };
                if (e.Match != null && e.Match.Length > 0 && !string.IsNullOrEmpty(e.Name))
                    list.Add(e);
            }
            return list;
        }

        private static IEnumerable<string> SplitObjects(string json)
        {
            int depth = 0, start = -1;
            bool inStr = false; char prev = '\0';
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (inStr)
                {
                    if (c == '"' && prev != '\\') inStr = false;
                }
                else
                {
                    if (c == '"') inStr = true;
                    else if (c == '{') { if (depth == 0) start = i; depth++; }
                    else if (c == '}') { depth--; if (depth == 0 && start >= 0) { yield return json.Substring(start, i - start + 1); start = -1; } }
                }
                prev = c;
            }
        }

        private static string GetString(string obj, string key)
        {
            int k = obj.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
            if (k < 0) return null;
            int colon = obj.IndexOf(':', k);
            if (colon < 0) return null;
            int i = colon + 1;
            while (i < obj.Length && char.IsWhiteSpace(obj[i])) i++;
            if (i >= obj.Length || obj[i] != '"') return null;
            i++;
            var sb = new StringBuilder();
            while (i < obj.Length && obj[i] != '"')
            {
                if (obj[i] == '\\' && i + 1 < obj.Length) { i++; sb.Append(obj[i] == 'n' ? '\n' : obj[i]); }
                else sb.Append(obj[i]);
                i++;
            }
            return sb.ToString();
        }

        private static string[] GetStringArray(string obj, string key)
        {
            int k = obj.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
            if (k < 0) return null;
            int open = obj.IndexOf('[', k);
            if (open < 0) return null;
            int close = obj.IndexOf(']', open);
            if (close < 0) return null;
            string inner = obj.Substring(open + 1, close - open - 1);
            var items = new List<string>();
            foreach (var part in inner.Split(','))
            {
                string s = part.Trim().Trim('"').Trim();
                if (!string.IsNullOrEmpty(s)) items.Add(s);
            }
            return items.Count > 0 ? items.ToArray() : null;
        }

        private static bool GetBool(string obj, string key)
        {
            int k = obj.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
            if (k < 0) return false;
            int colon = obj.IndexOf(':', k);
            if (colon < 0) return false;
            string rest = obj.Substring(colon + 1, Math.Min(10, obj.Length - colon - 1)).ToLowerInvariant();
            return rest.Contains("true");
        }
    }
}
