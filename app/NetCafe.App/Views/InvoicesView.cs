// Trách nhiệm: đối soát hóa đơn, void có audit và xuất PDF/Excel từ DTO đã lọc.
// Liên kết: billing.invoice, billing.payment, ExportService và DbRepository.

using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NetCafe.App.Services;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class InvoicesView : AdminViewBase
{
    // Toolbar
    private readonly Panel _toolbar = new();
    private readonly Label _titleLabel = new();
    private readonly Label _subtitleLabel = new();
    private readonly Button _refreshButton = new();

    // KPI Cards Panel
    private readonly Panel _kpiContainer = new();
    private readonly Label _kpiRevenueValue = new();
    private readonly Label _kpiCountValue = new();
    private readonly Label _kpiPlayValue = new();
    private readonly Label _kpiServiceValue = new();

    // Filter Bar
    private readonly Panel _filterPanel = new();
    private readonly TextBox _searchBox = new();
    private readonly ComboBox _statusCombo = new();
    private readonly Button _searchButton = new();
    private readonly Button _clearFilterButton = new();
    private readonly Label _countSummaryLabel = new();

    // Master-Detail Workspace
    private readonly SplitContainer _splitContainer = new();
    private readonly DataGridView _grid = new();

    // Right: Digital Receipt Panel
    private readonly Panel _receiptPanel = new();
    private readonly Label _receiptHeaderStore = new();
    private readonly Label _receiptHeaderSub = new();
    private readonly Label _receiptInvoiceIdLabel = new();
    private readonly Label _receiptStatusBadge = new();
    private readonly Label _receiptCustomerLabel = new();
    private readonly Label _receiptSessionLabel = new();
    private readonly Label _receiptTimeLabel = new();
    private readonly Label _receiptPlayLabel = new();
    private readonly Label _receiptServiceLabel = new();
    private readonly Label _receiptDiscountLabel = new();
    private readonly Label _receiptTotalLabel = new();
    private readonly Button _printReceiptButton = new();
    private readonly Button _copyReceiptButton = new();
    private readonly Button _voidInvoiceButton = new();
    private readonly Button _exportExcelButton = new();

    private List<InvoiceItem> _allInvoices = new();
    private List<InvoiceItem> _filteredInvoices = new();
    private InvoiceItem? _selectedInvoice;

    public InvoicesView()
    {
        InitializeComponent();
        BuildLayout();
    }

    private void BuildLayout()
    {
        BackColor = BackgroundColor;
        Dock = DockStyle.Fill;
        Padding = new Padding(12);

        // 1. Toolbar
        _toolbar.Dock = DockStyle.Top;
        _toolbar.Height = 56;
        _toolbar.BackColor = CyberTheme.Surface;
        _toolbar.Padding = new Padding(12, 6, 12, 6);

        _titleLabel.Text = "🧾 Quản lý Hóa đơn & Sổ quỹ";
        _titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _titleLabel.ForeColor = CyberTheme.Text;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(12, 8);

        _subtitleLabel.Text = "Theo dõi doanh thu phòng máy, lịch sử thanh toán giờ chơi và dịch vụ ăn uống.";
        _subtitleLabel.Font = new Font("Segoe UI", 8.5F);
        _subtitleLabel.ForeColor = CyberTheme.MutedText;
        _subtitleLabel.AutoSize = true;
        _subtitleLabel.Location = new Point(13, 31);

        _refreshButton.Text = "🔄 Làm mới";
        _refreshButton.Dock = DockStyle.Right;
        _refreshButton.Width = 110;
        CyberTheme.StyleButton(_refreshButton, PrimaryColor);
        _refreshButton.Click += async (_, _) => await RefreshAsync();

        _toolbar.Controls.Add(_refreshButton);
        _toolbar.Controls.Add(_titleLabel);
        _toolbar.Controls.Add(_subtitleLabel);

        // 2. KPI Summary Cards Panel
        _kpiContainer.Dock = DockStyle.Top;
        _kpiContainer.Height = 84;
        _kpiContainer.Padding = new Padding(0, 8, 0, 8);
        _kpiContainer.BackColor = BackgroundColor;
        _kpiContainer.Resize += (_, _) => ResizeKpiCards();

        BuildKpiCards();

        // 3. Filter Bar
        _filterPanel.Dock = DockStyle.Top;
        _filterPanel.Height = 44;
        _filterPanel.BackColor = CyberTheme.Surface;
        _filterPanel.Padding = new Padding(10, 6, 10, 6);

        _searchBox.Width = 240;
        _searchBox.Height = 28;
        _searchBox.Location = new Point(10, 8);
        _searchBox.BackColor = CyberTheme.SurfaceRaised;
        _searchBox.ForeColor = CyberTheme.Text;
        _searchBox.BorderStyle = BorderStyle.FixedSingle;
        _searchBox.Font = new Font("Segoe UI", 9.5F);
        _searchBox.PlaceholderText = "🔍 Tìm mã HĐ, khách hàng...";
        _searchBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) ApplyFilter(); };

        _statusCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusCombo.Items.AddRange(new object[]
        {
            "Tất cả chứng từ",
            "💰 Sổ quỹ nạp tiền ví",
            "🧾 Hóa đơn phiên chơi",
            "✓ Đã thanh toán (paid)",
            "○ Chưa thanh toán (unpaid)",
            "✕ Đã hủy (void)"
        });
        _statusCombo.SelectedIndex = 0;
        _statusCombo.Location = new Point(260, 8);
        _statusCombo.Width = 195;
        _statusCombo.BackColor = CyberTheme.SurfaceRaised;
        _statusCombo.ForeColor = CyberTheme.Text;
        _statusCombo.Font = new Font("Segoe UI", 9F);
        _statusCombo.SelectedIndexChanged += (_, _) => ApplyFilter();

        _searchButton.Text = "🔍 Tìm";
        _searchButton.Width = 75;
        _searchButton.Height = 28;
        _searchButton.Location = new Point(450, 8);
        CyberTheme.StyleButton(_searchButton, PrimaryColor);
        _searchButton.Click += (_, _) => ApplyFilter();

        _clearFilterButton.Text = "✕ Xóa lọc";
        _clearFilterButton.Width = 85;
        _clearFilterButton.Height = 28;
        _clearFilterButton.Location = new Point(532, 8);
        CyberTheme.StyleButton(_clearFilterButton, CyberTheme.SurfaceRaised);
        _clearFilterButton.Click += (_, _) =>
        {
            _searchBox.Text = string.Empty;
            _statusCombo.SelectedIndex = 0;
            ApplyFilter();
        };

        _exportExcelButton.Text = "📊 Xuất Excel";
        _exportExcelButton.Width = 108;
        _exportExcelButton.Height = 28;
        _exportExcelButton.Dock = DockStyle.Right;
        CyberTheme.StyleButton(_exportExcelButton, CyberTheme.Success);
        _exportExcelButton.Click += async (_, _) => await ExportExcelAsync();

        _countSummaryLabel.Dock = DockStyle.Right;
        _countSummaryLabel.AutoSize = true;
        _countSummaryLabel.Font = new Font("Segoe UI", 9F);
        _countSummaryLabel.ForeColor = CyberTheme.MutedText;
        _countSummaryLabel.Padding = new Padding(0, 6, 10, 0);

        _filterPanel.Controls.Add(_searchBox);
        _filterPanel.Controls.Add(_statusCombo);
        _filterPanel.Controls.Add(_searchButton);
        _filterPanel.Controls.Add(_clearFilterButton);
        _filterPanel.Controls.Add(_countSummaryLabel);
        _filterPanel.Controls.Add(_exportExcelButton);

        // 4. Master-Detail Workspace
        _splitContainer.Dock = DockStyle.Fill;
        _splitContainer.Orientation = Orientation.Vertical;
        _splitContainer.SplitterDistance = 680;
        _splitContainer.SplitterWidth = 6;
        _splitContainer.BackColor = CyberTheme.SurfaceRaised;

        // Left: Grid
        _splitContainer.Panel1.BackColor = BackgroundColor;
        _splitContainer.Panel1.Padding = new Padding(0, 8, 4, 0);
        StyleGrid(_grid);
        _grid.CellFormatting += Grid_CellFormatting;
        _grid.SelectionChanged += Grid_SelectionChanged;
        _grid.CellDoubleClick += async (_, _) => await ShowInvoiceDetailAsync();
        _splitContainer.Panel1.Controls.Add(_grid);

        // Right: Digital Receipt Panel
        _splitContainer.Panel2.BackColor = BackgroundColor;
        _splitContainer.Panel2.Padding = new Padding(4, 8, 0, 0);
        BuildReceiptPanel();
        _splitContainer.Panel2.Controls.Add(_receiptPanel);

        // Assemble View
        Controls.Add(_splitContainer);
        Controls.Add(_filterPanel);
        Controls.Add(_kpiContainer);
        Controls.Add(_toolbar);
    }

    private void BuildKpiCards()
    {
        _kpiContainer.Controls.Clear();

        var card1 = CreateKpiCard("💳 TỔNG THU SỔ QUỸ", "0 đ", CyberTheme.Primary, _kpiRevenueValue);
        var card2 = CreateKpiCard("🧾 SỐ LƯỢNG CHỨNG TỪ", "0", CyberTheme.Text, _kpiCountValue);
        var card3 = CreateKpiCard("💰 TIỀN NẠP VÍ", "0 đ", CyberTheme.Success, _kpiPlayValue);
        var card4 = CreateKpiCard("🎮 TIỀN GIỜ & DỊCH VỤ", "0 đ", CyberTheme.Warning, _kpiServiceValue);

        _kpiContainer.Controls.Add(card4);
        _kpiContainer.Controls.Add(card3);
        _kpiContainer.Controls.Add(card2);
        _kpiContainer.Controls.Add(card1);

        ResizeKpiCards();
    }

    private Panel CreateKpiCard(string title, string initialValue, Color valueColor, Label valueLabelRef)
    {
        var panel = new Panel
        {
            Height = 68,
            BackColor = CyberTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12, 8, 12, 8)
        };

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = title,
            ForeColor = CyberTheme.MutedText,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold)
        };

        valueLabelRef.Dock = DockStyle.Fill;
        valueLabelRef.Text = initialValue;
        valueLabelRef.ForeColor = valueColor;
        valueLabelRef.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        valueLabelRef.TextAlign = ContentAlignment.MiddleLeft;

        var accentStrip = new Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = valueColor
        };

        panel.Controls.Add(valueLabelRef);
        panel.Controls.Add(titleLabel);
        panel.Controls.Add(accentStrip);

        return panel;
    }

    private void ResizeKpiCards()
    {
        var totalWidth = _kpiContainer.ClientSize.Width;
        if (totalWidth <= 0 || _kpiContainer.Controls.Count == 0) return;

        var count = _kpiContainer.Controls.Count;
        var margin = 8;
        var cardWidth = (totalWidth - (margin * (count - 1))) / count;

        var x = 0;
        // In reverse because docked controls or explicit positioning
        for (int i = _kpiContainer.Controls.Count - 1; i >= 0; i--)
        {
            var card = _kpiContainer.Controls[i];
            card.Location = new Point(x, 8);
            card.Size = new Size(cardWidth, 68);
            x += cardWidth + margin;
        }
    }

    private void BuildReceiptPanel()
    {
        _receiptPanel.Dock = DockStyle.Fill;
        _receiptPanel.BackColor = CyberTheme.Surface;
        _receiptPanel.BorderStyle = BorderStyle.FixedSingle;
        _receiptPanel.Padding = new Padding(16);
        _receiptPanel.AutoScroll = true;

        // Receipt Brand
        _receiptHeaderStore.Text = "NETCAFE CYBER GAMING";
        _receiptHeaderStore.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _receiptHeaderStore.ForeColor = CyberTheme.Primary;
        _receiptHeaderStore.TextAlign = ContentAlignment.MiddleCenter;
        _receiptHeaderStore.Dock = DockStyle.Top;
        _receiptHeaderStore.Height = 28;

        _receiptHeaderSub.Text = "— PHIẾU THANH TOÁN DỊCH VỤ —";
        _receiptHeaderSub.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        _receiptHeaderSub.ForeColor = CyberTheme.MutedText;
        _receiptHeaderSub.TextAlign = ContentAlignment.MiddleCenter;
        _receiptHeaderSub.Dock = DockStyle.Top;
        _receiptHeaderSub.Height = 22;

        var sep1 = CreateDottedSeparator();
        sep1.Dock = DockStyle.Top;

        // Invoice ID and Badge
        var idRow = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(0, 4, 0, 4) };
        _receiptInvoiceIdLabel.Text = "#HĐ-00000";
        _receiptInvoiceIdLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _receiptInvoiceIdLabel.ForeColor = CyberTheme.Text;
        _receiptInvoiceIdLabel.Dock = DockStyle.Left;
        _receiptInvoiceIdLabel.AutoSize = true;

        _receiptStatusBadge.Text = "ĐÃ THANH TOÁN";
        _receiptStatusBadge.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _receiptStatusBadge.ForeColor = CyberTheme.Success;
        _receiptStatusBadge.Dock = DockStyle.Right;
        _receiptStatusBadge.AutoSize = true;
        idRow.Controls.Add(_receiptInvoiceIdLabel);
        idRow.Controls.Add(_receiptStatusBadge);

        // Info Rows
        var infoPanel = new Panel { Dock = DockStyle.Top, Height = 84, Padding = new Padding(0, 4, 0, 4) };
        _receiptTimeLabel.Text = "Thời gian: --";
        _receiptTimeLabel.Font = new Font("Segoe UI", 9F);
        _receiptTimeLabel.ForeColor = CyberTheme.MutedText;
        _receiptTimeLabel.Dock = DockStyle.Top;
        _receiptTimeLabel.Height = 24;

        _receiptCustomerLabel.Text = "Khách hàng: --";
        _receiptCustomerLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _receiptCustomerLabel.ForeColor = CyberTheme.Text;
        _receiptCustomerLabel.Dock = DockStyle.Top;
        _receiptCustomerLabel.Height = 26;

        _receiptSessionLabel.Text = "Mã phiên máy: --";
        _receiptSessionLabel.Font = new Font("Segoe UI", 9F);
        _receiptSessionLabel.ForeColor = CyberTheme.MutedText;
        _receiptSessionLabel.Dock = DockStyle.Top;
        _receiptSessionLabel.Height = 24;

        infoPanel.Controls.Add(_receiptSessionLabel);
        infoPanel.Controls.Add(_receiptTimeLabel);
        infoPanel.Controls.Add(_receiptCustomerLabel);

        var sep2 = CreateDottedSeparator();
        sep2.Dock = DockStyle.Top;

        // Breakdown items
        var breakdownPanel = new Panel { Dock = DockStyle.Top, Height = 95, Padding = new Padding(0, 6, 0, 6) };

        _receiptPlayLabel.Text = "🎮 Tiền giờ chơi: 0 đ";
        _receiptPlayLabel.Font = new Font("Segoe UI", 9.5F);
        _receiptPlayLabel.ForeColor = CyberTheme.Text;
        _receiptPlayLabel.Dock = DockStyle.Top;
        _receiptPlayLabel.Height = 26;

        _receiptServiceLabel.Text = "🍜 Tiền dịch vụ: 0 đ";
        _receiptServiceLabel.Font = new Font("Segoe UI", 9.5F);
        _receiptServiceLabel.ForeColor = CyberTheme.Text;
        _receiptServiceLabel.Dock = DockStyle.Top;
        _receiptServiceLabel.Height = 26;

        _receiptDiscountLabel.Text = "🏷️ Giảm giá: -0 đ";
        _receiptDiscountLabel.Font = new Font("Segoe UI", 9.5F);
        _receiptDiscountLabel.ForeColor = CyberTheme.Warning;
        _receiptDiscountLabel.Dock = DockStyle.Top;
        _receiptDiscountLabel.Height = 26;

        breakdownPanel.Controls.Add(_receiptDiscountLabel);
        breakdownPanel.Controls.Add(_receiptServiceLabel);
        breakdownPanel.Controls.Add(_receiptPlayLabel);

        var sep3 = CreateDottedSeparator();
        sep3.Dock = DockStyle.Top;

        // Total
        var totalPanel = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(0, 8, 0, 8) };
        var totalTitle = new Label
        {
            Text = "TỔNG THANH TOÁN",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = CyberTheme.MutedText,
            Dock = DockStyle.Top,
            Height = 18
        };
        _receiptTotalLabel.Text = "0 đ";
        _receiptTotalLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        _receiptTotalLabel.ForeColor = CyberTheme.Primary;
        _receiptTotalLabel.Dock = DockStyle.Fill;

        totalPanel.Controls.Add(_receiptTotalLabel);
        totalPanel.Controls.Add(totalTitle);

        // Buttons Action
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(0, 6, 0, 0),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        _copyReceiptButton.Text = "📋 Sao chép";
        _copyReceiptButton.Width = 110;
        _copyReceiptButton.Dock = DockStyle.Left;
        CyberTheme.StyleButton(_copyReceiptButton, CyberTheme.SurfaceRaised);
        _copyReceiptButton.Click += CopyReceiptDetails;

        _printReceiptButton.Text = "📄 Xuất PDF";
        _printReceiptButton.Width = 112;
        CyberTheme.StyleButton(_printReceiptButton, PrimaryColor);
        _printReceiptButton.Click += async (_, _) => await ExportPdfAsync();

        _voidInvoiceButton.Text = "🚫 Hủy hóa đơn";
        _voidInvoiceButton.Width = 126;
        CyberTheme.StyleButton(_voidInvoiceButton, CyberTheme.Danger);
        _voidInvoiceButton.Click += async (_, _) => await VoidSelectedInvoiceAsync();

        buttonPanel.Controls.Add(_copyReceiptButton);
        buttonPanel.Controls.Add(_voidInvoiceButton);
        buttonPanel.Controls.Add(_printReceiptButton);

        // Assemble Receipt Panel in order (Reverse order for Dock = Top)
        _receiptPanel.Controls.Add(buttonPanel);
        _receiptPanel.Controls.Add(totalPanel);
        _receiptPanel.Controls.Add(sep3);
        _receiptPanel.Controls.Add(breakdownPanel);
        _receiptPanel.Controls.Add(sep2);
        _receiptPanel.Controls.Add(infoPanel);
        _receiptPanel.Controls.Add(idRow);
        _receiptPanel.Controls.Add(sep1);
        _receiptPanel.Controls.Add(_receiptHeaderSub);
        _receiptPanel.Controls.Add(_receiptHeaderStore);
    }

    private Control CreateDottedSeparator()
    {
        var panel = new Panel
        {
            Height = 14,
            BackColor = Color.Transparent
        };
        panel.Paint += (s, e) =>
        {
            using var pen = new Pen(CyberTheme.Border, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            e.Graphics.DrawLine(pen, 0, 7, panel.Width, 7);
        };
        return panel;
    }

    public override void SetContext(AdminViewContext context)
    {
        base.SetContext(context);
        UpdateRolePermissions(context);
    }

    private void UpdateRolePermissions(AdminViewContext context)
    {
        if (context.IsAdmin)
        {
            _voidInvoiceButton.Enabled = true;
            _exportExcelButton.Enabled = true;
            _printReceiptButton.Enabled = true;
            _copyReceiptButton.Enabled = true;
            _subtitleLabel.Text = "Theo dõi doanh thu phòng máy, xem 100% hóa đơn toàn hệ thống và void có kiểm soát.";
        }
        else if (context.IsManager)
        {
            _voidInvoiceButton.Enabled = false;
            _exportExcelButton.Enabled = true;
            _printReceiptButton.Enabled = true;
            _copyReceiptButton.Enabled = true;
            _subtitleLabel.Text = "Quản lý xem toàn bộ hóa đơn; chỉ Siêu quản trị được void hóa đơn.";
        }
        else if (context.IsCashier)
        {
            _voidInvoiceButton.Enabled = false; // Thu ngân không được hủy hóa đơn
            _exportExcelButton.Enabled = true;
            _printReceiptButton.Enabled = true;
            _copyReceiptButton.Enabled = true;
            _subtitleLabel.Text = "🔒 Chỉ hiển thị hóa đơn ca làm việc của bạn (Cơ chế CSDL: Row-Level Security p_invoice_cashier).";
        }
        else
        {
            _voidInvoiceButton.Enabled = false;
            _exportExcelButton.Enabled = false;
            _printReceiptButton.Enabled = false;
            _copyReceiptButton.Enabled = false;
            _subtitleLabel.Text = "🔒 Role hiện tại không có quyền truy cập Hóa đơn & Sổ quỹ.";
            _allInvoices.Clear();
            _filteredInvoices.Clear();
            _grid.DataSource = null;
            UpdateKpis(_allInvoices);
        }
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (RequiredContext.UserRole == "role_unknown")
        {
            _allInvoices.Clear();
            _filteredInvoices.Clear();
            _grid.DataSource = null;
            UpdateKpis(_allInvoices);
            _subtitleLabel.Text = "🔒 Role hiện tại không có quyền truy cập Hóa đơn & Sổ quỹ.";
            return;
        }

        try
        {
            var rows = await RequiredContext.Repository.GetInvoicesAsync(cancellationToken: cancellationToken);
            _allInvoices = rows.ToList();
            UpdateKpis(_allInvoices);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void UpdateKpis(List<InvoiceItem> rows)
    {
        var paidRows = rows.Where(r => r.Status.Equals("paid", StringComparison.OrdinalIgnoreCase)).ToList();

        var totalRev = paidRows.Sum(r => r.TotalAmount);
        var totalTopup = paidRows.Where(r => r.InvoiceType == "topup").Sum(r => r.TotalAmount);
        var sessionPlay = paidRows.Where(r => r.InvoiceType == "session").Sum(r => r.PlayAmount);
        var sessionService = paidRows.Where(r => r.InvoiceType == "session").Sum(r => r.ServiceAmount);

        _kpiRevenueValue.Text = $"{totalRev:N0} đ";
        _kpiCountValue.Text = $"{rows.Count} chứng từ";
        _kpiPlayValue.Text = $"{totalTopup:N0} đ";
        _kpiServiceValue.Text = $"{(sessionPlay + sessionService):N0} đ";
    }

    private void ApplyFilter()
    {
        var query = _searchBox.Text.Trim();
        var selectedFilterIndex = _statusCombo.SelectedIndex;

        var filtered = _allInvoices.AsEnumerable();

        switch (selectedFilterIndex)
        {
            case 1:
                filtered = filtered.Where(x => x.InvoiceType == "topup");
                break;
            case 2:
                filtered = filtered.Where(x => x.InvoiceType == "session");
                break;
            case 3:
                filtered = filtered.Where(x => x.Status.Equals("paid", StringComparison.OrdinalIgnoreCase));
                break;
            case 4:
                filtered = filtered.Where(x => x.Status.Equals("unpaid", StringComparison.OrdinalIgnoreCase));
                break;
            case 5:
                filtered = filtered.Where(x => x.Status.Equals("void", StringComparison.OrdinalIgnoreCase));
                break;
        }

        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(x =>
                x.InvoiceId.ToString().Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.SessionId.ToString().Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.CustomerName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.ComputerCode.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.Note.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        var resultList = filtered.ToList();
        _filteredInvoices = resultList;
        _grid.DataSource = null;
        _grid.DataSource = resultList;

        FormatGridColumns();
        var sessionCount = resultList.Count(x => x.InvoiceType == "session");
        var topupCount = resultList.Count(x => x.InvoiceType == "topup");
        _countSummaryLabel.Text = $"Hiển thị {resultList.Count} / {_allInvoices.Count} chứng từ ({sessionCount} hóa đơn, {topupCount} phiếu nạp ví)";

        if (resultList.Count > 0)
        {
            _grid.Rows[0].Selected = true;
            UpdateReceiptPreview(resultList[0]);
        }
        else
        {
            ClearReceiptPreview();
        }
    }

    private void FormatGridColumns()
    {
        if (_grid.Columns["InvoiceId"] is { } colId) { colId.HeaderText = "Mã số"; colId.Width = 85; }
        if (_grid.Columns["InvoiceType"] is { } colType) { colType.HeaderText = "Loại chứng từ"; colType.Width = 115; }
        if (_grid.Columns["SessionId"] is { } colSession) { colSession.HeaderText = "Mã phiên"; colSession.Width = 85; }
        if (_grid.Columns["CustomerName"] is { } colCust) { colCust.HeaderText = "Khách hàng"; colCust.Width = 140; }
        if (_grid.Columns["ComputerCode"] is { } colComp) { colComp.HeaderText = "Máy / Nghiệp vụ"; colComp.Width = 125; }
        if (_grid.Columns["TotalAmount"] is { } colTotal)
        {
            colTotal.HeaderText = "Số tiền";
            colTotal.Width = 110;
            colTotal.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            colTotal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        if (_grid.Columns["PaymentMethod"] is { } colMethod) { colMethod.HeaderText = "Hình thức"; colMethod.Width = 100; }
        if (_grid.Columns["Status"] is { } colStatus) { colStatus.HeaderText = "Trạng thái"; colStatus.Width = 120; }
        if (_grid.Columns["IssuedAt"] is { } colTime)
        {
            colTime.HeaderText = "Thời gian lập";
            colTime.Width = 140;
            colTime.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        }
        if (_grid.Columns["PlayAmount"] is { } colPlay) { colPlay.Visible = false; }
        if (_grid.Columns["ServiceAmount"] is { } colService) { colService.Visible = false; }
        if (_grid.Columns["DiscountAmount"] is { } colDiscount) { colDiscount.Visible = false; }
        if (_grid.Columns["Note"] is { } colNote) { colNote.Visible = false; }
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;

        var colName = _grid.Columns[e.ColumnIndex].Name;
        if (e.Value is decimal amount && (colName.EndsWith("Amount", StringComparison.OrdinalIgnoreCase) ||
                                          colName.EndsWith("Total", StringComparison.OrdinalIgnoreCase)))
        {
            e.Value = $"{amount:N0} đ";
            e.FormattingApplied = true;
            return;
        }

        if (colName.Equals("InvoiceType", StringComparison.OrdinalIgnoreCase) && e.Value != null)
        {
            var type = e.Value.ToString()?.ToLowerInvariant();
            if (type == "topup")
            {
                e.Value = "💰 Nạp ví";
                e.CellStyle!.ForeColor = CyberTheme.Success;
                e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
            else
            {
                e.Value = "🧾 Phiên chơi";
                e.CellStyle!.ForeColor = CyberTheme.Primary;
            }
            e.FormattingApplied = true;
            return;
        }

        if (colName.Equals("SessionId", StringComparison.OrdinalIgnoreCase) && e.Value is long sid)
        {
            if (sid <= 0)
            {
                e.Value = "—";
                e.FormattingApplied = true;
                return;
            }
        }

        if (colName.Equals("PaymentMethod", StringComparison.OrdinalIgnoreCase) && e.Value != null)
        {
            var rawMethod = e.Value.ToString()?.ToLowerInvariant() ?? "";
            e.Value = rawMethod switch
            {
                "cash" => "Tiền mặt",
                "transfer" or "bank_transfer" => "Chuyển khoản",
                "balance" => "Số dư ví",
                "card" => "Thẻ",
                _ => rawMethod
            };
            e.FormattingApplied = true;
            return;
        }

        if (colName.Equals("Status", StringComparison.OrdinalIgnoreCase) && e.Value != null)
        {
            var raw = e.Value.ToString()?.ToLowerInvariant() ?? "";
            switch (raw)
            {
                case "paid":
                    e.Value = "✓ Đã thanh toán";
                    e.CellStyle!.ForeColor = CyberTheme.Success;
                    e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    break;
                case "void":
                    e.Value = "✕ Đã hủy";
                    e.CellStyle!.ForeColor = CyberTheme.Danger;
                    break;
                case "unpaid":
                    e.Value = "○ Chưa thanh toán";
                    e.CellStyle!.ForeColor = CyberTheme.Warning;
                    break;
                default:
                    e.Value = raw;
                    break;
            }
            e.FormattingApplied = true;
        }
    }

    private void Grid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.DataBoundItem is InvoiceItem item)
        {
            _selectedInvoice = item;
            UpdateReceiptPreview(item);
        }
    }

    private void UpdateReceiptPreview(InvoiceItem inv)
    {
        _selectedInvoice = inv;
        var isTopup = inv.InvoiceType == "topup";
        _receiptHeaderSub.Text = isTopup ? "— PHIẾU THU NẠP TIỀN VÍ —" : "— PHIẾU THANH TOÁN DỊCH VỤ —";
        _receiptInvoiceIdLabel.Text = isTopup ? $"#TOP-{inv.InvoiceId:D5}" : $"#HĐ-{inv.InvoiceId:D5}";
        _receiptStatusBadge.Text = inv.Status.ToLowerInvariant() switch
        {
            "paid" => "✓ ĐÃ THANH TOÁN",
            "unpaid" => "○ CHƯA THANH TOÁN",
            _ => "✕ ĐÃ HỦY"
        };
        _receiptStatusBadge.ForeColor = inv.Status.ToLowerInvariant() switch
        {
            "paid" => CyberTheme.Success,
            "unpaid" => CyberTheme.Warning,
            _ => CyberTheme.Danger
        };

        var methodText = FormatMethod(inv.PaymentMethod);
        _receiptCustomerLabel.Text = isTopup ? $"Hội viên: {inv.CustomerName}" : $"Khách hàng: {inv.CustomerName}";
        _receiptSessionLabel.Text = isTopup ? $"Nghiệp vụ: Nạp tiền ví · {inv.ComputerCode}" : $"Máy {inv.ComputerCode} · Phiên #{inv.SessionId}";
        _receiptTimeLabel.Text = $"Thời gian: {inv.IssuedAt:dd/MM/yyyy HH:mm:ss} · {methodText}";

        if (isTopup)
        {
            _receiptPlayLabel.Text = $"💵 Số tiền nạp: {inv.TotalAmount:N0} đ";
            _receiptServiceLabel.Text = $"📝 Ghi chú: {(string.IsNullOrWhiteSpace(inv.Note) ? "Nạp thẻ hội viên" : inv.Note)}";
            _receiptDiscountLabel.Text = "✓ Đã cộng trực tiếp vào số dư ví";
            _voidInvoiceButton.Enabled = false;
        }
        else
        {
            _receiptPlayLabel.Text = $"🎮 Tiền giờ chơi: {inv.PlayAmount:N0} đ";
            _receiptServiceLabel.Text = $"🍜 Tiền dịch vụ: {inv.ServiceAmount:N0} đ";
            _receiptDiscountLabel.Text = $"🏷️ Giảm giá: -{inv.DiscountAmount:N0} đ";
            _voidInvoiceButton.Enabled = RequiredContext.IsAdmin && inv.Status == "paid";
        }
        _receiptTotalLabel.Text = $"{inv.TotalAmount:N0} đ";
    }

    private static string FormatMethod(string raw)
    {
        return (raw ?? "").ToLowerInvariant() switch
        {
            "cash" => "Tiền mặt",
            "transfer" or "bank_transfer" => "Chuyển khoản",
            "balance" => "Số dư ví",
            "card" => "Thẻ",
            _ => raw ?? ""
        };
    }

    private void ClearReceiptPreview()
    {
        _selectedInvoice = null;
        _receiptInvoiceIdLabel.Text = "#HĐ-00000";
        _receiptStatusBadge.Text = "CHƯA CHỌN";
        _receiptStatusBadge.ForeColor = CyberTheme.MutedText;
        _receiptCustomerLabel.Text = "Khách hàng: --";
        _receiptSessionLabel.Text = "Mã phiên máy: --";
        _receiptTimeLabel.Text = "Thời gian: --";
        _receiptPlayLabel.Text = "🎮 Tiền giờ chơi: 0 đ";
        _receiptServiceLabel.Text = "🍜 Tiền dịch vụ: 0 đ";
        _receiptDiscountLabel.Text = "🏷️ Giảm giá: 0 đ";
        _receiptTotalLabel.Text = "0 đ";
    }

    private void CopyReceiptDetails(object? sender, EventArgs e)
    {
        if (_selectedInvoice == null)
        {
            MessageBox.Show("Vui lòng chọn một chứng từ để sao chép.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var isTopup = _selectedInvoice.InvoiceType == "topup";
        var sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("       NETCAFE CYBER GAMING");
        sb.AppendLine(isTopup ? "     PHIẾU THU NẠP TIỀN VÍ" : "     PHIẾU THANH TOÁN DỊCH VỤ");
        sb.AppendLine("========================================");
        sb.AppendLine($"Mã chứng từ: {(isTopup ? $"#TOP-{_selectedInvoice.InvoiceId:D5}" : $"#HĐ-{_selectedInvoice.InvoiceId:D5}")}");
        sb.AppendLine($"Trạng thái:  {_selectedInvoice.Status.ToUpperInvariant()}");
        sb.AppendLine($"Khách hàng:  {_selectedInvoice.CustomerName}");
        if (isTopup)
        {
            sb.AppendLine($"Nghiệp vụ:   Nạp tiền ví hội viên");
            sb.AppendLine($"Thời gian:   {_selectedInvoice.IssuedAt:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Hình thức:   {FormatMethod(_selectedInvoice.PaymentMethod)}");
            sb.AppendLine($"Ghi chú:     {_selectedInvoice.Note}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"SỐ TIỀN NẠP: {_selectedInvoice.TotalAmount:N0} đ");
        }
        else
        {
            sb.AppendLine($"Mã phiên:    #{_selectedInvoice.SessionId} (Máy {_selectedInvoice.ComputerCode})");
            sb.AppendLine($"Thời gian:   {_selectedInvoice.IssuedAt:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Hình thức:   {FormatMethod(_selectedInvoice.PaymentMethod)}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"Tiền giờ:    {_selectedInvoice.PlayAmount:N0} đ");
            sb.AppendLine($"Tiền dịch vụ:{_selectedInvoice.ServiceAmount:N0} đ");
            sb.AppendLine($"Giảm giá:   -{_selectedInvoice.DiscountAmount:N0} đ");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"TỔNG CỘNG:   {_selectedInvoice.TotalAmount:N0} đ");
        }
        sb.AppendLine("========================================");

        Clipboard.SetText(sb.ToString());
        MessageBox.Show($"Đã sao chép thông tin chứng từ #{_selectedInvoice.InvoiceId} vào clipboard!", "Đã sao chép", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task ExportPdfAsync()
    {
        if (_selectedInvoice == null)
        {
            MessageBox.Show("Vui lòng chọn một hóa đơn để xuất PDF.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"hoa-don-{_selectedInvoice.InvoiceId}.pdf"
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var detail = await RequiredContext.Repository.GetInvoiceDetailAsync(_selectedInvoice.InvoiceId);
            if (detail is null)
            {
                throw new InvalidOperationException("Không tìm thấy chi tiết hóa đơn để xuất PDF.");
            }
            ExportService.ExportInvoicePdf(detail, dialog.FileName);
            MessageBox.Show(FindForm(), $"Đã xuất PDF: {dialog.FileName}", "Xuất PDF thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task ExportExcelAsync()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = "danh-sach-hoa-don.xlsx"
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        try
        {
            ExportService.ExportInvoicesExcel(_filteredInvoices, dialog.FileName);
            MessageBox.Show(FindForm(), $"Đã xuất Excel: {dialog.FileName}", "Xuất Excel thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
        await Task.CompletedTask;
    }

    private async Task VoidSelectedInvoiceAsync()
    {
        if (_selectedInvoice is null)
        {
            return;
        }
        if (_selectedInvoice.InvoiceType == "topup")
        {
            MessageBox.Show(
                FindForm(),
                "Phiếu thu nạp tiền ví đã được cộng trực tiếp vào số dư hội viên; không thể hủy tại mục hóa đơn.",
                "Không thể hủy",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }
        if (!_selectedInvoice.Status.Equals("paid", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(FindForm(), "Chỉ hóa đơn paid mới được hủy.", "Không thể hủy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (RequiredContext.CurrentEmployeeId <= 0)
        {
            ReportError(new InvalidOperationException("Role hiện tại chưa ánh xạ employee_id; không thể hủy hóa đơn."));
            return;
        }

        var confirm = MessageBox.Show(
            FindForm(),
            "Cảnh báo: hành vi hủy hóa đơn sẽ được ghi vào nhật ký kiểm toán và AI Security Alert. Bạn có chắc chắn muốn hủy?",
            "Xác nhận hủy hóa đơn",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await RequiredContext.Repository.VoidInvoiceAsync(_selectedInvoice.InvoiceId, RequiredContext.CurrentEmployeeId);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task ShowInvoiceDetailAsync()
    {
        if (_selectedInvoice is null)
        {
            return;
        }

        try
        {
            var detail = await RequiredContext.Repository.GetInvoiceDetailAsync(_selectedInvoice.InvoiceId);
            if (detail is null)
            {
                return;
            }
            var lines = detail.Lines.Count == 0
                ? "Không có món dịch vụ."
                : string.Join(Environment.NewLine, detail.Lines.Select(line => $"- {line.ProductName} x{line.Quantity}: {line.LineTotal:N0} đ"));
            MessageBox.Show(
                FindForm(),
                $"Hóa đơn #{detail.InvoiceId}\nKhách: {detail.CustomerName} (@{detail.Username})\nMáy: {detail.ComputerCode}\nPhiên: {detail.SessionStart:dd/MM/yyyy HH:mm} → {detail.SessionEnd:dd/MM/yyyy HH:mm}\n\nMón đã gọi:\n{lines}\n\nTổng: {detail.TotalAmount:N0} đ",
                "Chi tiết hóa đơn",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }
}
