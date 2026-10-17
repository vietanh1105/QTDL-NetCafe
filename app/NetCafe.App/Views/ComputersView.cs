// Trách nhiệm: sơ đồ máy và trạng thái session Admin; liên kết vw_computer_grid và reconcile.

namespace NetCafe.App.Views;

public partial class ComputersView : AdminDataViewBase
{
    public ComputersView()
    {
        InitializeComponent();
        SetTitle("Sơ đồ máy trạm", "Heartbeat/reconcile được xử lý ở database; refresh chỉ chạy khi view đang hiện.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetComputersAsync();
        SetRows(rows, $"{rows.Count} máy | cập nhật {DateTime.Now:HH:mm:ss}");
    }
}
