// Trách nhiệm: hóa đơn và sổ thanh toán; liên kết billing.invoice/payment và RLS.

namespace NetCafe.App.Views;

public partial class InvoicesView : AdminDataViewBase
{
    public InvoicesView()
    {
        InitializeComponent();
        SetTitle("Hóa đơn và sổ quỹ", "Dữ liệu hiển thị theo role hiện tại; cashier bị RLS lọc theo cashier_id.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetRecentInvoicesAsync(100);
        SetRows(rows, $"{rows.Count} hóa đơn gần nhất");
    }
}
