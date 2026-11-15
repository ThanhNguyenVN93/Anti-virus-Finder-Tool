using System;
using System.Collections.Generic;
using System.IO;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  SONG NGỮ VIỆT / ANH (Feature round2 #12)
    //  Bản gọn: từ điển runtime cho các nhãn UI chính + lưu lựa chọn.
    //  (Có thể mở rộng dần sang .resx đầy đủ sau này.)
    // ──────────────────────────────────────────────────────────────
    internal static class Loc
    {
        public enum Lang { VI, EN }

        public static Lang Current { get; private set; } = Load();

        private static string LangFile => Path.Combine(AppPaths.DataDir, "lang.txt");

        private static readonly Dictionary<string, string[]> Strings =
            // key → { VI, EN }
            new Dictionary<string, string[]>
        {
            { "grpAvCheck",   new[] { "KIỂM TRA ANTIVIRUS", "ANTIVIRUS EXISTENCE CHECK" } },
            { "grpDownload",  new[] { "TẢI CÔNG CỤ GỠ", "DOWNLOAD MANAGER" } },
            { "grpOs",        new[] { "HỆ ĐIỀU HÀNH", "OPERATING SYSTEM" } },
            { "grpSupport",   new[] { "DANH SÁCH AV HỖ TRỢ", "AV SUPPORT LIST" } },
            { "btnCheck",     new[] { "KIỂM TRA", "CHECK" } },
            { "btnDownload",  new[] { "TẢI VỀ", "DOWNLOAD" } },
            { "lblAvName",    new[] { "Tên AV:", "AV Name:" } },
            { "lblDirectory", new[] { "Đường dẫn:", "Directory:" } },
            { "statusWaiting",new[] { "TRẠNG THÁI: Đang chờ...", "STATUS: Waiting..." } },
            { "speed0",       new[] { "Tốc độ: 0 KB/s", "Speed: 0 KB/s" } },
        };

        public static string T(string key)
        {
            if (Strings.TryGetValue(key, out string[] pair))
                return Current == Lang.VI ? pair[0] : pair[1];
            return key;
        }

        public static void Toggle()
        {
            Current = Current == Lang.VI ? Lang.EN : Lang.VI;
            Save();
        }

        private static Lang Load()
        {
            try
            {
                if (File.Exists(LangFile))
                {
                    string s = File.ReadAllText(LangFile).Trim().ToUpperInvariant();
                    if (s == "EN") return Lang.EN;
                }
            }
            catch { }
            return Lang.VI;
        }

        private static void Save()
        {
            try { File.WriteAllText(LangFile, Current == Lang.EN ? "EN" : "VI"); }
            catch { }
        }
    }
}
