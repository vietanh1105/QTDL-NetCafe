// Trách nhiệm: gọi món qua inventory.sp_customer_place_order; không đọc bảng trực tiếp.

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
        _orderButton.Click += async (_, _) =>
        {
            if (_productBox.SelectedItem is CustomerMenuItem product && int.TryParse(_quantityBox.Text, out var quantity) && OrderRequested is not null)
            {
                await OrderRequested(product.ProductId, quantity);
            }
        };
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
        _productBox.DataSource = menu.ToList();
        _productBox.DisplayMember = nameof(CustomerMenuItem.ProductName);
    }

    public void ShowMessage(string message)
    {
        _messageLabel.Text = message;
    }

    public void SetOrders(IReadOnlyList<CustomerOrderStatus> orders)
    {
        _ordersGrid.DataSource = orders
            .Select(order => new OrderRow(order.OrderId, order.Status, order.OrderedAt.LocalDateTime, order.TotalAmount))
            .ToList();
    }

    private sealed record OrderRow(long OrderId, string Status, DateTime OrderedAt, decimal TotalAmount);
}
