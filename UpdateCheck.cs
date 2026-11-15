using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  TỰ KIỂM TRA BẢN MỚI (Feature round2 #13)
    //  Hỏi GitHub Releases API xem có tag mới hơn phiên bản hiện tại.
    // ──────────────────────────────────────────────────────────────
    internal static class UpdateCheck
    {
        private const string LatestApi =
            "https://api.github.com/repos/ThanhNguyenVN93/Anti-virus-Finder-Tool/releases/latest";
        private const string ReleasesPage =
            "https://github.com/ThanhNguyenVN93/Anti-virus-Finder-Tool/releases";

        public sealed class Result
        {
            public bool HasUpdate;
            public string LatestVersion;
            public string CurrentVersion;
            public string Url = ReleasesPage;
            public string Notes = "";          // What's New (release body)
            public string AssetUrl = "";       // Link tải asset (.exe/.zip) nếu có
            public string AssetName = "";
        }

        public static Result Check()
        {
            var res = new Result { CurrentVersion = CurrentVersion() };
            try
            {
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "AVFinder");
                    wc.Headers.Add("Accept", "application/vnd.github+json");
                    string json = wc.DownloadString(LatestApi);

                    var m = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                    if (!m.Success) return res;

                    string tag = m.Groups[1].Value.TrimStart('v', 'V');
                    res.LatestVersion = tag;
                    res.HasUpdate = CompareVersions(tag, res.CurrentVersion) > 0;

                    // What's New (release body) — giải mã chuỗi JSON.
                    res.Notes = ExtractJsonString(json, "body");

                    // Link trang release cụ thể.
                    string htmlUrl = ExtractJsonString(json, "html_url");
                    if (!string.IsNullOrEmpty(htmlUrl)) res.Url = htmlUrl;

                    // Asset đầu tiên là .exe hoặc .zip.
                    var am = Regex.Match(json,
                        "\"browser_download_url\"\\s*:\\s*\"([^\"]+\\.(?:exe|zip))\"",
                        RegexOptions.IgnoreCase);
                    if (am.Success)
                    {
                        res.AssetUrl = am.Groups[1].Value;
                        try { res.AssetName = System.IO.Path.GetFileName(new Uri(res.AssetUrl).LocalPath); } catch { }
                    }

                    Log.Write($"UpdateCheck: current={res.CurrentVersion} latest={tag} hasUpdate={res.HasUpdate} asset={res.AssetName}");
                }
            }
            catch (Exception ex)
            {
                Log.Write($"UpdateCheck failed: {ex.Message}");
            }
            return res;
        }

        // Đọc một chuỗi JSON theo key (giải mã \n, \" , \\ , \/, \t).
        private static string ExtractJsonString(string json, string key)
        {
            int k = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (k < 0) return "";
            int colon = json.IndexOf(':', k);
            if (colon < 0) return "";
            int i = colon + 1;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            if (i >= json.Length || json[i] != '"') return "";
            i++;
            var sb = new System.Text.StringBuilder();
            while (i < json.Length && json[i] != '"')
            {
                char c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    char n = json[i + 1];
                    switch (n)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'u':
                            if (i + 5 < json.Length &&
                                int.TryParse(json.Substring(i + 2, 4),
                                    System.Globalization.NumberStyles.HexNumber,
                                    System.Globalization.CultureInfo.InvariantCulture, out int cp))
                            {
                                sb.Append((char)cp); i += 4;
                            }
                            break;
                        default: sb.Append(n); break;
                    }
                    i += 2;
                }
                else { sb.Append(c); i++; }
            }
            return sb.ToString();
        }

        private static string CurrentVersion()
        {
            try
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                return $"{v.Major}.{v.Minor}.{v.Build}";
            }
            catch
            {
                return "0.0.0";
            }
        }

        // >0 nếu a mới hơn b.
        private static int CompareVersions(string a, string b)
        {
            try
            {
                var pa = ParseNums(a);
                var pb = ParseNums(b);
                int n = Math.Max(pa.Length, pb.Length);
                for (int i = 0; i < n; i++)
                {
                    int x = i < pa.Length ? pa[i] : 0;
                    int y = i < pb.Length ? pb[i] : 0;
                    if (x != y) return x.CompareTo(y);
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private static int[] ParseNums(string s)
        {
            var parts = s.Split('.', '-', '+');
            var nums = new System.Collections.Generic.List<int>();
            foreach (var p in parts)
            {
                if (int.TryParse(new string(p.TrimStart().TakeWhile(char.IsDigit).ToArray()), out int v))
                    nums.Add(v);
                else break;
            }
            return nums.ToArray();
        }
    }
}
