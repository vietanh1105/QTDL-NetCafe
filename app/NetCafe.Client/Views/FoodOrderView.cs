// Trách nhiệm: hiển thị menu hai cột và trạng thái đơn của Station.
// Liên kết: ProductCardControl, DbRepository và MainForm.

using System.Drawing;
using NetCafe.Client.Controls;
using NetCafe.Data;

namespace NetCafe.Client.Views;

public partial class FoodOrderView : UserControl
{
    public event Func<int, int, Task>? OrderRequested;
    public event Func<Task>? OrdersRefreshRequested;
    public event Action? BackRequested;

    public FoodOrderView()
    {
        InitializeComponent();
        _refreshOrdersButton.Click += async (_, _) =>
        {
            if (OrdersRefreshRequested is not null)
            {
                await OrdersRefreshRequested();
            }
        };
        _backButton.Click += (_, _) => BackRequested?.Invoke();
    }

    public void SetMenu(IReadOnlyList<CustomerMenuItem> menu)
    {
        _menuGrid.Controls.Clear();
        _menuGrid.RowStyles.Clear();
        _menuGrid.RowCount = Math.Max(1, (menu.Count + 1) / 2);
        for (var index = 0; index < menu.Count; index++)
        {
            var productCard = new ProductCardControl();
            productCard.Bind(menu[index]);
            productCard.Dock = DockStyle.Fill;
            productCard.Margin = new Padding(5, 4, 5, 8);
            productCard.OrderRequested += async (productId, quantity) =>
            {
                if (OrderRequested is not null)
                {
                    await OrderRequested(productId, quantity);
                }
            };
            var row = index / 2;
            var column = index % 2;
            _menuGrid.Controls.Add(productCard, column, row);
        }

        for (var row = 0; row < _menuGrid.RowCount; row++)
        {
            _menuGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        _menuGrid.PerformLayout();
    }

    public void ShowMessage(string message)
    {
        _messageLabel.Text = message;
    }

    public void SetOrders(IReadOnlyList<CustomerOrderStatus> orders)
    {
        _ordersPanel.Controls.Clear();
        foreach (var order in orders)
        {
            var cardWidth = Math.Max(
                280,
                _ordersPanel.ClientSize.Width
                - _ordersPanel.Padding.Horizontal
                - SystemInformation.VerticalScrollBarWidth);
            _ordersPanel.Controls.Add(CreateOrderCard(order, cardWidth));
        }

        _ordersPanel.PerformLayout();
    }

    private static Control CreateOrderCard(CustomerOrderStatus order, int width)
    {
        var status = order.Status.ToLowerInvariant();
        var statusText = status switch
        {
            "placed" => "Chờ chế biến",
            "served" => "Đã phục vụ",
            "cancelled" => "Đã hủy",
            _ => order.Status
        };
        var accent = status switch
        {
            "placed" => Color.FromArgb(234, 88, 12),
            "served" => Color.FromArgb(75, 85, 99),
            "cancelled" => Color.FromArgb(185, 28, 28),
            _ => Color.FromArgb(100, 116, 139)
        };
        var card = new Panel
        {
            Width = width,
            Height = 76,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = StationTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle
        };
        var strip = new Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = accent
        };
        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = $"Đơn #{order.OrderId}",
            ForeColor = StationTheme.Text,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        var statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = statusText,
            ForeColor = accent,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
        };
        var total = new Label
        {
            Dock = DockStyle.Right,
            Width = 116,
            Text = $"{order.TotalAmount:N0} đ",
            ForeColor = accent,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight
        };
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 7, 12, 7) };
        body.Controls.Add(total);
        body.Controls.Add(statusLabel);
        body.Controls.Add(title);
        card.Controls.Add(body);
        card.Controls.Add(strip);
        return card;
    }
}
