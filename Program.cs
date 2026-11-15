using System;
using System.Windows.Forms;

namespace frmavfinnder
{
    internal static class Program
    {
        // Giữ mutex sống suốt vòng đời app.
        private static SingleInstance _instance;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Feature nhóm 3 #11: chế độ dòng lệnh cho kỹ thuật viên.
            // Chạy headless rồi thoát, không mở UI và không giữ mutex.
            if (Cli.TryHandle(args, out int cliExit))
            {
                Environment.Exit(cliExit);
                return;
            }

            // Fix lỗi #2: chặn mở 2-3 bản cùng lúc. Nếu đã có bản đang chạy,
            // đưa cửa sổ đó lên trước rồi thoát — tránh double-download,
            // chạy uninstaller trùng, và reboot/bcdedit nhiều lần.
            _instance = new SingleInstance("AntiVirusFinderTool");
            if (!_instance.IsFirstInstance)
            {
                SingleInstance.FocusExistingInstance();
                _instance.Dispose();
                return;
            }

            // Bắt mọi exception chưa xử lý để không rơi vào trạng thái nguy hiểm im lặng.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { Log.Write($"FATAL: {(e.ExceptionObject as Exception)?.ToString() ?? e.ExceptionObject?.ToString()}"); } catch { }
            };
            Application.ThreadException += (s, e) =>
            {
                Log.Write($"UI EXCEPTION: {e.Exception}");
                MessageBox.Show($"Đã xảy ra lỗi: {e.Exception.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Log.Write($"=== App started (elevated={Elevation.IsElevated()}) ===");

                // Manifest yêu cầu Admin nên bình thường luôn elevated; nếu vì lý do
                // nào đó không phải, cảnh báo vì bcdedit/Safe Mode/cleanup sẽ fail.
                if (!Elevation.IsElevated())
                {
                    MessageBox.Show(
                        "Ứng dụng đang KHÔNG chạy với quyền Administrator.\n" +
                        "Các chức năng Safe Mode (bcdedit), gỡ AV và dọn tàn dư có thể thất bại.\n" +
                        "Hãy chạy lại bằng 'Run as administrator'.",
                        "Cần quyền Administrator", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                Application.Run(new Form1());
            }
            finally
            {
                _instance.Dispose();
                Log.Write("=== App exited ===");
            }
        }
    }
}
