using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  THƯ MỤC DỮ LIỆU DÙNG CHUNG
    //  Fix H4: không ghép cứng UserProfile\Downloads nữa.
    //  Dùng %ProgramData%\AVFinder — ổn định kể cả khi UAC elevate
    //  bằng tài khoản admin khác hoặc user đã dời thư mục Downloads.
    // ──────────────────────────────────────────────────────────────
    internal static class AppPaths
    {
        public static string DataDir
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "AVFinder");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string StateFile => Path.Combine(DataDir, "state.json");
        public static string LogFile => Path.Combine(DataDir, "avfinder.log");

        // Tên file tải về nằm trong DataDir để tránh đụng file lạ trong Downloads.
        public static string DownloadPath(string fileName) => Path.Combine(DataDir, fileName);
    }

    // ──────────────────────────────────────────────────────────────
    //  GHI LOG ĐƠN GIẢN (Feature nhóm 3: log + báo cáo)
    // ──────────────────────────────────────────────────────────────
    internal static class Log
    {
        private static readonly object _lock = new object();

        public static void Write(string message)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(AppPaths.LogFile,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}",
                        Encoding.UTF8);
                }
            }
            catch
            {
                // Không để việc ghi log làm app chết.
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  CHẶN CHẠY NHIỀU BẢN (Fix lỗi #2: mở được 2-3 tool cùng lúc)
    // ──────────────────────────────────────────────────────────────
    internal sealed class SingleInstance : IDisposable
    {
        private Mutex _mutex;
        public bool IsFirstInstance { get; private set; }

        public SingleInstance(string name)
        {
            bool createdNew;
            // Global\ để chặn trên mọi session (kể cả khi chạy elevate ở session khác).
            _mutex = new Mutex(initiallyOwned: true, name: $"Global\\{name}", createdNew: out createdNew);
            IsFirstInstance = createdNew;
        }

        public void Dispose()
        {
            try { _mutex?.ReleaseMutex(); } catch { }
            _mutex?.Dispose();
            _mutex = null;
        }

        // Đưa cửa sổ của bản đang chạy lên trước.
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;

        public static void FocusExistingInstance()
        {
            try
            {
                var current = System.Diagnostics.Process.GetCurrentProcess();
                foreach (var p in System.Diagnostics.Process.GetProcessesByName(current.ProcessName))
                {
                    if (p.Id != current.Id && p.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(p.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(p.MainWindowHandle);
                        break;
                    }
                }
            }
            catch
            {
                // Không focus được cũng không sao, chỉ cần không mở thêm bản mới.
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  XÁC MINH CHỮ KÝ SỐ (Fix H1: chạy exe lạ với quyền admin)
    //  Feature nhóm 1: hiện ✅/❌ trước khi chạy uninstaller.
    // ──────────────────────────────────────────────────────────────
    internal static class SignatureVerifier
    {
        // Kết quả xác minh để UI hiển thị.
        public sealed class Result
        {
            public bool IsTrusted;       // Chữ ký Authenticode hợp lệ, chuỗi tin cậy OK.
            public string SignerSubject;  // Tên người ký (CN).
            public string Message;        // Mô tả cho người dùng.
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            [MarshalAs(UnmanagedType.LPWStr)] public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
            public IntPtr pSignatureSettings;
        }

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern int WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID, IntPtr pWVTData);

        private static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 =
            new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

        private const uint WTD_UI_NONE = 2;
        private const uint WTD_REVOKE_NONE = 0;
        private const uint WTD_CHOICE_FILE = 1;
        private const uint WTD_STATEACTION_VERIFY = 1;
        private const uint WTD_STATEACTION_CLOSE = 2;
        // KHÔNG dùng WTD_SAFER_FLAG (0x100): nó áp chính sách SAFER và gây
        // CERT_E_UNTRUSTEDROOT (0x800B0109) ngay cả với file ký hợp lệ (vd kavremvr.exe
        // của Kaspersky). Dùng WTD_REVOCATION_CHECK_NONE để verify chuỗi tin cậy mà
        // không gọi mạng kiểm thu hồi (tránh treo/offline).
        private const uint WTD_REVOCATION_CHECK_NONE = 0x10;

        /// <summary>
        /// Xác minh file: chữ ký Authenticode hợp lệ + (tuỳ chọn) đúng tên người ký kỳ vọng.
        /// </summary>
        public static Result Verify(string filePath, string[] expectedSignerContains = null)
        {
            var result = new Result { IsTrusted = false, SignerSubject = "", Message = "Unknown" };

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                result.Message = "File không tồn tại.";
                return result;
            }

            // Bước 1: WinVerifyTrust — kiểm tra chữ ký và chuỗi tin cậy.
            var fileInfo = new WINTRUST_FILE_INFO
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)),
                pcwszFilePath = filePath,
                hFile = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero
            };

            IntPtr pFile = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));
            IntPtr pData = IntPtr.Zero;
            try
            {
                Marshal.StructureToPtr(fileInfo, pFile, false);

                var data = new WINTRUST_DATA
                {
                    cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_DATA)),
                    dwUIChoice = WTD_UI_NONE,
                    fdwRevocationChecks = WTD_REVOKE_NONE,
                    dwUnionChoice = WTD_CHOICE_FILE,
                    pFile = pFile,
                    dwStateAction = WTD_STATEACTION_VERIFY,
                    dwProvFlags = WTD_REVOCATION_CHECK_NONE
                };

                pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_DATA)));
                Marshal.StructureToPtr(data, pData, false);

                int hr = WinVerifyTrust(IntPtr.Zero, WINTRUST_ACTION_GENERIC_VERIFY_V2, pData);

                // Đóng state.
                data.dwStateAction = WTD_STATEACTION_CLOSE;
                Marshal.StructureToPtr(data, pData, false);
                WinVerifyTrust(IntPtr.Zero, WINTRUST_ACTION_GENERIC_VERIFY_V2, pData);

                if (hr != 0)
                {
                    result.Message = $"File chưa ký số hoặc chữ ký không hợp lệ. (hr=0x{(uint)hr:X8})";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Message = $"Lỗi xác minh chữ ký: {ex.Message}";
                return result;
            }
            finally
            {
                if (pData != IntPtr.Zero) Marshal.FreeHGlobal(pData);
                Marshal.FreeHGlobal(pFile);
            }

            // Bước 2: lấy tên người ký.
            try
            {
                var cert = X509Certificate.CreateFromSignedFile(filePath);
                var cert2 = new X509Certificate2(cert);
                result.SignerSubject = cert2.GetNameInfo(X509NameType.SimpleName, false);
                if (string.IsNullOrEmpty(result.SignerSubject))
                    result.SignerSubject = cert2.Subject;
            }
            catch (Exception ex)
            {
                result.Message = $"Không đọc được thông tin người ký: {ex.Message}";
                return result;
            }

            // Bước 3: (tuỳ chọn) khớp tên người ký với hãng kỳ vọng.
            if (expectedSignerContains != null && expectedSignerContains.Length > 0)
            {
                string subjectLower = result.SignerSubject.ToLowerInvariant();
                bool match = false;
                foreach (var expected in expectedSignerContains)
                {
                    if (!string.IsNullOrEmpty(expected) &&
                        subjectLower.Contains(expected.ToLowerInvariant()))
                    {
                        match = true;
                        break;
                    }
                }

                if (!match)
                {
                    result.IsTrusted = false;
                    result.Message =
                        $"Chữ ký hợp lệ nhưng người ký \"{result.SignerSubject}\" không khớp hãng AV kỳ vọng.";
                    return result;
                }
            }

            result.IsTrusted = true;
            result.Message = $"Đã xác minh chữ ký: {result.SignerSubject}";
            return result;
        }

        public static string Sha256(string filePath)
        {
            try
            {
                using (var sha = SHA256.Create())
                using (var fs = File.OpenRead(filePath))
                {
                    byte[] hash = sha.ComputeHash(fs);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
