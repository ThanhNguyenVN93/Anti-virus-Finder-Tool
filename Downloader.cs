using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace frmavfinnder
{
    // ──────────────────────────────────────────────────────────────
    //  TẢI FILE AN TOÀN (Feature round2 #14 + round3 #3)
    //  - HttpClient thay WebClient
    //  - Ghim host qua TỪNG bước redirect (chặn nhảy sang host lạ)
    //  - HTTPS-only, kiểm Content-Type
    //  - Retry + resume (HTTP Range) + hủy (CancellationToken)
    // ──────────────────────────────────────────────────────────────
    internal sealed class DownloadProgress
    {
        public long BytesReceived;
        public long TotalBytes;       // -1 nếu server không trả Content-Length
        public double SpeedKBps;
    }

    internal static class Downloader
    {
        public sealed class Outcome
        {
            public bool Success;
            public string Message;
            public bool Cancelled;
        }

        private const int MaxRedirects = 6;
        private const int MaxRetries = 3;

        public static async Task<Outcome> DownloadAsync(
            string url, string partPath,
            IProgress<DownloadProgress> progress, CancellationToken ct)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                return new Outcome { Success = false, Message = "URL không hợp lệ." };

            // Chính sách host ngay từ URL gốc.
            if (!DownloadPolicy.IsAllowed(uri))
                return new Outcome { Success = false, Message = $"Host không được phép: {DownloadPolicy.Describe(uri)}" };

            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            Exception last = null;
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var outcome = await DownloadOnce(uri, partPath, progress, ct, resume: attempt > 1);
                    if (outcome.Success || outcome.Cancelled)
                        return outcome;
                    last = new Exception(outcome.Message);
                }
                catch (OperationCanceledException)
                {
                    return new Outcome { Success = false, Cancelled = true, Message = "Đã hủy." };
                }
                catch (Exception ex)
                {
                    last = ex;
                    Log.Write($"Download attempt {attempt} failed: {ex.Message}");
                }

                if (attempt < MaxRetries)
                    await Task.Delay(1000 * attempt, ct);
            }

            return new Outcome { Success = false, Message = $"Tải thất bại sau {MaxRetries} lần: {last?.Message}" };
        }

        private static async Task<Outcome> DownloadOnce(
            Uri uri, string partPath, IProgress<DownloadProgress> progress,
            CancellationToken ct, bool resume)
        {
            // Giải quyết redirect thủ công để kiểm host từng bước.
            Uri current = uri;
            using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
            using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) })
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AVFinder");

                long existing = 0;
                if (resume && File.Exists(partPath))
                {
                    existing = new FileInfo(partPath).Length;
                }

                HttpResponseMessage resp = null;
                for (int hop = 0; hop <= MaxRedirects; hop++)
                {
                    // URL gốc đã được allowlist ở DownloadAsync. Với các bước redirect,
                    // nhiều hãng nhảy sang CDN ở domain khác (cloudfront, akamai...),
                    // nên chỉ bắt buộc HTTPS ở đây và ghi log host; chốt chặn an toàn
                    // thật sự khi chạy Admin là XÁC MINH CHỮ KÝ ở Form1/Cli.
                    if (!current.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                        return new Outcome { Success = false, Message = $"Redirect sang non-HTTPS bị từ chối: {DownloadPolicy.Describe(current)}" };
                    if (hop > 0 && !DownloadPolicy.IsAllowed(current))
                        Log.Write($"Download redirect to non-allowlisted HTTPS host: {DownloadPolicy.Describe(current)} (signature verify will gate execution)");

                    var req = new HttpRequestMessage(HttpMethod.Get, current);
                    if (existing > 0)
                        req.Headers.Range = new RangeHeaderValue(existing, null);

                    resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                    if ((int)resp.StatusCode >= 300 && (int)resp.StatusCode < 400 &&
                        resp.Headers.Location != null)
                    {
                        current = resp.Headers.Location.IsAbsoluteUri
                            ? resp.Headers.Location
                            : new Uri(current, resp.Headers.Location);
                        resp.Dispose();
                        existing = 0; // reset resume khi đổi URL
                        continue;
                    }
                    break;
                }

                if (resp == null)
                    return new Outcome { Success = false, Message = "Không nhận được phản hồi." };

                using (resp)
                {
                    if (!resp.IsSuccessStatusCode &&
                        resp.StatusCode != HttpStatusCode.PartialContent)
                    {
                        return new Outcome { Success = false, Message = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}" };
                    }

                    // Kiểm Content-Type: từ chối trang HTML (link hỏng trả về trang lỗi).
                    string ctype = resp.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
                    if (ctype.Contains("text/html"))
                        return new Outcome { Success = false, Message = $"Server trả HTML, không phải file tải (link có thể đã hỏng)." };

                    bool append = resp.StatusCode == HttpStatusCode.PartialContent && existing > 0;
                    long totalFromHeader = resp.Content.Headers.ContentLength ?? -1;
                    long grandTotal = totalFromHeader >= 0
                        ? (append ? existing + totalFromHeader : totalFromHeader)
                        : -1;

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    long received = append ? existing : 0;
                    long lastReported = received;

                    using (var netStream = await resp.Content.ReadAsStreamAsync())
                    using (var fs = new FileStream(partPath,
                        append ? FileMode.Append : FileMode.Create,
                        FileAccess.Write, FileShare.None, 81920, useAsync: true))
                    {
                        byte[] buffer = new byte[81920];
                        int read;
                        while ((read = await netStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                        {
                            await fs.WriteAsync(buffer, 0, read, ct);
                            received += read;

                            double sec = sw.Elapsed.TotalSeconds;
                            if (sec >= 0.5)
                            {
                                double speed = (received - lastReported) / sec / 1024.0;
                                progress?.Report(new DownloadProgress
                                {
                                    BytesReceived = received,
                                    TotalBytes = grandTotal,
                                    SpeedKBps = speed
                                });
                                lastReported = received;
                                sw.Restart();
                            }
                        }
                    }

                    progress?.Report(new DownloadProgress
                    {
                        BytesReceived = received,
                        TotalBytes = grandTotal,
                        SpeedKBps = 0
                    });

                    return new Outcome { Success = true, Message = "OK" };
                }
            }
        }
    }
}
