// Trách nhiệm: menu và tồn kho F&B; liên kết inventory.product và sp_place_order.

namespace NetCafe.App.Views;

public partial class OrdersView : AdminCardViewBase
{
    public OrdersView()
    {
        InitializeComponent();
        SetTitle("Dịch vụ và F&B", "Stock chỉ thay đổi qua trigger database; không cập nhật tồn kho từ UI.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetProductsAsync();
        SetCards(
            rows,
            product => new AdminCardModel(
                product.ProductName,
                $"{product.Sku} · {product.CategoryName}",
                $"Giá: {product.UnitPrice:N0} đ\nTồn kho: {product.StockQty:N0}",
                product.StockQty > 0 ? "Đang bán" : "Hết hàng",
                product.StockQty > 0 ? CyberTheme.Success : CyberTheme.Danger),
            $"{rows.Count} sản phẩm đang bán");
    }
}
