using System;
using System.IO;
using System.Text;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  TRẠNG THÁI BỀN VỮNG GIỮA CÁC LẦN CHẠY
    //  Fix lỗi #1 (tự tải lại khi mở lại) và C1 (vòng lặp reboot):
    //  lưu lại đã xử lý AV nào, tới bước nào, để lần sau không lặp.
    // ──────────────────────────────────────────────────────────────
    public sealed class AppState
    {
        public string Phase = "idle";          // idle | downloaded | uninstalling | safemode_pending | completed
        public string TargetAv = "";           // Tên AV đang/đã xử lý
        public string DownloadedFile = "";     // Đường dẫn file đã tải (đã xác minh)
        public bool DownloadVerified = false;  // File đã qua kiểm tra chữ ký chưa
        public string LastResultAv = "";       // AV đã gỡ xong ở lần trước
        public DateTime UpdatedUtc = DateTime.UtcNow;

        private static string File_ => AppPaths.StateFile;

        public static AppState Load()
        {
            try
            {
                if (!File.Exists(File_)) return new AppState();
                string json = File.ReadAllText(File_, Encoding.UTF8);
                return new AppState
                {
                    Phase = Extract(json, "phase") ?? "idle",
                    TargetAv = Extract(json, "targetAv") ?? "",
                    DownloadedFile = Extract(json, "downloadedFile") ?? "",
                    DownloadVerified = (Extract(json, "downloadVerified") ?? "false")
                                           .Equals("true", StringComparison.OrdinalIgnoreCase),
                    LastResultAv = Extract(json, "lastResultAv") ?? "",
                };
            }
            catch
            {
                return new AppState();
            }
        }

        public void Save()
        {
            try
            {
                UpdatedUtc = DateTime.UtcNow;
                var sb = new StringBuilder();
                sb.Append("{\n");
                sb.Append($"  \"phase\": \"{Esc(Phase)}\",\n");
                sb.Append($"  \"targetAv\": \"{Esc(TargetAv)}\",\n");
                sb.Append($"  \"downloadedFile\": \"{Esc(DownloadedFile)}\",\n");
                sb.Append($"  \"downloadVerified\": {(DownloadVerified ? "true" : "false")},\n");
                sb.Append($"  \"lastResultAv\": \"{Esc(LastResultAv)}\",\n");
                sb.Append($"  \"updatedUtc\": \"{UpdatedUtc:o}\"\n");
                sb.Append("}\n");
                File.WriteAllText(File_, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log.Write($"State: save failed ({ex.Message})");
            }
        }

        public void Reset()
        {
            Phase = "idle";
            TargetAv = "";
            DownloadedFile = "";
            DownloadVerified = false;
            Save();
        }

        private static string Esc(string s) =>
            (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string Extract(string json, string key)
        {
            int k = json.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
            if (k < 0) return null;
            int colon = json.IndexOf(':', k);
            if (colon < 0) return null;
            int i = colon + 1;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            if (i >= json.Length) return null;

            if (json[i] == '"')
            {
                i++;
                var sb = new StringBuilder();
                while (i < json.Length && json[i] != '"')
                {
                    if (json[i] == '\\' && i + 1 < json.Length) { i++; sb.Append(json[i]); }
                    else sb.Append(json[i]);
                    i++;
                }
                return sb.ToString();
            }
            else
            {
                // bool/number
                var sb = new StringBuilder();
                while (i < json.Length && json[i] != ',' && json[i] != '\n' && json[i] != '}')
                {
                    sb.Append(json[i]); i++;
                }
                return sb.ToString().Trim();
            }
        }
    }
}
