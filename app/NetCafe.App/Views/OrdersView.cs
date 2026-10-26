// Trách nhiệm: menu gọi món, tồn kho dịch vụ và hàng đợi bếp từ máy trạm.
// Liên kết: inventory.product, inventory.food_order, sp_update_order_status và sp_receive_stock.

using System.Drawing;
using System.Windows.Forms;
using NetCafe.App.Controls;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class OrdersView : AdminViewBase
{
    private readonly Panel _toolbar = new();
    private readonly Label _titleLabel = new();
    private readonly Label _subtitleLabel = new();
    private readonly Button _refreshAllButton = new();

    private readonly SplitContainer _splitContainer = new();

    // Menu section (Top)
    private readonly Panel _menuHeaderPanel = new();
    private readonly Label _menuTitleLabel = new();
    private readonly Label _menuCountLabel = new();
    private readonly ComboBox _categoryCombo = new();
    private readonly Button _receiveStockButton = new();
    private readonly Button _orderForStationButton = new();
    private readonly FlowLayoutPanel _cardsPanel = new();

    // Kitchen Queue section (Bottom)
    private readonly Panel _kitchenHeaderPanel = new();
    private readonly Label _kitchenTitleLabel = new();
    private readonly Label _kitchenBadgeLabel = new();
    private readonly Button _serveButton = new();
    private readonly Button _cancelOrderButton = new();
    private readonly Button _refreshKitchenButton = new();
    private readonly DataGridView _kitchenGrid = new();

    private List<KitchenOrderItem> _currentKitchenOrders = new();
    private List<ProductItem> _allProducts = new();
    private ProductItem? _selectedProduct;

    public OrdersView()
    {
        InitializeComponent();
        BuildLayout();
    }

    private void BuildLayout()
    {
        BackColor = BackgroundColor;
        Dock = DockStyle.Fill;
        Padding = new Padding(12);

        // Main Toolbar
        _toolbar.Dock = DockStyle.Top;
        _toolbar.Height = 56;
        _toolbar.BackColor = CyberTheme.Surface;
        _toolbar.Padding = new Padding(12, 6, 12, 6);

        _titleLabel.Text = "🍽️ Gọi món";
        _titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _titleLabel.ForeColor = CyberTheme.Text;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(12, 8);

        _subtitleLabel.Text = "Thực đơn món ăn và danh sách đơn khách đặt từ máy trạm chờ bếp chế biến.";
        _subtitleLabel.Font = new Font("Segoe UI", 8.5F);
        _subtitleLabel.ForeColor = CyberTheme.MutedText;
        _subtitleLabel.AutoSize = true;
        _subtitleLabel.Location = new Point(13, 31);

        _refreshAllButton.Text = "🔄 Làm mới tất cả";
        _refreshAllButton.Dock = DockStyle.Right;
        _refreshAllButton.Width = 140;
        CyberTheme.StyleButton(_refreshAllButton, PrimaryColor);
        CyberTheme.FitToolbarButton(_refreshAllButton, 140);
        _refreshAllButton.Click += async (_, _) => await RefreshAsync();

        _toolbar.Controls.Add(_refreshAllButton);
        _toolbar.Controls.Add(_titleLabel);
        _toolbar.Controls.Add(_subtitleLabel);

        // Split Container
        _splitContainer.Dock = DockStyle.Fill;
        _splitContainer.Orientation = Orientation.Horizontal;
        _splitContainer.SplitterDistance = 320;
        _splitContainer.SplitterWidth = 6;
        _splitContainer.BackColor = CyberTheme.SurfaceRaised;

        // --- Panel 1: Menu Thực đơn ---
        _splitContainer.Panel1.BackColor = BackgroundColor;
        _splitContainer.Panel1.Padding = new Padding(0, 8, 0, 4);

        _menuHeaderPanel.Dock = DockStyle.Top;
        _menuHeaderPanel.Height = 36;
        _menuHeaderPanel.BackColor = CyberTheme.Surface;
        _menuHeaderPanel.Padding = new Padding(10, 6, 10, 6);

        _menuTitleLabel.Text = "📋 Thực đơn & Tồn kho";
        _menuTitleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _menuTitleLabel.ForeColor = CyberTheme.Text;
        _menuTitleLabel.Dock = DockStyle.Left;
        _menuTitleLabel.AutoSize = true;

        _menuCountLabel.Text = "0 sản phẩm";
        _menuCountLabel.Font = new Font("Segoe UI", 9F);
        _menuCountLabel.ForeColor = CyberTheme.MutedText;
        _menuCountLabel.Dock = DockStyle.Right;
        _menuCountLabel.AutoSize = true;

        _categoryCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _categoryCombo.Width = 125;
        _categoryCombo.Items.AddRange(new object[] { "Tất cả", "Đồ uống", "Món ăn", "Thẻ game" });
        _categoryCombo.SelectedIndex = 0;
        _categoryCombo.Dock = DockStyle.Right;
        _categoryCombo.SelectedIndexChanged += (_, _) => ApplyProductFilter();

        _receiveStockButton.Text = "Nhập hàng";
        _receiveStockButton.Width = 98;
        _receiveStockButton.Height = 26;
        _receiveStockButton.Dock = DockStyle.Right;
        CyberTheme.StyleButton(_receiveStockButton, CyberTheme.Success);
        CyberTheme.FitToolbarButton(_receiveStockButton, 98);
        _receiveStockButton.Click += async (_, _) => await ReceiveStockAsync();

        _orderForStationButton.Text = "🛎️ Gọi món cho máy";
        _orderForStationButton.Width = 145;
        _orderForStationButton.Height = 26;
        _orderForStationButton.Dock = DockStyle.Right;
        CyberTheme.StyleButton(_orderForStationButton, CyberTheme.Primary);
        CyberTheme.FitToolbarButton(_orderForStationButton, 145);
        _orderForStationButton.Click += async (_, _) => await OrderForStationAsync();

        _menuHeaderPanel.Controls.Add(_orderForStationButton);

        _menuHeaderPanel.Controls.Add(_receiveStockButton);
        _menuHeaderPanel.Controls.Add(_categoryCombo);
        _menuHeaderPanel.Controls.Add(_menuTitleLabel);
        _menuHeaderPanel.Controls.Add(_menuCountLabel);

        _cardsPanel.Dock = DockStyle.Fill;
        _cardsPanel.AutoScroll = true;
        _cardsPanel.WrapContents = true;
        _cardsPanel.FlowDirection = FlowDirection.LeftToRight;
        _cardsPanel.Padding = new Padding(6, 8, 6, 8);
        _cardsPanel.BackColor = BackgroundColor;
        _cardsPanel.Resize += (_, _) => ResizeCards();

        _splitContainer.Panel1.Controls.Add(_cardsPanel);
        _splitContainer.Panel1.Controls.Add(_menuHeaderPanel);

        // --- Panel 2: Hàng đợi Bếp (Kitchen Order Queue) ---
        _splitContainer.Panel2.BackColor = BackgroundColor;
        _splitContainer.Panel2.Padding = new Padding(0, 4, 0, 0);

        _kitchenHeaderPanel.Dock = DockStyle.Top;
        _kitchenHeaderPanel.Height = 44;
        _kitchenHeaderPanel.BackColor = CyberTheme.Surface;
        _kitchenHeaderPanel.Padding = new Padding(10, 6, 10, 6);

        _kitchenTitleLabel.Text = "👨‍🍳 Đơn món ăn khách đặt (Bếp chế biến) — Đơn lưu vào phiên, tính tiền khi đóng máy";
        _kitchenTitleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _kitchenTitleLabel.ForeColor = CyberTheme.Text;
        _kitchenTitleLabel.Dock = DockStyle.Left;
        _kitchenTitleLabel.AutoSize = true;

        _kitchenBadgeLabel.Text = "0 đơn chờ";
        _kitchenBadgeLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _kitchenBadgeLabel.ForeColor = CyberTheme.Warning;
        _kitchenBadgeLabel.Dock = DockStyle.Left;
        _kitchenBadgeLabel.Padding = new Padding(12, 0, 0, 0);
        _kitchenBadgeLabel.AutoSize = true;

        // Action buttons
        _refreshKitchenButton.Text = "🔄 Làm mới bếp";
        _refreshKitchenButton.Width = 115;
        _refreshKitchenButton.Dock = DockStyle.Right;
        CyberTheme.StyleButton(_refreshKitchenButton, CyberTheme.SurfaceRaised);
        CyberTheme.FitToolbarButton(_refreshKitchenButton, 115);
        _refreshKitchenButton.Click += async (_, _) => await RefreshKitchenOrdersAsync();

        _cancelOrderButton.Text = "✕ Hủy đơn";
        _cancelOrderButton.Width = 95;
        _cancelOrderButton.Dock = DockStyle.Right;
        CyberTheme.StyleButton(_cancelOrderButton, CyberTheme.Danger);
        CyberTheme.FitToolbarButton(_cancelOrderButton, 95);
        _cancelOrderButton.Click += async (_, _) => await CancelSelectedOrderAsync();

        _serveButton.Text = "✓ Đã làm xong & Phục vụ";
        _serveButton.Width = 175;
        _serveButton.Dock = DockStyle.Right;
        CyberTheme.StyleButton(_serveButton, CyberTheme.Success);
        CyberTheme.FitToolbarButton(_serveButton, 175);
        _serveButton.Click += async (_, _) => await ServeSelectedOrderAsync();

        _kitchenHeaderPanel.Controls.Add(_kitchenTitleLabel);
        _kitchenHeaderPanel.Controls.Add(_kitchenBadgeLabel);
        _kitchenHeaderPanel.Controls.Add(_serveButton);
        _kitchenHeaderPanel.Controls.Add(_cancelOrderButton);
        _kitchenHeaderPanel.Controls.Add(_refreshKitchenButton);

        // Setup Kitchen DataGridView
        StyleGrid(_kitchenGrid);
        _kitchenGrid.CellFormatting += KitchenGrid_CellFormatting;

        _splitContainer.Panel2.Controls.Add(_kitchenGrid);
        _splitContainer.Panel2.Controls.Add(_kitchenHeaderPanel);

        Controls.Add(_splitContainer);
        Controls.Add(_toolbar);
    }

    public override void SetContext(AdminViewContext context)
    {
        base.SetContext(context);
        UpdateRolePermissions(context);
    }

    private void UpdateRolePermissions(AdminViewContext context)
    {
        if (context.IsAdmin || context.IsManager)
        {
            _receiveStockButton.Enabled = true;
            _orderForStationButton.Enabled = true;
            _serveButton.Enabled = true;
            _cancelOrderButton.Enabled = true;
        }
        else if (context.IsCashier)
        {
            _receiveStockButton.Enabled = false;
            _orderForStationButton.Enabled = true;
            _serveButton.Enabled = true;
            _cancelOrderButton.Enabled = true;
        }
        else
        {
            _receiveStockButton.Enabled = false;
            _orderForStationButton.Enabled = false;
            _serveButton.Enabled = false;
            _cancelOrderButton.Enabled = false;
            ClearForbiddenState();
        }
    }

    private void ClearForbiddenState()
    {
        _cardsPanel.SuspendLayout();
        _cardsPanel.Controls.Clear();
        var emptyLabel = new Label
        {
            Text = "🔒 Role hiện tại không có quyền truy cập Gọi món.",
            ForeColor = CyberTheme.MutedText,
            Font = new Font("Segoe UI", 10F, FontStyle.Italic),
            AutoSize = true,
            Padding = new Padding(16, 24, 16, 16)
        };
        _cardsPanel.Controls.Add(emptyLabel);
        _cardsPanel.ResumeLayout();
        _kitchenGrid.DataSource = null;
        _menuCountLabel.Text = "Không có quyền";
        _kitchenBadgeLabel.Text = "Bị khóa";
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (RequiredContext.UserRole == "role_unknown")
        {
            ClearForbiddenState();
            return;
        }

        try
        {
            var products = await RequiredContext.Repository.GetProductsAsync();
            if (products is not null && products.Count > 0)
            {
                _allProducts = products;
                ApplyProductFilter();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }

        try
        {
            var kitchenOrders = await RequiredContext.Repository.GetKitchenOrdersAsync(cancellationToken);
            if (kitchenOrders is not null)
            {
                PopulateKitchenOrders(kitchenOrders);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private async Task RefreshKitchenOrdersAsync()
    {
        try
        {
            var orders = await RequiredContext.Repository.GetKitchenOrdersAsync();
            PopulateKitchenOrders(orders);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void ApplyProductFilter()
    {
        var selectedCategory = _categoryCombo.SelectedItem?.ToString();
        var products = string.IsNullOrWhiteSpace(selectedCategory) || selectedCategory == "Tất cả"
            ? _allProducts
            : _allProducts.Where(product => MatchesCategory(product.CategoryName, selectedCategory)).ToList();
        PopulateProducts(products);
    }

    private static bool MatchesCategory(string categoryName, string displayCategory)
    {
        return displayCategory switch
        {
            "Đồ uống" => categoryName.Contains("Uống", StringComparison.OrdinalIgnoreCase),
            "Món ăn" => categoryName.Contains("Ăn", StringComparison.OrdinalIgnoreCase),
            "Thẻ game" => categoryName.Contains("Thẻ", StringComparison.OrdinalIgnoreCase),
            _ => string.Equals(categoryName, displayCategory, StringComparison.OrdinalIgnoreCase)
        };
    }

    private void PopulateProducts(IReadOnlyList<ProductItem> products)
    {
        _selectedProduct = null;
        _cardsPanel.SuspendLayout();
        _cardsPanel.Controls.Clear();

        foreach (var product in products)
        {
            var card = CreateProductCard(product);
            _cardsPanel.Controls.Add(card);
        }

        _cardsPanel.ResumeLayout();
        ResizeCards();
        _menuCountLabel.Text = $"{products.Count} món trong thực đơn";
    }

    private Panel CreateProductCard(ProductItem product)
    {
        var isAvailable = product.StockQty > 0;
        var accentColor = isAvailable ? CyberTheme.Success : CyberTheme.Danger;

        var card = new Panel
        {
            Width = 270,
            Height = 238,
            Margin = new Padding(0, 0, 10, 10),
            Padding = new Padding(12),
            BackColor = CyberTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle
        };

        var accent = new Panel
        {
            Dock = DockStyle.Left,
            Width = 5,
            BackColor = accentColor
        };

        var image = new PictureBox
        {
            Dock = DockStyle.Top,
            Height = 98,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = CyberTheme.Surface,
            Image = AdminProductImageCatalog.Load(product.Sku)
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = product.ProductName,
            ForeColor = CyberTheme.Text,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            AutoEllipsis = true
        };

        var category = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = $"SKU: {product.Sku} · {product.CategoryName}",
            ForeColor = CyberTheme.MutedText,
            Font = new Font("Segoe UI", 8.5F),
            AutoEllipsis = true
        };

        var status = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = isAvailable ? "● Đang kinh doanh" : "○ Hết hàng",
            ForeColor = accentColor,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            AutoEllipsis = true
        };

        var priceStock = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = $"Giá: {product.UnitPrice:N0} đ | Tồn: {product.StockQty:N0} phần",
            ForeColor = CyberTheme.Primary,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        var orderBtn = new Button
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            Text = isAvailable ? "🛎️ Đặt món" : "Hết hàng",
            Enabled = isAvailable,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
        };
        CyberTheme.StyleButton(orderBtn, isAvailable ? CyberTheme.Primary : CyberTheme.SurfaceRaised);
        orderBtn.Click += async (_, _) =>
        {
            _selectedProduct = product;
            await OrderForStationAsync();
        };

        card.Tag = product;
        card.Controls.Add(orderBtn);
        card.Controls.Add(priceStock);
        card.Controls.Add(status);
        card.Controls.Add(category);
        card.Controls.Add(title);
        card.Controls.Add(image);
        card.Controls.Add(accent);

        WireProductCardClick(card);

        return card;
    }

    private void WireProductCardClick(Control control)
    {
        control.Click += (_, _) => SelectProduct(control);
        foreach (Control child in control.Controls)
        {
            WireProductCardClick(child);
        }
    }

    private void SelectProduct(Control control)
    {
        var card = control;
        while (card.Parent is not null && !ReferenceEquals(card.Parent, _cardsPanel))
        {
            card = card.Parent;
        }

        _selectedProduct = card.Tag as ProductItem;
        foreach (Control candidate in _cardsPanel.Controls)
        {
            candidate.BackColor = ReferenceEquals(candidate, card)
                ? CyberTheme.SurfaceRaised
                : CyberTheme.Surface;
        }
    }

    private async Task ReceiveStockAsync()
    {
        if (!RequiredContext.IsAdmin && !RequiredContext.IsManager)
        {
            MessageBox.Show(
                FindForm(),
                "Chỉ Chủ quán và Quản lý mới có quyền quản lý menu và nhập kho hàng.",
                "Từ chối quyền hạn",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (_selectedProduct is null)
        {
            MessageBox.Show(FindForm(), "Hãy chọn một card sản phẩm trước khi nhập hàng.", "Chưa chọn sản phẩm", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (RequiredContext.CurrentEmployeeId <= 0)
        {
            ReportError(new InvalidOperationException("Vai trò hiện tại chưa ánh xạ employee_id."));
            return;
        }

        using var dialog = new StockReceiveDialog(_selectedProduct.ProductName);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var stock = await RequiredContext.Repository.ReceiveStockAsync(_selectedProduct.ProductId, dialog.Quantity);
            MessageBox.Show(FindForm(), $"Đã nhập {dialog.Quantity:N0} sản phẩm. Tồn kho mới: {stock:N0}.", "Nhập hàng thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task OrderForStationAsync()
    {
        if (_selectedProduct is null)
        {
            MessageBox.Show(FindForm(), "Vui lòng chọn một món ăn/nước uống trong thực đơn trước.", "Chưa chọn món", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_selectedProduct.StockQty <= 0)
        {
            MessageBox.Show(FindForm(), $"Món '{_selectedProduct.ProductName}' đã hết hàng trong kho.", "Hết hàng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var computers = await RequiredContext.Repository.GetComputersAsync();
            var activeStations = computers
                .Where(c => c.ComputerStatus.Equals("in_use", StringComparison.OrdinalIgnoreCase) && c.CurrentSessionId.HasValue)
                .ToList();

            if (activeStations.Count == 0)
            {
                MessageBox.Show(FindForm(), "Hiện không có máy trạm nào đang mở phiên chơi để gọi món.", "Không có máy đang hoạt động", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new StationOrderDialog(_selectedProduct, activeStations);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.SelectedStation == null)
            {
                return;
            }

            var sessionId = dialog.SelectedStation.CurrentSessionId!.Value;
            var quantity = dialog.Quantity;
            var empId = RequiredContext.CurrentEmployeeId;

            var orderId = await RequiredContext.Repository.PlaceOrderAsync(sessionId, empId, _selectedProduct.ProductId, quantity);
            MessageBox.Show(FindForm(),
                $"Đã tạo đơn món #{orderId} cho {dialog.SelectedStation.ComputerCode} thành công!\nSố lượng: {quantity} x {_selectedProduct.ProductName}\nTổng tiền: {quantity * _selectedProduct.UnitPrice:N0} đ",
                "Đặt món thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void ResizeCards()
    {
        var availableWidth = _cardsPanel.ClientSize.Width - _cardsPanel.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
        if (availableWidth <= 0) return;

        var columns = Math.Max(1, availableWidth / 280);
        var targetWidth = (availableWidth / columns) - 10;

        foreach (Control control in _cardsPanel.Controls)
        {
            control.Width = targetWidth;
        }
    }

    private void PopulateKitchenOrders(List<KitchenOrderItem> orders)
    {
        _currentKitchenOrders = orders;
        _kitchenGrid.DataSource = null;
        _kitchenGrid.DataSource = orders;

        // Custom column headers
        if (_kitchenGrid.Columns["OrderId"] is { } colId) { colId.HeaderText = "Mã đơn"; colId.Width = 85; }
        if (_kitchenGrid.Columns["SessionId"] is { } colSession) { colSession.Visible = false; }
        if (_kitchenGrid.Columns["ComputerCode"] is { } colComp) { colComp.HeaderText = "Máy trạm"; colComp.Width = 110; }
        if (_kitchenGrid.Columns["CustomerName"] is { } colCust) { colCust.HeaderText = "Khách hàng"; colCust.Width = 140; }
        if (_kitchenGrid.Columns["ItemsSummary"] is { } colItems) { colItems.HeaderText = "Món ăn & Số lượng"; colItems.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }
        if (_kitchenGrid.Columns["TotalAmount"] is { } colTotal)
        {
            colTotal.HeaderText = "Tổng tiền";
            colTotal.Width = 110;

            colTotal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        if (_kitchenGrid.Columns["Status"] is { } colStatus) { colStatus.HeaderText = "Trạng thái"; colStatus.Width = 130; }
        if (_kitchenGrid.Columns["OrderedAt"] is { } colTime)
        {
            colTime.HeaderText = "Thời gian gọi";
            colTime.Width = 140;
            colTime.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        }

        // Count pending
        var pendingCount = orders.Count(o => o.Status.Equals("placed", StringComparison.OrdinalIgnoreCase));
        if (pendingCount > 0)
        {
            _kitchenBadgeLabel.Text = $"⚡ {pendingCount} đơn chờ chế biến";
            _kitchenBadgeLabel.ForeColor = CyberTheme.Warning;
        }
        else
        {
            _kitchenBadgeLabel.Text = "✓ Đã phục vụ hết";
            _kitchenBadgeLabel.ForeColor = CyberTheme.Success;
        }
    }

    private void KitchenGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _kitchenGrid.Rows.Count) return;

        var columnName = _kitchenGrid.Columns[e.ColumnIndex].Name;
        if (columnName.Equals("TotalAmount", StringComparison.OrdinalIgnoreCase) && e.Value is decimal totalAmount)
        {
            e.Value = $"{totalAmount:N0} đ";
            e.FormattingApplied = true;
            return;
        }
        if (columnName.Equals("Status", StringComparison.OrdinalIgnoreCase) && e.Value != null)
        {
            var rawStatus = e.Value.ToString()?.ToLowerInvariant() ?? "";
            switch (rawStatus)
            {
                case "placed":
                    e.Value = "⏳ Chờ chế biến";
                    e.CellStyle!.ForeColor = CyberTheme.Warning;
                    e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    break;
                case "served":
                    e.Value = "✓ Đã phục vụ";
                    e.CellStyle!.ForeColor = CyberTheme.Success;
                    break;
                case "cancelled":
                    e.Value = "✕ Đã hủy";
                    e.CellStyle!.ForeColor = CyberTheme.MutedText;
                    break;
            }
        }
    }

    private KitchenOrderItem? GetSelectedKitchenOrder()
    {
        if (_kitchenGrid.CurrentRow?.DataBoundItem is KitchenOrderItem item)
        {
            return item;
        }
        return null;
    }

    private async Task ServeSelectedOrderAsync()
    {
        var order = GetSelectedKitchenOrder();
        if (order == null)
        {
            MessageBox.Show("Vui lòng chọn một đơn món từ danh sách bếp.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (order.Status.Equals("served", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show($"Đơn món #{order.OrderId} đã được phục vụ trước đó.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (order.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show($"Đơn món #{order.OrderId} đã bị hủy, không thể phục vụ.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            await RequiredContext.Repository.UpdateFoodOrderStatusAsync(order.OrderId, "served", RequiredContext.CurrentEmployeeId);
            MessageBox.Show(
                $"Đã hoàn thành chế biến và phục vụ đơn #{order.OrderId} cho máy {order.ComputerCode}!\n\n" +
                $"Tiền món ({order.TotalAmount:N0} đ) đã được LƯU VÀO PHIÊN CHƠI và sẽ được chốt thanh toán 1 lần duy nhất khi máy hoặc tài khoản đó kết thúc/tắt máy.",
                "Phục vụ món thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            await RefreshKitchenOrdersAsync();
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private async Task CancelSelectedOrderAsync()
    {
        var order = GetSelectedKitchenOrder();
        if (order == null)
        {
            MessageBox.Show("Vui lòng chọn một đơn món từ danh sách bếp.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (order.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show($"Đơn món #{order.OrderId} đã bị hủy rồi.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn HỦY đơn món #{order.OrderId} của {order.ComputerCode} ({order.ItemsSummary}) không?",
            "Xác nhận hủy đơn",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await RequiredContext.Repository.UpdateFoodOrderStatusAsync(order.OrderId, "cancelled", RequiredContext.CurrentEmployeeId);
            MessageBox.Show($"Đã hủy đơn #{order.OrderId} thành công.", "Đã hủy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshKitchenOrdersAsync();
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }
}

internal sealed class StockReceiveDialog : Form
{
    private readonly NumericUpDown _quantityBox = new();
    public int Quantity => (int)_quantityBox.Value;

    public StockReceiveDialog(string productName)
    {
        Text = $"Nhập hàng - {productName}";
        Width = 340;
        Height = 170;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        layout.Controls.Add(new Label { Text = "Số lượng nhập", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _quantityBox.Minimum = 1;
        _quantityBox.Maximum = 100000;
        _quantityBox.Value = 10;
        _quantityBox.Dock = DockStyle.Fill;
        layout.Controls.Add(_quantityBox, 1, 0);
        var save = new Button { Text = "Xác nhận", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        CyberTheme.StyleButton(save, CyberTheme.Success);
        layout.Controls.Add(save, 1, 1);
        Controls.Add(layout);
        AcceptButton = save;
    }
}


internal sealed class StationOrderDialog : Form
{
    private readonly ComboBox _stationCombo = new();
    private readonly NumericUpDown _qtyBox = new();
    private readonly Label _totalLabel = new();
    private readonly decimal _unitPrice;

    public ComputerGridItem? SelectedStation { get; private set; }
    public int Quantity => (int)_qtyBox.Value;

    public StationOrderDialog(ProductItem product, IReadOnlyList<ComputerGridItem> activeStations)
    {
        _unitPrice = product.UnitPrice;
        Text = $"Gọi món cho máy - {product.ProductName}";
        StartPosition = FormStartPosition.CenterParent;
        Width = 440;
        Height = 310;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var infoLabel = new Label
        {
            Text = $"Món: {product.ProductName} | Đơn giá: {product.UnitPrice:N0} đ | Tồn kho: {product.StockQty:N0}",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = CyberTheme.Primary,
            Dock = DockStyle.Fill
        };

        var stationPanel = new Panel { Dock = DockStyle.Fill };
        var stationLbl = new Label { Text = "Máy trạm nhận món:", Dock = DockStyle.Top, Height = 18, ForeColor = CyberTheme.Text };
        _stationCombo.Dock = DockStyle.Bottom;
        _stationCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (var s in activeStations)
        {
            _stationCombo.Items.Add(new StationOption(s));
        }
        if (_stationCombo.Items.Count > 0) _stationCombo.SelectedIndex = 0;
        stationPanel.Controls.Add(stationLbl);
        stationPanel.Controls.Add(_stationCombo);

        var qtyPanel = new Panel { Dock = DockStyle.Fill };
        var qtyLbl = new Label { Text = "Số lượng món:", Dock = DockStyle.Top, Height = 18, ForeColor = CyberTheme.Text };
        _qtyBox.Dock = DockStyle.Bottom;
        _qtyBox.Minimum = 1;
        _qtyBox.Maximum = Math.Max(1, product.StockQty);
        _qtyBox.Value = 1;
        _qtyBox.ValueChanged += (_, _) => UpdateTotal();
        qtyPanel.Controls.Add(qtyLbl);
        qtyPanel.Controls.Add(_qtyBox);

        _totalLabel.Text = $"Thành tiền: {_unitPrice:N0} đ";
        _totalLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _totalLabel.ForeColor = CyberTheme.Success;
        _totalLabel.Dock = DockStyle.Fill;

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            FlowDirection = FlowDirection.RightToLeft
        };
        var okBtn = new Button
        {
            Text = "Xác nhận đặt",
            DialogResult = DialogResult.OK,
            Width = 120,
            Height = 36
        };
        CyberTheme.StyleButton(okBtn, CyberTheme.Success);
        okBtn.Click += (_, _) =>
        {
            if (_stationCombo.SelectedItem is StationOption option)
            {
                SelectedStation = option.Station;
            }
        };

        var cancelBtn = new Button
        {
            Text = "Hủy",
            DialogResult = DialogResult.Cancel,
            Width = 80,
            Height = 36
        };
        CyberTheme.StyleButton(cancelBtn, CyberTheme.SurfaceRaised);
        btnPanel.Controls.Add(cancelBtn);
        btnPanel.Controls.Add(okBtn);

        root.Controls.Add(infoLabel, 0, 0);
        root.Controls.Add(stationPanel, 0, 1);
        root.Controls.Add(qtyPanel, 0, 2);
        root.Controls.Add(_totalLabel, 0, 3);
        root.Controls.Add(btnPanel, 0, 4);
        Controls.Add(root);
        AcceptButton = okBtn;
        CancelButton = cancelBtn;
    }

    private void UpdateTotal()
    {
        var total = _qtyBox.Value * _unitPrice;
        _totalLabel.Text = $"Thành tiền: {total:N0} đ";
    }

    private sealed class StationOption
    {
        public ComputerGridItem Station { get; }
        public StationOption(ComputerGridItem station) => Station = station;
        public override string ToString() => $"{Station.ComputerCode} - {Station.CurrentCustomerName} (Phiên #{Station.CurrentSessionId})";
    }
}
