// Trách nhiệm: menu và tồn kho F&B; liên kết inventory.product và sp_place_order.

namespace NetCafe.App.Views;

public partial class OrdersView : AdminCardViewBase
{
    public OrdersView()
    {
        InitializeComponent();
        SetTitle("Dịch vụ ăn uống (F&B)", "Tồn kho được trừ tự động qua trigger CSDL khi đặt món.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetProductsAsync();
        SetCards(
            rows,
            product => new AdminCardModel(
                product.ProductName,
                $"Mã SKU: {product.Sku} · {product.CategoryName}",
                $"Đơn giá: {product.UnitPrice:N0} đ\nTồn kho: {product.StockQty:N0}",
                product.StockQty > 0 ? "Đang kinh doanh" : "Hết hàng",
                product.StockQty > 0 ? CyberTheme.Success : CyberTheme.Danger),
            $"{rows.Count} sản phẩm trong thực đơn");
    }
}
