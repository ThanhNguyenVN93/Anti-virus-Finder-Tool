using System;
using System.IO;
using System.Linq;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  CHÍNH SÁCH TRIỂN KHAI (Feature round3 #7)
    //  policy.json (đặt cạnh exe hoặc trong %ProgramData%\AVFinder) cho
    //  phép môi trường doanh nghiệp giới hạn hành vi của tool.
    //
    //  {
    //    "allowAutoReboot": true,
    //    "requireRestorePoint": true,
    //    "disableAutoRun": false,
    //    "allowedAv": ["ESET","Kaspersky"]   // null/[] = cho phép tất cả
    //  }
    // ──────────────────────────────────────────────────────────────
    public sealed class Policy
    {
        public bool AllowAutoReboot = true;
        public bool RequireRestorePoint = false;
        public bool DisableAutoRun = false;
        public string[] AllowedAv = null;    // null = tất cả

        private static Policy _cached;

        public static Policy Current
        {
            get
            {
                if (_cached == null) _cached = Load();
                return _cached;
            }
        }

        public bool IsAvAllowed(string name)
        {
            if (AllowedAv == null || AllowedAv.Length == 0) return true;
            if (string.IsNullOrEmpty(name)) return false;
            return AllowedAv.Any(a =>
                !string.IsNullOrEmpty(a) &&
                name.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static Policy Load()
        {
            string[] candidates =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "policy.json"),
                Path.Combine(AppPaths.DataDir, "policy.json")
            };

            foreach (var path in candidates)
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    string json = File.ReadAllText(path);
                    var p = new Policy
                    {
                        AllowAutoReboot = GetBool(json, "allowAutoReboot", true),
                        RequireRestorePoint = GetBool(json, "requireRestorePoint", false),
                        DisableAutoRun = GetBool(json, "disableAutoRun", false),
                        AllowedAv = GetArray(json, "allowedAv")
                    };
                    Log.Write($"Policy loaded from {path}");
                    return p;
                }
                catch (Exception ex)
                {
                    Log.Write($"Policy load error ({path}): {ex.Message}");
                }
            }
            return new Policy();
        }

        private static bool GetBool(string json, string key, bool dflt)
        {
            int k = json.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
            if (k < 0) return dflt;
            int colon = json.IndexOf(':', k);
            if (colon < 0) return dflt;
            string rest = json.Substring(colon + 1, Math.Min(12, json.Length - colon - 1)).ToLowerInvariant();
            if (rest.Contains("true")) return true;
            if (rest.Contains("false")) return false;
            return dflt;
        }

        private static string[] GetArray(string json, string key)
        {
            int k = json.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
            if (k < 0) return null;
            int open = json.IndexOf('[', k);
            if (open < 0) return null;
            int close = json.IndexOf(']', open);
            if (close < 0) return null;
            string inner = json.Substring(open + 1, close - open - 1);
            var items = inner.Split(',')
                .Select(s => s.Trim().Trim('"').Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
            return items.Length > 0 ? items : null;
        }
    }
}
