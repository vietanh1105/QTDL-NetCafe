// Trách nhiệm: gọi món qua inventory.sp_customer_place_order; không đọc bảng trực tiếp.

using NetCafe.Data;

namespace NetCafe.Client.Views;

public partial class FoodOrderView : UserControl
{
    public event Func<int, int, Task>? OrderRequested;
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
}
