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
                    Log.Write($"UpdateCheck: current={res.CurrentVersion} latest={tag} hasUpdate={res.HasUpdate}");
                }
            }
            catch (Exception ex)
            {
                Log.Write($"UpdateCheck failed: {ex.Message}");
            }
            return res;
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
