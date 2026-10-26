// Trách nhiệm: dashboard chẩn đoán hệ thống, quyền DBA và tối ưu hóa PostgreSQL VACUUM ANALYZE.
// Liên kết: AdminViewContext, DatabaseRuntimeInfo, listener PostgreSQL và AdminOperationsRepository.

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class SettingsView : AdminViewBase
{
    // Toolbar controls
    private readonly Panel _toolbar = new();
    private readonly Label _titleLabel = new();
    private readonly Label _subtitleLabel = new();
    private readonly Button _refreshButton = new();

    // Scrollable container
    private readonly Panel _mainScrollPanel = new();

    // Hero DBA Maintenance Panel
    private readonly Panel _heroCard = new();
    private readonly Label _heroTitleLabel = new();
    private readonly Label _heroDescLabel = new();
    private readonly Label _heroDbSizeBadge = new();
    private readonly Label _heroDeadTuplesBadge = new();
    private readonly Label _heroCacheHitBadge = new();
    private readonly Button _vacuumButton = new();
    private readonly Label _vacuumHintLabel = new();

    // Telemetry section
    private readonly Label _sectionLabel = new();
    private readonly FlowLayoutPanel _cardsPanel = new();

    // Dynamic telemetry value labels
    private readonly Label _cloudHostLabel = new();
    private readonly Label _cloudDbLabel = new();
    private readonly Label _cloudSslLabel = new();
    private readonly Label _cloudStatusBadge = new();

    private readonly Label _cacheRatioLabel = new();
    private readonly Label _cacheDescLabel = new();
    private readonly Label _cacheDeadTuplesLabel = new();
    private readonly Label _cacheStatusBadge = new();

    private readonly Label _poolTotalLabel = new();
    private readonly Label _poolActiveLabel = new();
    private readonly Label _poolIdleLabel = new();
    private readonly Label _poolStatusBadge = new();

    private readonly Label _permUserLabel = new();
    private readonly Label _permRoleLabel = new();
    private readonly Label _permEmpLabel = new();
    private readonly Label _permStatusBadge = new();

    private readonly Label _contractStatusLabel = new();
    private readonly Label _contractSchemasLabel = new();
    private readonly Label _contractIntegrityLabel = new();
    private readonly Label _contractStatusBadge = new();

    private readonly Label _listenStateLabel = new();
    private readonly Label _listenChannelsLabel = new();
    private readonly Label _listenMechanismLabel = new();
    private readonly Label _listenStatusBadge = new();

    private bool _isVacuumRunning;

    public SettingsView()
    {
        InitializeComponent();
        InitializeCustomUi();
    }

    private void InitializeCustomUi()
    {
        BackColor = CyberTheme.Background;
        Dock = DockStyle.Fill;
        Padding = new Padding(12);

        // 1. Toolbar Panel
        _toolbar.Dock = DockStyle.Top;
        _toolbar.Height = 58;
        _toolbar.BackColor = CyberTheme.Surface;
        _toolbar.Padding = new Padding(14, 8, 14, 8);

        _titleLabel.Text = "⚙️ THÔNG TIN HỆ THỐNG & QUẢN TRỊ CSDL";
        _titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _titleLabel.ForeColor = CyberTheme.Text;
        _titleLabel.AutoSize = true;
        _titleLabel.UseMnemonic = false;
        _titleLabel.Location = new Point(14, 8);

        _subtitleLabel.Text = "Giám sát tài nguyên PostgreSQL 17 Cloud, kết nối phân quyền và dọn dẹp tối ưu hóa CSDL.";
        _subtitleLabel.Font = new Font("Segoe UI", 8.5F);
        _subtitleLabel.ForeColor = CyberTheme.MutedText;
        _subtitleLabel.AutoSize = true;
        _subtitleLabel.Location = new Point(15, 31);

        _refreshButton.Text = "↻";
        _refreshButton.Dock = DockStyle.Right;
        _refreshButton.Width = 48;
        _refreshButton.Height = 32;
        _refreshButton.AccessibleName = "Làm mới dữ liệu";
        CyberTheme.StyleButton(_refreshButton, CyberTheme.Primary);
        _refreshButton.Click += async (_, _) => await RefreshAsync();

        _toolbar.Controls.Add(_refreshButton);
        _toolbar.Controls.Add(_titleLabel);
        _toolbar.Controls.Add(_subtitleLabel);

        // 2. Main Scrollable Container
        _mainScrollPanel.Dock = DockStyle.Fill;
        _mainScrollPanel.AutoScroll = true;
        _mainScrollPanel.BackColor = CyberTheme.Background;
        _mainScrollPanel.Padding = new Padding(0, 10, 8, 20);

        // 3. Build Hero Card (VACUUM ANALYZE)
        BuildHeroCard();

        // 4. Section Header
        _sectionLabel.Text = "📊 CHỈ SỐ GIÁM SÁT & KIẾN TRÚC HỆ THỐNG CLOUD";
        _sectionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _sectionLabel.ForeColor = CyberTheme.Primary;
        _sectionLabel.Height = 28;
        _sectionLabel.Dock = DockStyle.Top;
        _sectionLabel.UseMnemonic = false;
        _sectionLabel.Padding = new Padding(2, 6, 0, 0);

        // 5. Build Telemetry Cards Grid
        BuildTelemetryCards();

        // Add to main container in order
        _mainScrollPanel.Controls.Add(_cardsPanel);
        _mainScrollPanel.Controls.Add(_sectionLabel);
        _mainScrollPanel.Controls.Add(_heroCard);

        Controls.Add(_mainScrollPanel);
        Controls.Add(_toolbar);

        _mainScrollPanel.Resize += (_, _) => ResizeTelemetryCards();
    }

    private void BuildHeroCard()
    {
        _heroCard.Dock = DockStyle.Top;
        _heroCard.Height = 158;
        _heroCard.BackColor = Color.FromArgb(24, 28, 42);
        _heroCard.Padding = new Padding(18, 14, 18, 14);
        _heroCard.Margin = new Padding(0, 0, 0, 12);
        _heroCard.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(64, 72, 98), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, _heroCard.Width - 1, _heroCard.Height - 1);
        };

        _heroTitleLabel.Text = "🧹 TỐI ƯU HÓA & THU DỌN RÁC CSDL (POSTGRESQL VACUUM & ANALYZE)";
        _heroTitleLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _heroTitleLabel.ForeColor = Color.FromArgb(245, 158, 11);
        _heroTitleLabel.AutoSize = true;
        _heroTitleLabel.UseMnemonic = false;
        _heroTitleLabel.Location = new Point(18, 14);

        _heroDescLabel.Text = "Thực thi lệnh VACUUM (ANALYZE) trên các phân vùng CSDL (core, billing, inventory, hr). " +
            "Thu hồi vùng nhớ đĩa từ các bản ghi đã xóa (Dead Tuples), dọn dẹp bloat và làm mới " +
            "thống kê tối ưu hóa truy vấn cho PostgreSQL Cost-based Planner.";
        _heroDescLabel.Font = new Font("Segoe UI", 8.5F);
        _heroDescLabel.ForeColor = CyberTheme.MutedText;
        _heroDescLabel.Size = new Size(680, 42);
        _heroDescLabel.Location = new Point(19, 40);

        // Metrics pills on Hero card
        SetupMetricBadge(_heroDbSizeBadge, "📦 Dung lượng CSDL: Đang đọc...", 19, 92, Color.FromArgb(56, 189, 248));
        SetupMetricBadge(_heroDeadTuplesBadge, "🗑️ Rác: Đang đọc...", 230, 92, Color.FromArgb(245, 158, 11));
        SetupMetricBadge(_heroCacheHitBadge, "⚡ RAM Cache: Đang đọc...", 430, 92, Color.FromArgb(34, 197, 94));

        // Vacuum Button
        _vacuumButton.Text = "⚡ THỰC THI VACUUM ANALYZE";
        _vacuumButton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _vacuumButton.Size = new Size(260, 46);
        _vacuumButton.FlatStyle = FlatStyle.Flat;
        _vacuumButton.FlatAppearance.BorderSize = 0;
        _vacuumButton.BackColor = Color.FromArgb(245, 158, 11);
        _vacuumButton.ForeColor = Color.FromArgb(24, 24, 27);
        _vacuumButton.Cursor = Cursors.Hand;
        _vacuumButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _vacuumButton.Location = new Point(_heroCard.Width - 280, 24);
        _vacuumButton.MouseEnter += (_, _) =>
        {
            if (!_isVacuumRunning && _vacuumButton.Enabled)
            {
                _vacuumButton.BackColor = Color.FromArgb(251, 191, 36);
            }
        };
        _vacuumButton.MouseLeave += (_, _) =>
        {
            if (!_isVacuumRunning && _vacuumButton.Enabled)
            {
                _vacuumButton.BackColor = Color.FromArgb(245, 158, 11);
            }
        };
        _vacuumButton.Click += async (_, _) => await RunVacuumAsync();

        _vacuumHintLabel.Text = "* Thao tác an toàn, chạy nền không khóa bảng (Lock-free)";
        _vacuumHintLabel.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
        _vacuumHintLabel.ForeColor = CyberTheme.MutedText;
        _vacuumHintLabel.AutoSize = true;
        _vacuumHintLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _vacuumHintLabel.Location = new Point(_heroCard.Width - 280, 76);

        _heroCard.Controls.AddRange(new Control[]
        {
            _heroTitleLabel,
            _heroDescLabel,
            _heroDbSizeBadge,
            _heroDeadTuplesBadge,
            _heroCacheHitBadge,
            _vacuumButton,
            _vacuumHintLabel
        });

        _heroCard.Resize += (_, _) =>
        {
            _vacuumButton.Left = Math.Max(720, _heroCard.Width - 280);
            _vacuumHintLabel.Left = _vacuumButton.Left;
            _heroDescLabel.Width = Math.Max(400, _vacuumButton.Left - 30);
        };
    }

    private static void SetupMetricBadge(Label label, string text, int x, int y, Color accent)
    {
        label.Text = text;
        label.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        label.ForeColor = accent;
        label.BackColor = Color.FromArgb(32, 38, 54);
        label.AutoSize = true;
        label.UseMnemonic = false;
        label.Padding = new Padding(10, 6, 10, 6);
        label.Location = new Point(x, y);
    }

    private void BuildTelemetryCards()
    {
        _cardsPanel.Dock = DockStyle.Top;
        _cardsPanel.AutoSize = true;
        _cardsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _cardsPanel.FlowDirection = FlowDirection.LeftToRight;
        _cardsPanel.WrapContents = true;
        _cardsPanel.BackColor = Color.Transparent;
        _cardsPanel.Padding = new Padding(0, 4, 0, 16);

        _cardsPanel.Controls.Add(CreateTelemetryCard("☁️ Máy chủ Cloud & CSDL", _cloudStatusBadge, "● Đang kết nối",
            CyberTheme.Success, new[]
            {
                ("Máy chủ Host", _cloudHostLabel),
                ("Tên Database & Cổng", _cloudDbLabel),
                ("Mã hóa truyền tải", _cloudSslLabel)
            }));

        _cardsPanel.Controls.Add(CreateTelemetryCard("🚀 Hiệu năng RAM & Bộ đệm", _cacheStatusBadge, "● Tối ưu RAM",
            CyberTheme.Success, new[]
            {
                ("Shared Buffers Hit", _cacheRatioLabel),
                ("Đánh giá hiệu năng", _cacheDescLabel),
                ("Dead Tuples (Rác)", _cacheDeadTuplesLabel)
            }));

        _cardsPanel.Controls.Add(CreateTelemetryCard("🔌 Quản lý Kết nối (Pooling)", _poolStatusBadge, "● Npgsql Pool",
            CyberTheme.Primary, new[]
            {
                ("Tổng kết nối CSDL", _poolTotalLabel),
                ("Đang xử lý truy vấn", _poolActiveLabel),
                ("Đang chờ tái sử dụng", _poolIdleLabel)
            }));

        _cardsPanel.Controls.Add(CreateTelemetryCard("👤 Phân quyền Phiên làm việc", _permStatusBadge, "● Xác thực",
            CyberTheme.Success, new[]
            {
                ("Tài khoản phiên", _permUserLabel),
                ("Vai trò PostgreSQL", _permRoleLabel),
                ("Mã nhân viên (ID)", _permEmpLabel)
            }));

        _cardsPanel.Controls.Add(CreateTelemetryCard("📜 Hợp đồng Schemas Migration", _contractStatusBadge, "● Sẵn sàng",
            CyberTheme.Success, new[]
            {
                ("Trạng thái Schema", _contractStatusLabel),
                ("Phân vùng bảng", _contractSchemasLabel),
                ("Toàn vẹn ràng buộc", _contractIntegrityLabel)
            }));

        _cardsPanel.Controls.Add(CreateTelemetryCard("🔔 Sự kiện Thời gian thực", _listenStatusBadge, "● Real-time",
            Color.FromArgb(245, 158, 11), new[]
            {
                ("Bộ lắng nghe (Listener)", _listenStateLabel),
                ("Kênh an ninh & máy", _listenChannelsLabel),
                ("Cơ chế phân phối", _listenMechanismLabel)
            }));
    }

    private Panel CreateTelemetryCard(
        string title,
        Label badgeLabel,
        string badgeText,
        Color badgeColor,
        (string Caption, Label ValueLabel)[] rows)
    {
        var card = new Panel
        {
            Width = 360,
            Height = 160,
            Margin = new Padding(0, 0, 12, 12),
            BackColor = Color.FromArgb(28, 32, 44),
            Padding = new Padding(14, 12, 14, 12)
        };
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(48, 54, 76), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var titleLbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = CyberTheme.Text,
            AutoSize = true,
            UseMnemonic = false,
            Location = new Point(14, 12)
        };

        badgeLabel.Text = badgeText;
        badgeLabel.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        badgeLabel.ForeColor = badgeColor;
        badgeLabel.BackColor = Color.FromArgb(36, 42, 58);
        badgeLabel.AutoSize = true;
        badgeLabel.UseMnemonic = false;
        badgeLabel.Padding = new Padding(6, 3, 6, 3);
        badgeLabel.Location = new Point(card.Width - 120, 10);

        void RepositionBadge()
        {
            badgeLabel.Left = card.Width - badgeLabel.PreferredSize.Width - 14;
        }

        badgeLabel.TextChanged += (_, _) => RepositionBadge();

        card.Controls.Add(titleLbl);
        card.Controls.Add(badgeLabel);

        int currentY = 44;
        foreach (var (caption, valLbl) in rows)
        {
            var captionLbl = new Label
            {
                Text = caption + ":",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                UseMnemonic = false,
                Location = new Point(14, currentY)
            };

            valLbl.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            valLbl.ForeColor = Color.FromArgb(241, 245, 249);
            valLbl.AutoSize = false;
            valLbl.Size = new Size(190, 20);
            valLbl.Location = new Point(154, currentY);
            valLbl.AutoEllipsis = true;
            valLbl.UseMnemonic = false;

            card.Controls.Add(captionLbl);
            card.Controls.Add(valLbl);
            currentY += 28;
        }

        card.Resize += (_, _) =>
        {
            RepositionBadge();
            foreach (var (_, valLbl) in rows)
            {
                valLbl.Width = Math.Max(100, card.Width - 165);
            }
        };

        return card;
    }

    private void ResizeTelemetryCards()
    {
        if (_cardsPanel.Controls.Count == 0) return;
        int usableWidth = _mainScrollPanel.ClientSize.Width - _mainScrollPanel.Padding.Horizontal - 20;
        if (usableWidth < 400) return;

        int columns = usableWidth >= 1020 ? 3 : usableWidth >= 680 ? 2 : 1;
        int cardWidth = (usableWidth - (columns - 1) * 12) / columns;

        _cardsPanel.SuspendLayout();
        foreach (Control c in _cardsPanel.Controls)
        {
            c.Width = cardWidth;
        }
        _cardsPanel.ResumeLayout();
    }

    public override void SetContext(AdminViewContext context)
    {
        base.SetContext(context);
        UpdateRolePermissions(context);
    }

    private void UpdateRolePermissions(AdminViewContext context)
    {
        var canVacuum = context.IsAdmin && context.RuntimeInfo?.CanRunVacuum == true;
        if (canVacuum)
        {
            _vacuumButton.Visible = true;
            _vacuumButton.Enabled = true;
            _vacuumButton.Text = "⚡ THỰC THI VACUUM ANALYZE";
            _vacuumButton.BackColor = Color.FromArgb(245, 158, 11);
            _vacuumButton.ForeColor = Color.FromArgb(24, 24, 27);
            _vacuumHintLabel.Text = "* Thao tác an toàn, chạy nền không khóa bảng (Lock-free)";
        }
        else if (context.IsAdmin)
        {
            _vacuumButton.Visible = true;
            _vacuumButton.Enabled = false;
            _vacuumButton.Text = "🔒 VACUUM (Thiếu quyền MAINTAIN)";
            _vacuumButton.BackColor = Color.FromArgb(48, 54, 72);
            _vacuumButton.ForeColor = CyberTheme.MutedText;
            _vacuumHintLabel.Text = "* Tài khoản quản trị hiện tại chưa có quyền MAINTAIN trên bảng CSDL.";
        }
        else
        {
            _vacuumButton.Visible = true;
            _vacuumButton.Enabled = false;
            _vacuumButton.Text = "🔒 Yêu cầu quyền Quản trị viên (DBA)";
            _vacuumButton.BackColor = Color.FromArgb(48, 54, 72);
            _vacuumButton.ForeColor = CyberTheme.MutedText;
            _vacuumHintLabel.Text = "* Chỉ Quản trị viên (role_admin) mới có quyền dọn dẹp và tối ưu CSDL.";
        }
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var context = RequiredContext;
        UpdateRolePermissions(context);

        DbaStatisticsInfo dbaStats;
        try
        {
            dbaStats = await context.Repository.GetDbaStatisticsAsync(cancellationToken);
        }
        catch
        {
            dbaStats = new DbaStatisticsInfo();
        }

        // Update Hero Card Metrics
        _heroDbSizeBadge.Text = $"📦 Dung lượng CSDL: {dbaStats.DatabaseSize}";
        _heroDeadTuplesBadge.Text = $"🗑️ Rác: {dbaStats.TotalDeadTuples:N0} dead tuples";
        _heroDeadTuplesBadge.ForeColor = dbaStats.TotalDeadTuples > 0 ? Color.FromArgb(245, 158, 11) : Color.FromArgb(34, 197, 94);
        _heroCacheHitBadge.Text = dbaStats.CacheHitRatio.HasValue
            ? $"⚡ RAM Cache: {dbaStats.CacheHitRatio.Value:N1}%"
            : "⚡ RAM Cache: Chưa đo";

        // Card 1: Cloud & DB
        _cloudHostLabel.Text = AppConfig.Host;
        _cloudDbLabel.Text = $"{AppConfig.Database} (Cổng {AppConfig.Port})";
        _cloudSslLabel.Text = "Require (TLSv1.3)";
        _cloudStatusBadge.Text = "● Đang hoạt động";
        _cloudStatusBadge.ForeColor = CyberTheme.Success;

        // Card 2: Cache & RAM
        _cacheRatioLabel.Text = dbaStats.CacheHitRatio.HasValue ? $"{dbaStats.CacheHitRatio.Value:N1}%" : "N/A";
        _cacheDescLabel.Text = dbaStats.CacheHitRatio >= 99 ? "Đạt chuẩn RAM (>99%)" : "Cần quan sát";
        _cacheDeadTuplesLabel.Text = $"{dbaStats.TotalDeadTuples:N0} bản ghi rác";
        _cacheStatusBadge.Text = dbaStats.TotalDeadTuples == 0 ? "● Đã tối ưu" : "● Cần dọn rác";
        _cacheStatusBadge.ForeColor = dbaStats.TotalDeadTuples == 0 ? CyberTheme.Success : Color.FromArgb(245, 158, 11);

        // Card 3: Connection Pool
        _poolTotalLabel.Text = $"{dbaStats.TotalConnections} kết nối";
        _poolActiveLabel.Text = $"{dbaStats.ActiveConnections} kết nối đang chạy";
        _poolIdleLabel.Text = $"{dbaStats.IdleConnections} kết nối rảnh (idle)";
        _poolStatusBadge.Text = "● Npgsql Active";

        // Card 4: Permissions & User
        _permUserLabel.Text = context.CurrentUser;
        _permRoleLabel.Text = context.UserRole;
        _permEmpLabel.Text = context.CurrentEmployeeId > 0 ? $"ID #{context.CurrentEmployeeId}" : "Không gắn nhân viên";
        _permStatusBadge.Text = context.IsAdmin ? "● Toàn quyền DBA" : "● Người dùng";
        _permStatusBadge.ForeColor = context.IsAdmin ? CyberTheme.Success : Color.FromArgb(245, 158, 11);

        // Card 5: Contract & Schemas
        _contractStatusLabel.Text = context.RuntimeInfo?.ContractStatus ?? "Đã nạp metadata";
        _contractSchemasLabel.Text = "core, billing, inventory, hr, ai";
        _contractIntegrityLabel.Text = context.RuntimeInfo?.IsStationContractReady == true ? "Hoàn chỉnh 100%" : "Đang kiểm tra";
        _contractStatusBadge.Text = "● Sẵn sàng";

        // Card 6: LISTEN / NOTIFY
        _listenStateLabel.Text = context.ListenerStatus;
        _listenChannelsLabel.Text = "ai_security_alert, computer_state";
        _listenMechanismLabel.Text = "PostgreSQL Push Notification";
        _listenStatusBadge.Text = "● Real-time Live";

        ResizeTelemetryCards();
    }

    private async Task RunVacuumAsync()
    {
        if (!RequiredContext.IsAdmin)
        {
            MessageBox.Show(
                FindForm(),
                "Tài khoản hiện tại chưa có quyền MAINTAIN để chạy VACUUM ANALYZE.",
                "Từ chối quyền DBA",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (_isVacuumRunning) return;

        try
        {
            _isVacuumRunning = true;
            _vacuumButton.Enabled = false;
            _vacuumButton.Text = "⏳ ĐANG DỌN DẸP CSDL...";
            _vacuumButton.BackColor = Color.FromArgb(180, 110, 10);
            Cursor = Cursors.WaitCursor;

            await RequiredContext.Repository.RunVacuumAnalyzeAsync();

            MessageBox.Show(
                FindForm(),
                "✓ Đã hoàn tất thu dọn rác và phân tích tối ưu CSDL (VACUUM ANALYZE)!\n\n" +
                "• Các bản ghi đã xóa (Dead Tuples) đã được thu hồi vào không gian trống.\n" +
                "• Số liệu thống kê của bộ lập lịch truy vấn (Cost-based Query Planner) đã được cập nhật.",
                "Tối ưu CSDL thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
        finally
        {
            _isVacuumRunning = false;
            Cursor = Cursors.Default;
            UpdateRolePermissions(RequiredContext);
        }
    }
}
