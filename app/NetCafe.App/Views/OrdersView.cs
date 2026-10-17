// Trách nhiệm: menu và tồn kho F&B; liên kết inventory.product và sp_place_order.

namespace NetCafe.App.Views;

public partial class OrdersView : AdminDataViewBase
{
    public OrdersView()
    {
        InitializeComponent();
        SetTitle("Dịch vụ và F&B", "Stock chỉ thay đổi qua trigger database; không cập nhật tồn kho từ UI.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetProductsAsync();
        SetRows(rows, $"{rows.Count} sản phẩm đang bán");
    }
}
