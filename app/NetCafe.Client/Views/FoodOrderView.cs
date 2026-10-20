// Trách nhiệm: hiển thị menu thật theo hai cột và trạng thái đơn của Station.
// Liên kết: ProductCardControl, StationRepository và MainForm.

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
            _ordersPanel.Controls.Add(CreateOrderCard(order));
        }

        _ordersPanel.PerformLayout();
    }

    private static Control CreateOrderCard(CustomerOrderStatus order)
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
            Width = 380,
            Height = 64,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        var title = new Label
        {
            AutoSize = true,
            Location = new Point(12, 8),
            Text = $"Đơn #{order.OrderId}",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        var statusLabel = new Label
        {
            AutoSize = true,
            Location = new Point(12, 31),
            Text = statusText,
            ForeColor = accent
        };
        var total = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(250, 20),
            Text = $"{order.TotalAmount:N0} đ",
            ForeColor = accent,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        card.Controls.Add(title);
        card.Controls.Add(statusLabel);
        card.Controls.Add(total);
        return card;
    }
}
