// Trách nhiệm: menu món ăn, tồn kho F&B và hàng đợi bếp làm đơn đặt từ máy trạm.
// Liên kết: inventory.product, inventory.food_order và sp_place_order.

using System.Drawing;
using System.Windows.Forms;
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

        _titleLabel.Text = "🍽️ Đặt món & Bếp F&B";
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

        _kitchenTitleLabel.Text = "👨‍🍳 Đơn món ăn khách đặt (Bếp chế biến)";
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
        _refreshKitchenButton.Click += async (_, _) => await RefreshKitchenOrdersAsync();

        _cancelOrderButton.Text = "✕ Hủy đơn";
        _cancelOrderButton.Width = 95;
        _cancelOrderButton.Dock = DockStyle.Right;
        CyberTheme.StyleButton(_cancelOrderButton, CyberTheme.Danger);
        _cancelOrderButton.Click += async (_, _) => await CancelSelectedOrderAsync();

        _serveButton.Text = "✓ Đã làm xong & Phục vụ";
        _serveButton.Width = 175;
        _serveButton.Dock = DockStyle.Right;
        CyberTheme.StyleButton(_serveButton, CyberTheme.Success);
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

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var productsTask = RequiredContext.Repository.GetProductsAsync();
            var kitchenOrdersTask = RequiredContext.Repository.GetKitchenOrdersAsync(cancellationToken);

            await Task.WhenAll(productsTask, kitchenOrdersTask);

            PopulateProducts(productsTask.Result);
            PopulateKitchenOrders(kitchenOrdersTask.Result);
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

    private void PopulateProducts(List<ProductItem> products)
    {
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
            Height = 135,
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
            Dock = DockStyle.Fill,
            Text = $"Giá: {product.UnitPrice:N0} đ\nTồn kho: {product.StockQty:N0} phần",
            ForeColor = CyberTheme.Primary,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        card.Controls.Add(priceStock);
        card.Controls.Add(status);
        card.Controls.Add(category);
        card.Controls.Add(title);
        card.Controls.Add(accent);

        return card;
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
            colTotal.DefaultCellStyle.Format = "N0 đ";
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
            MessageBox.Show($"Đã hoàn thành chế biến và phục vụ đơn #{order.OrderId} ({order.ComputerCode})!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
