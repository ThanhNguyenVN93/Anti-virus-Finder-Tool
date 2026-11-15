using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace frmavfinnder
{
    public partial class Form1 : Form
    {
        // ── Download state ──
        private string _partPath;        // File tạm .part đang tải
        private string _finalPath;       // File đích sau khi tải+xác minh xong
        private AvEntry _targetEntry;    // Mục catalog đang xử lý
        private CancellationTokenSource _cts;   // Hủy download
        private bool _downloading;

        // ── Trạng thái bền vững giữa các lần chạy ──
        private AppState _state;

        // Nút phụ thêm bằng code (không đụng Designer).
        private Button _btnCancel;
        private Button _btnOpenData;
        private Button _btnLang;
        private LinkLabel _lblUpdate;
        private UpdateCheck.Result _updateResult;
        private Panel _header;
        private Label _lblTitle;
        private Label _lblSubtitle;

        // Bảng màu dùng chung.
        private static readonly System.Drawing.Color Accent = System.Drawing.Color.FromArgb(0, 120, 215);
        private static readonly System.Drawing.Color AccentDark = System.Drawing.Color.FromArgb(0, 99, 177);

        public Form1()
        {
            InitializeComponent();
            SetupExtraButtons();
        }

        // Header + nút phụ (thiết kế lại giao diện).
        private void SetupExtraButtons()
        {
            // ── Thanh header màu nhấn ở trên cùng ──
            _header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Accent
            };

            _lblTitle = new Label
            {
                Text = "🛡  ANTIVIRUS FINDER",
                AutoSize = true,
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(14, 7),
                BackColor = System.Drawing.Color.Transparent
            };
            _lblSubtitle = new Label
            {
                Text = "Phát hiện & gỡ antivirus bên thứ ba • v1.2",
                AutoSize = true,
                ForeColor = System.Drawing.Color.FromArgb(220, 235, 250),
                Font = new System.Drawing.Font("Segoe UI", 8F),
                Location = new System.Drawing.Point(16, 34),
                BackColor = System.Drawing.Color.Transparent
            };

            _btnLang = MakeHeaderButton("VI / EN");
            _btnLang.Location = new System.Drawing.Point(272, 6);   // tinh chỉnh lại ở OnShown
            _btnLang.Click += (s, e) => { Loc.Toggle(); ApplyLanguage(); };

            _btnOpenData = MakeHeaderButton("📂 Data/Log");
            _btnOpenData.Location = new System.Drawing.Point(360, 6);
            _btnOpenData.Click += (s, e) => { try { Process.Start("explorer.exe", AppPaths.DataDir); } catch { } };

            _lblUpdate = new LinkLabel
            {
                Text = "",
                AutoSize = true,
                Visible = false,
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold),
                LinkColor = System.Drawing.Color.White,
                ActiveLinkColor = System.Drawing.Color.FromArgb(255, 235, 150),
                BackColor = System.Drawing.Color.Transparent
            };
            _lblUpdate.LinkClicked += (s, e) => ShowUpdateDialog();

            _header.Controls.Add(_lblTitle);
            _header.Controls.Add(_lblSubtitle);
            _header.Controls.Add(_btnLang);
            _header.Controls.Add(_btnOpenData);
            _header.Controls.Add(_lblUpdate);

            // ── Nút HỦY khi đang tải (nằm trên form, cạnh thanh tiến trình) ──
            _btnCancel = new Button
            {
                Text = "HỦY",
                Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold),
                Size = new System.Drawing.Size(62, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(200, 60, 60),
                ForeColor = System.Drawing.Color.White,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) => { try { _cts?.Cancel(); } catch { } };

            this.Controls.Add(_header);
            // Nút HỦY là con của nhóm "Tải công cụ gỡ", nằm ngay cạnh thanh tiến trình.
            // (progressBar ở (110,55) rộng 228 → kết thúc x=338; đặt HỦY ở x=344.)
            _btnCancel.Location = new System.Drawing.Point(344, 55);
            groupBox2.Controls.Add(_btnCancel);
            _btnCancel.BringToFront();
            _header.BringToFront();

            // Tiêu đề các nhóm dùng màu nhấn cho đồng bộ.
            foreach (var gb in new[] { groupBox1, groupBox2, groupBox3, groupBox4 })
                gb.ForeColor = Accent;
        }

        private Button MakeHeaderButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold),
                Size = new System.Drawing.Size(84, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = System.Drawing.Color.White,
                ForeColor = Accent,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(225, 236, 248);
            return b;
        }

        // Áp ngôn ngữ cho các nhãn UI chính (Feature round2 #12).
        private void ApplyLanguage()
        {
            try
            {
                groupBox1.Text = Loc.T("grpAvCheck");
                groupBox2.Text = Loc.T("grpDownload");
                groupBox3.Text = Loc.T("grpOs");
                groupBox4.Text = Loc.T("grpSupport");
                btnavcheck.Text = Loc.T("btnCheck");
                btndownload.Text = Loc.T("btnDownload");
                lblavname.Text = Loc.T("lblAvName");
                label1.Text = Loc.T("lblDirectory");
                if (_lblSubtitle != null)
                    _lblSubtitle.Text = Loc.Pick("Phát hiện & gỡ antivirus bên thứ ba • v1.2",
                                                 "Detect & remove third-party antivirus • v1.2");
            }
            catch (Exception ex)
            {
                Log.Write($"ApplyLanguage error: {ex.Message}");
            }
        }

        protected override void OnShown(EventArgs e)
        {
            // Căn nút phụ trong header TRƯỚC khi vẽ (form cố định kích thước nên ổn định).
            int hw = _header.ClientSize.Width;
            _btnOpenData.Location = new System.Drawing.Point(hw - 90, 6);
            _btnLang.Location = new System.Drawing.Point(hw - 90 - 88, 6);
            _btnOpenData.BringToFront();
            _btnLang.BringToFront();
            base.OnShown(e);

            // Feature round2 #13: kiểm tra bản mới ở background.
            Task.Run(() => UpdateCheck.Check()).ContinueWith(t =>
            {
                var r = t.Result;
                _updateResult = r;
                if (r != null && r.HasUpdate && !IsDisposed)
                {
                    try
                    {
                        BeginInvoke(new Action(() =>
                        {
                            // Thay subtitle bằng thông báo bản mới (tránh chồng chữ).
                            _lblSubtitle.Visible = false;
                            _lblUpdate.Text = $"⬆ Có bản mới {r.LatestVersion} — xem & tải";
                            _lblUpdate.Location = new System.Drawing.Point(16, 34);
                            _lblUpdate.Visible = true;
                            _lblUpdate.BringToFront();
                        }));
                    }
                    catch { }
                }
            });
        }

        // Hộp thoại "Có gì mới" + tải bản mới (Feature round2 #13 nâng cấp).
        private async void ShowUpdateDialog()
        {
            var r = _updateResult;
            if (r == null) { try { Process.Start("https://github.com/ThanhNguyenVN93/Anti-virus-Finder-Tool/releases"); } catch { } return; }

            string notes = string.IsNullOrWhiteSpace(r.Notes) ? "(không có mô tả)" : r.Notes.Trim();
            if (notes.Length > 1500) notes = notes.Substring(0, 1500) + "\n...";

            bool hasAsset = !string.IsNullOrEmpty(r.AssetUrl);
            var buttons = hasAsset ? MessageBoxButtons.YesNoCancel : MessageBoxButtons.OKCancel;

            var choice = MessageBox.Show(
                $"Phiên bản mới: {r.LatestVersion}  (đang dùng {r.CurrentVersion})\n\n" +
                $"— Có gì mới —\n{notes}\n\n" +
                (hasAsset
                    ? "[Yes] Tải bản mới   •   [No] Mở trang release   •   [Cancel] Đóng"
                    : "[OK] Mở trang release   •   [Cancel] Đóng"),
                "Cập nhật", buttons, MessageBoxIcon.Information);

            if (choice == DialogResult.Cancel) return;

            if (hasAsset && choice == DialogResult.Yes)
            {
                await DownloadUpdateAsset(r);
                return;
            }

            // Mở trang release.
            try { Process.Start(r.Url ?? "https://github.com/ThanhNguyenVN93/Anti-virus-Finder-Tool/releases"); }
            catch { }
        }

        private async Task DownloadUpdateAsset(UpdateCheck.Result r)
        {
            try
            {
                string dest = AppPaths.DownloadPath(string.IsNullOrEmpty(r.AssetName) ? "update-download" : r.AssetName);
                string part = dest + ".part";
                lblstatus.Text = "Đang tải bản mới...";
                using (var cts = new CancellationTokenSource())
                {
                    var prog = new Progress<DownloadProgress>(p =>
                    {
                        if (p.TotalBytes > 0)
                            lblstatus.Text = $"Tải bản mới... {p.BytesReceived * 100 / p.TotalBytes}%";
                    });
                    var outcome = await Downloader.DownloadAsync(r.AssetUrl, part, prog, cts.Token);
                    if (!outcome.Success)
                    {
                        lblstatus.Text = "Tải bản mới thất bại.";
                        MessageBox.Show($"Không tải được bản mới: {outcome.Message}", "Lỗi",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                try { if (File.Exists(dest)) File.Delete(dest); File.Move(part, dest); } catch { }
                lblstatus.Text = "Đã tải bản mới.";
                // Mở thư mục chứa file để người dùng tự chạy/giải nén (tránh tự thay exe đang chạy).
                try { Process.Start("explorer.exe", $"/select,\"{dest}\""); } catch { }
            }
            catch (Exception ex)
            {
                Log.Write($"DownloadUpdateAsset error: {ex.Message}");
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Text = $"ANTI-VIRUS FINDER TOOL V1.2 - {DateTime.Now:HH:mm:ss}";
        }

        // ────────────────────────────────────────────────────
        //  FORM LOAD
        // ────────────────────────────────────────────────────
        private void Form1_Load(object sender, EventArgs e)
        {
            lblavstatus.Visible = true;
            lblavstatus.Text = "";

            _state = AppState.Load();

            // Cập nhật catalog online ở background (Feature #6) — không chặn UI.
            Task.Run(() => AvCatalog.TryRefreshFromOnline());

            // ── GUARD SAFE MODE ──
            // Fix C3: nếu đang ở Safe Mode nhưng không có tiến trình gỡ hợp lệ
            // (file chưa tải / chưa xác minh / state không phải safemode_pending),
            // thì KHÔNG chạy tiếp như bình thường (sẽ kẹt Safe Mode + lỗi WMI),
            // mà xóa cờ safeboot để thoát.
            if (SafeBoot.IsInSafeMode)
            {
                bool hasValidResume =
                    _state.Phase == "safemode_pending" &&
                    !string.IsNullOrEmpty(_state.DownloadedFile) &&
                    File.Exists(_state.DownloadedFile) &&
                    _state.DownloadVerified;

                if (hasValidResume)
                {
                    Log.Write($"Resuming Safe Mode uninstall for {_state.TargetAv}");
                    LaunchForm2(_state.DownloadedFile, _state.TargetAv);
                    return;
                }
                else
                {
                    SafeBoot.RecoverFromStuckSafeMode(
                        "Không có file uninstaller đã xác minh để tiếp tục.");
                    _state.Reset();
                    // Tiếp tục hiển thị thông tin bình thường (không tự động gì nữa).
                }
            }

            // Phát hiện hệ điều hành qua WMI.
            string r = "";
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        using (obj)
                        {
                            r = obj["Caption"] + " - " + obj["OSArchitecture"];
                        }
                    }
                }
                r = r.Replace("NT 5.1.2600", "XP").Replace("NT 5.2.3790", "Server 2003");
            }
            catch (Exception ex)
            {
                r = $"Không thể xác định hệ điều hành ({ex.Message})";
            }
            lblos.Text = r;

            PopulateAvSupportList();
            ApplyLanguage();

            // Cho phép bỏ qua auto-scan lúc mở (hữu ích cho ảnh chụp/CI/kỹ thuật viên
            // muốn tự bấm CHECK). Bật bằng biến môi trường AVFINDER_NO_AUTOSCAN=1.
            if (Environment.GetEnvironmentVariable("AVFINDER_NO_AUTOSCAN") == "1")
                return;

            btnavcheck.PerformClick();
        }

        private void PopulateAvSupportList()
        {
            lbAvSupport.Items.Clear();
            foreach (var entry in AvCatalog.Entries)
            {
                string mark = entry.HasRemover ? "✓" : "•";
                string note = entry.RequiresSafeMode ? " (Safe Mode)"
                            : (!entry.HasRemover ? " (detect only)" : "");
                lbAvSupport.Items.Add($"{mark} {entry.Name}{note}");
            }
        }

        // ────────────────────────────────────────────────────
        //  PHÁT HIỆN ANTIVIRUS (background thread)
        // ────────────────────────────────────────────────────
        private string DetectAntivirus()
        {
            // Feature nhóm 2 #5: phát hiện đa nguồn (SecurityCenter2 + registry + service).
            // Fix H3 (chuẩn hoá path) và M2 (nhận Microsoft Defender) nằm trong Detection.
            try
            {
                var first = Detection.DetectFirst(out _);
                return first?.DisplayName;
            }
            catch (Exception ex)
            {
                Log.Write($"DetectAntivirus error: {ex.Message}");
                return null;
            }
        }

        private bool IsWindows7()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        using (obj)
                        {
                            string version = obj["Version"]?.ToString() ?? "";
                            uint productType = (uint)(obj["ProductType"] ?? 0u);
                            return version.StartsWith("6.1") && productType == 1;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        // ────────────────────────────────────────────────────
        //  NÚT ANTI-VIRUS CHECK
        // ────────────────────────────────────────────────────
        private async void btnavcheck_Click_1(object sender, EventArgs e)
        {
            lblavstatus.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            lblavstatus.Text = Loc.Pick("Đang quét...", "Scanning...");
            txtfoundav.Text = "";
            txtDirectory.Text = "";
            btnavcheck.Enabled = false;
            btndownload.Enabled = false;

            List<DetectionResult> allAv = null;
            DetectionResult first = await Task.Run(() => Detection.DetectFirst(out allAv));
            string detectedAvName = first?.DisplayName;

            if (string.IsNullOrEmpty(detectedAvName))
            {
                // Feature nhóm 1 #4: nếu lần trước vừa gỡ AV mà giờ không còn
                // phát hiện nữa → báo thành công và kiểm tra Windows Defender.
                if (!string.IsNullOrEmpty(_state.LastResultAv))
                {
                    PostRemovalCheck(_state.LastResultAv);
                    _state.LastResultAv = "";
                    _state.Reset();
                }
                ShowNoThirdPartyAv(await Task.Run(() => IsWindows7()));
                btnavcheck.Enabled = true;
                return;
            }

            // Feature round2 #8: nhiều AV cùng lúc → xử lý từng cái qua các lần chạy.
            if (allAv != null && allAv.Count > 1)
            {
                string list = string.Join("\n", allAv.Select(a => $"  • {a.DisplayName} [{a.Source}]"));
                MessageBox.Show(
                    Loc.Pick(
                        $"Phát hiện {allAv.Count} phần mềm diệt virus:\n{list}\n\nSẽ xử lý trước: {detectedAvName}.\nSau khi gỡ xong và khởi động lại, chạy lại tool để gỡ phần còn lại.",
                        $"Detected {allAv.Count} antivirus products:\n{list}\n\nWill handle first: {detectedAvName}.\nAfter it is removed and you reboot, run the tool again for the rest."),
                    Loc.Pick("Nhiều AV", "Multiple AV"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            _targetEntry = AvCatalog.FindByDisplayName(detectedAvName);

            // Feature round3 #7: chính sách doanh nghiệp có thể giới hạn AV được gỡ.
            if (_targetEntry != null && !Policy.Current.IsAvAllowed(_targetEntry.Name))
            {
                lblavstatus.Text = Loc.Pick("Bị chặn bởi policy", "Blocked by policy");
                txtfoundav.Text = detectedAvName;
                txtDirectory.Text = Loc.Pick($"Policy không cho phép gỡ {_targetEntry.Name}.", $"Policy does not allow removing {_targetEntry.Name}.");
                btndownload.Enabled = false;
                btnavcheck.Enabled = true;
                return;
            }

            lblavstatus.ForeColor = System.Drawing.Color.FromArgb(200, 120, 0);
            lblavstatus.Text = Loc.Pick("Phát hiện:", "Found:");
            txtfoundav.Text = detectedAvName;

            // AV có trong catalog nhưng chưa có remover (BKAV/CMC...).
            if (_targetEntry == null || !_targetEntry.HasRemover)
            {
                txtDirectory.Text = Loc.Pick("Chưa có công cụ gỡ chính thức tải trực tiếp cho AV này.", "No direct official removal tool for this AV yet.");
                btndownload.Enabled = false;
                btnavcheck.Enabled = true;
                return;
            }

            txtDirectory.Text = _targetEntry.Url;
            btndownload.Enabled = true;

            // Fix lỗi #1 + C1: KHÔNG tự tải lại một cách im lặng.
            // Nếu AV này vừa được gỡ ở lần trước (entry có thể còn rác tới khi reboot),
            // không rủ gỡ lại — tránh vòng lặp tải/ gỡ/ reboot.
            if (!string.IsNullOrEmpty(_state.LastResultAv) &&
                _state.LastResultAv.Equals(_targetEntry.Name, StringComparison.OrdinalIgnoreCase))
            {
                lblavstatus.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                lblavstatus.Text = Loc.Pick("Đã xử lý:", "Handled:");
                txtDirectory.Text = Loc.Pick(
                    $"{_targetEntry.Name} đã được gỡ ở lần chạy trước. Nếu entry còn hiển thị, hãy khởi động lại máy. Bấm DOWNLOAD nếu muốn chạy lại thủ công.",
                    $"{_targetEntry.Name} was removed in a previous run. If the entry still shows, please reboot. Click DOWNLOAD to run it again manually.");
                btnavcheck.Enabled = true;
                return;
            }

            // Nếu đã có file đã tải + xác minh từ trước → đề nghị dùng lại, không tải lại.
            if (!string.IsNullOrEmpty(_state.DownloadedFile) &&
                File.Exists(_state.DownloadedFile) &&
                _state.DownloadVerified &&
                _state.TargetAv.Equals(_targetEntry.Name, StringComparison.OrdinalIgnoreCase))
            {
                var choice = MessageBox.Show(
                    Loc.Pick(
                        $"Đã có công cụ gỡ {_targetEntry.Name} tải trước đó (đã xác minh):\n{_state.DownloadedFile}\n\nDùng lại file này?\n\n[Yes] Chạy file đã có   •   [No] Tải lại mới   •   [Cancel] Không làm gì",
                        $"A {_targetEntry.Name} remover was already downloaded (verified):\n{_state.DownloadedFile}\n\nReuse this file?\n\n[Yes] Run existing   •   [No] Download again   •   [Cancel] Do nothing"),
                    Loc.Pick("File đã tồn tại", "File exists"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (choice == DialogResult.Yes)
                {
                    _finalPath = _state.DownloadedFile;
                    ProceedAfterDownload();
                    return;
                }
                if (choice == DialogResult.Cancel)
                {
                    btnavcheck.Enabled = true;
                    return;
                }
                // No → tải lại.
            }

            // Feature round3 #7: policy có thể tắt auto-run → chỉ bật nút, chờ người dùng.
            if (Policy.Current.DisableAutoRun)
            {
                txtDirectory.Text = _targetEntry.Url + Loc.Pick("  (bấm DOWNLOAD để tải — auto-run bị tắt bởi policy)", "  (click DOWNLOAD — auto-run disabled by policy)");
                btnavcheck.Enabled = true;
                return;
            }

            // Hỏi xác nhận trước khi tải (Feature #2: không còn thao tác bất ngờ).
            string safeNote = _targetEntry.RequiresSafeMode
                ? Loc.Pick("\n⚠️ AV này cần khởi động vào Safe Mode để gỡ sạch. Máy sẽ cần restart.",
                           "\n⚠️ This AV needs a Safe Mode reboot for a clean removal. The PC will restart.")
                : "";
            var confirm = MessageBox.Show(
                Loc.Pick(
                    $"Phát hiện: {detectedAvName}\n\nTải công cụ gỡ chính thức ({_targetEntry.Name}) và chạy nó?\n" + safeNote,
                    $"Detected: {detectedAvName}\n\nDownload the official removal tool ({_targetEntry.Name}) and run it?\n" + safeNote),
                Loc.Pick("Xác nhận", "Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                btnavcheck.Enabled = true;
                return;
            }

            btndownload_Click(btndownload, EventArgs.Empty);
        }

        // Hậu kiểm sau khi gỡ AV: xác nhận đã sạch + bật lại Defender nếu cần.
        private void PostRemovalCheck(string removedAv)
        {
            var defState = Defender.GetRealtimeState();
            Log.Write($"PostRemovalCheck: {removedAv} removed, Defender={defState}");

            if (defState == Defender.State.Disabled)
            {
                var choice = MessageBox.Show(
                    $"✓ {removedAv} đã được gỡ.\n\n" +
                    "Windows Defender hiện đang TẮT real-time protection. " +
                    "Bật lại ngay để máy không bị hở?",
                    "Gỡ thành công — bật lại Defender?",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (choice == DialogResult.Yes)
                {
                    bool ok = Defender.TryEnableRealtime();
                    MessageBox.Show(
                        ok ? "✓ Đã bật lại Windows Defender real-time protection."
                           : "Không tự bật được. Hãy mở Windows Security → Virus & threat protection để bật thủ công.",
                        ok ? "Thành công" : "Cần thao tác thủ công",
                        MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(
                    $"✓ {removedAv} đã được gỡ.\n\n" +
                    (defState == Defender.State.Enabled
                        ? "Windows Defender đang hoạt động — máy vẫn được bảo vệ."
                        : "Không kiểm tra được trạng thái Defender."),
                    "Gỡ thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            OfferRemnantCleanup(removedAv);
        }

        // Feature round2 #9: đề nghị dọn tàn dư (thư mục/dịch vụ còn sót).
        private void OfferRemnantCleanup(string avName)
        {
            try
            {
                var report = Cleanup.Preview(avName);
                if (report.IsEmpty) return;

                var r = MessageBox.Show(
                    $"Phát hiện tàn dư của {avName} còn sót lại:\n\n{report}\n\n" +
                    "Xoá những mục này? (hành động KHÔNG hoàn tác được — đã có điểm khôi phục nếu bạn tạo trước đó)",
                    "Dọn tàn dư", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (r != DialogResult.Yes) return;

                var errors = Cleanup.Apply(report);
                if (errors.Count == 0)
                {
                    MessageBox.Show("✓ Đã dọn sạch tàn dư.", "Hoàn tất",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Một số mục không xoá được (có thể cần khởi động lại):\n\n" +
                        string.Join("\n", errors),
                        "Dọn tàn dư — còn sót", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Log.Write($"OfferRemnantCleanup error: {ex.Message}");
            }
        }

        private void ShowNoThirdPartyAv(bool isWin7)
        {
            if (isWin7)
            {
                lblavstatus.ForeColor = System.Drawing.Color.FromArgb(200, 0, 0);
                lblavstatus.Text = Loc.Pick("⚠️ Không được bảo vệ:", "⚠️ Not Protected:");
                txtfoundav.Text = Loc.Pick("Windows 7 - Không tìm thấy antivirus", "Windows 7 - No antivirus found");
                txtDirectory.Text = Loc.Pick("Khuyến nghị: dùng AV nhẹ (Avast/AVG) hoặc nâng cấp Windows 10", "Recommend: Use a lightweight AV (Avast/AVG) or update to Windows 10");
            }
            else
            {
                lblavstatus.ForeColor = System.Drawing.Color.FromArgb(0, 150, 50);
                lblavstatus.Text = Loc.Pick("Được bảo vệ:", "Protected:");
                txtfoundav.Text = Loc.Pick("Windows Defender đang hoạt động", "Windows Defender is active");
                txtDirectory.Text = Loc.Pick("Hệ thống được bảo vệ bởi Windows Defender", "System is protected by Windows Defender");
            }
            btndownload.Enabled = false;
        }

        private void LaunchForm2(string uninstallerPath, string avName)
        {
            Form2 safeModeFrm = new Form2(uninstallerPath, avName, _state);
            safeModeFrm.FormClosed += (s, args) => Application.Exit();
            safeModeFrm.Show();
            this.Hide();
        }

        // ────────────────────────────────────────────────────
        //  NÚT DOWNLOAD (async, HttpClient, hủy được)
        // ────────────────────────────────────────────────────
        private async void btndownload_Click(object sender, EventArgs e)
        {
            if (_downloading) return;

            if (_targetEntry == null || !_targetEntry.HasRemover)
            {
                MessageBox.Show("Không có link tải hợp lệ cho AV này.", "ERRORS",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Feature round3 #3: chỉ cho tải từ host tin cậy qua HTTPS.
            if (!Uri.TryCreate(_targetEntry.Url, UriKind.Absolute, out Uri uriResult) ||
                !DownloadPolicy.IsAllowed(uriResult))
            {
                MessageBox.Show(
                    $"Link tải không hợp lệ hoặc host không nằm trong danh sách tin cậy:\n{_targetEntry.Url}",
                    "ERRORS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string fileName = _targetEntry.ResolveFileName();
            _finalPath = AppPaths.DownloadPath(fileName);
            _partPath = _finalPath + ".part";
            try { if (File.Exists(_partPath)) File.Delete(_partPath); } catch { }

            _downloading = true;
            _cts = new CancellationTokenSource();
            lblstatus.Text = "Starting...";
            lblspeed.Text = "0 KB/s";
            progressBarDownload.Value = 0;
            btndownload.Enabled = false;
            btnavcheck.Enabled = false;
            _btnCancel.Visible = true;
            _btnCancel.BringToFront();

            var progress = new Progress<DownloadProgress>(p =>
            {
                if (p.TotalBytes > 0)
                {
                    int pct = (int)Math.Max(0, Math.Min(100, p.BytesReceived * 100 / p.TotalBytes));
                    progressBarDownload.Value = pct;
                    lblstatus.Text = $"Downloading... ({pct}%)";
                }
                else
                {
                    lblstatus.Text = "Downloading...";
                }
                string total = p.TotalBytes > 0 ? $"{p.TotalBytes / 1_048_576.0:N2} MB" : "? MB";
                lblspeed.Text = $"{p.BytesReceived / 1_048_576.0:N2} MB / {total}  |  {p.SpeedKBps:N0} KB/s";
            });

            Log.Write($"Download start: {_targetEntry.Name} <- {DownloadPolicy.Describe(uriResult)}");
            Downloader.Outcome outcome;
            try
            {
                outcome = await Downloader.DownloadAsync(_targetEntry.Url, _partPath, progress, _cts.Token);
            }
            catch (Exception ex)
            {
                outcome = new Downloader.Outcome { Success = false, Message = ex.Message };
            }

            _downloading = false;
            _btnCancel.Visible = false;
            _cts?.Dispose();
            _cts = null;
            btnavcheck.Enabled = true;

            if (outcome.Cancelled)
            {
                lblstatus.Text = "Download Canceled.";
                TryDeletePart();
                btndownload.Enabled = true;
                return;
            }
            if (!outcome.Success)
            {
                lblstatus.Text = "Download Failed!";
                TryDeletePart();
                MessageBox.Show($"Download Unsuccessful: {outcome.Message}",
                    "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btndownload.Enabled = true;
                return;
            }

            if (!File.Exists(_partPath) || new FileInfo(_partPath).Length == 0)
            {
                lblstatus.Text = "Download invalid.";
                TryDeletePart();
                MessageBox.Show("File tải về trống hoặc không hợp lệ.", "Download Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btndownload.Enabled = true;
                return;
            }

            bool isExe = _finalPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            bool verified = false;

            if (!string.IsNullOrEmpty(_targetEntry.Sha256))
            {
                // Tool tự phát hành (BKAV/CMC) không ký số → chốt bằng SHA256.
                lblstatus.Text = "Verifying SHA256...";
                string actual = SignatureVerifier.Sha256(_partPath);
                Log.Write($"SHA256 {_targetEntry.Name}: expected={_targetEntry.Sha256} actual={actual}");
                if (actual == null || !actual.Equals(_targetEntry.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    lblstatus.Text = "❌ SHA256 không khớp!";
                    TryDeletePart();
                    MessageBox.Show(
                        Loc.Pick(
                            $"KHÔNG dùng file vì SHA256 không khớp giá trị đã ghim trong danh mục.\n\nKỳ vọng: {_targetEntry.Sha256}\nThực tế:  {actual ?? "(lỗi đọc)"}",
                            $"NOT using the file: SHA256 does not match the pinned value.\n\nExpected: {_targetEntry.Sha256}\nActual:   {actual ?? "(read error)"}"),
                        Loc.Pick("Xác minh hash thất bại", "Hash Verification Failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btndownload.Enabled = true;
                    return;
                }
                verified = true;
                lblstatus.Text = "✓ Verified (SHA256)";
            }
            else if (isExe)
            {
                // Fix H1: xác minh chữ ký số trước khi chạy exe.
                lblstatus.Text = "Verifying signature...";
                var verify = SignatureVerifier.Verify(_partPath, _targetEntry.ExpectedSigner);
                Log.Write($"Verify {_targetEntry.Name}: trusted={verify.IsTrusted} signer={verify.SignerSubject} msg={verify.Message}");

                if (!verify.IsTrusted)
                {
                    lblstatus.Text = "❌ Signature check failed!";
                    TryDeletePart();
                    MessageBox.Show(
                        Loc.Pick(
                            $"KHÔNG chạy file vì xác minh chữ ký thất bại:\n\n{verify.Message}\n\nVì file sẽ chạy với quyền Admin, tool từ chối chạy file không rõ nguồn gốc.",
                            $"NOT running the file: signature verification failed:\n\n{verify.Message}\n\nSince it would run as Admin, the tool refuses files of unknown origin."),
                        Loc.Pick("Xác minh chữ ký thất bại", "Signature Verification Failed"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btndownload.Enabled = true;
                    return;
                }
                verified = true;
                lblstatus.Text = $"✓ Verified: {verify.SignerSubject}";
            }

            // Đổi .part → file đích.
            try
            {
                if (File.Exists(_finalPath)) File.Delete(_finalPath);
                File.Move(_partPath, _finalPath);
            }
            catch (Exception ex)
            {
                TryDeletePart();
                MessageBox.Show($"Không thể hoàn tất file tải: {ex.Message}", "File Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btndownload.Enabled = true;
                return;
            }

            _state.TargetAv = _targetEntry.Name;
            _state.DownloadedFile = _finalPath;
            _state.DownloadVerified = verified;
            _state.Phase = "downloaded";
            _state.Save();

            lblstatus.Text = "Complete!";
            ProceedAfterDownload();
        }

        // ────────────────────────────────────────────────────
        //  SAU KHI ĐÃ CÓ FILE HỢP LỆ → CHẠY
        // ────────────────────────────────────────────────────
        private async void ProceedAfterDownload()
        {
            try
            {
                if (string.IsNullOrEmpty(_finalPath) || !File.Exists(_finalPath))
                {
                    MessageBox.Show("Downloaded file not found.", "File Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btndownload.Enabled = true;
                    return;
                }

                bool isExe = _finalPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

                // File không phải .exe (vd Trend Micro .zip) → mở thư mục cho người dùng.
                if (!isExe)
                {
                    MessageBox.Show(
                        Loc.Pick(
                            $"File đã tải về nhưng không thể tự chạy (không phải .exe):\n{_finalPath}\n\nVui lòng giải nén/chạy thủ công.",
                            $"Downloaded but cannot auto-run (not an .exe):\n{_finalPath}\n\nPlease extract/run it manually."),
                        Loc.Pick("Cần thao tác thủ công", "Manual Action Required"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Process.Start("explorer.exe", $"/select,\"{_finalPath}\"");
                    btndownload.Enabled = true;
                    btnavcheck.Enabled = true;
                    return;
                }

                // Feature round3 #4: preflight trước khi làm việc nguy hiểm.
                if (!RunPreflight(_targetEntry != null && _targetEntry.RequiresSafeMode))
                {
                    btndownload.Enabled = true;
                    btnavcheck.Enabled = true;
                    return;
                }

                // Feature nhóm 1 #3: tạo điểm khôi phục trước khi gỡ (best-effort).
                bool rpOk = CreateRestorePointBestEffort();
                if (Policy.Current.RequireRestorePoint && !rpOk)
                {
                    MessageBox.Show(
                        "Policy yêu cầu phải tạo được điểm khôi phục trước khi gỡ, nhưng không tạo được " +
                        "(System Protection có thể đang tắt). Đã dừng.",
                        "Policy chặn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btndownload.Enabled = true;
                    btnavcheck.Enabled = true;
                    return;
                }

                // AV cần Safe Mode (ESET) → chuyển sang Form2, có xác nhận ở đó.
                if (_targetEntry != null && _targetEntry.RequiresSafeMode)
                {
                    if (!Policy.Current.AllowAutoReboot)
                    {
                        MessageBox.Show(
                            $"{_targetEntry.Name} cần khởi động vào Safe Mode để gỡ, nhưng policy không cho tự khởi động lại.\n" +
                            "Vui lòng gỡ thủ công hoặc đổi policy.",
                            "Policy chặn reboot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        btndownload.Enabled = true;
                        btnavcheck.Enabled = true;
                        return;
                    }
                    _state.Phase = "safemode_pending";
                    _state.Save();
                    LaunchForm2(_finalPath, _targetEntry.Name);
                    return;
                }

                // AV thường → xác nhận lần cuối rồi chạy.
                var confirm = MessageBox.Show(
                    Loc.Pick($"Chạy công cụ gỡ {_targetEntry?.Name} ngay bây giờ?\n({_finalPath})",
                             $"Run the {_targetEntry?.Name} remover now?\n({_finalPath})"),
                    Loc.Pick("Xác nhận chạy", "Confirm run"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                {
                    btndownload.Enabled = true;
                    btnavcheck.Enabled = true;
                    return;
                }

                Log.Write($"Running remover: {_finalPath}");
                Process.Start(_finalPath);

                // Đánh dấu đã xử lý AV này để lần mở sau không rủ gỡ lại (Fix lỗi #1).
                _state.LastResultAv = _targetEntry?.Name ?? "";
                _state.Phase = "completed";
                _state.DownloadVerified = false;   // sẽ cần tải mới nếu muốn chạy lại
                _state.Save();

                await Task.Delay(1500);
                Application.Exit();
            }
            catch (Exception ex)
            {
                Log.Write($"ProceedAfterDownload error: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "System Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btndownload.Enabled = true;
                btnavcheck.Enabled = true;
            }
        }

        // Preflight: tamper protection, installer khác đang chạy, pin laptop.
        // Trả false nếu người dùng quyết định dừng.
        private bool RunPreflight(bool willReboot)
        {
            // Installer khác đang chạy → nên dừng để tránh xung đột.
            if (PreflightChecks.IsAnotherInstallerRunning())
            {
                var r = MessageBox.Show(
                    "Phát hiện một trình cài đặt/gỡ (Windows Installer) khác đang chạy.\n" +
                    "Chạy tiếp có thể gây xung đột. Vẫn tiếp tục?",
                    "Cảnh báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return false;
            }

            // Tamper Protection còn bật → cảnh báo (một số remover sẽ fail).
            bool? tamper = PreflightChecks.IsDefenderTamperProtectionOn();
            if (tamper == true)
            {
                MessageBox.Show(
                    "Windows Defender Tamper Protection đang BẬT.\n\n" +
                    "Một số công cụ gỡ có thể bị chặn. Nếu gỡ thất bại, hãy tắt tạm:\n" +
                    "Windows Security → Virus & threat protection → Manage settings → Tamper Protection = Off.",
                    "Tamper Protection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // Sắp reboot (Safe Mode) mà laptop pin thấp, không cắm sạc → nguy hiểm.
            if (willReboot)
            {
                int? batt = PreflightChecks.GetBatteryPercent();
                if (batt.HasValue && batt.Value < 30 && !PreflightChecks.IsOnAcPower())
                {
                    var r = MessageBox.Show(
                        $"Pin chỉ còn {batt.Value}% và chưa cắm sạc.\n\n" +
                        "Quy trình Safe Mode cần khởi động lại; mất điện giữa chừng có thể gây rủi ro.\n" +
                        "Cắm sạc rồi tiếp tục. Vẫn tiếp tục ngay bây giờ?",
                        "Pin yếu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (r != DialogResult.Yes) return false;
                }
            }

            return true;
        }

        private bool CreateRestorePointBestEffort()
        {
            try
            {
                lblstatus.Text = "Creating restore point...";
                Application.DoEvents();
                bool ok = SystemRestore.TryCreate($"Before removing {_targetEntry?.Name} (AVFinder)");
                lblstatus.Text = ok ? "✓ Restore point created" : "Restore point skipped";
                Log.Write($"Restore point created={ok}");
                return ok;
            }
            catch (Exception ex)
            {
                Log.Write($"CreateRestorePoint error: {ex.Message}");
                return false;
            }
        }

        private void TryDeletePart()
        {
            try { if (!string.IsNullOrEmpty(_partPath) && File.Exists(_partPath)) File.Delete(_partPath); }
            catch { }
        }

        // Hủy download khi đóng form (Fix M4).
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _cts?.Cancel();
                TryDeletePart();
            }
            catch { }
            base.OnFormClosing(e);
        }
    }
}
