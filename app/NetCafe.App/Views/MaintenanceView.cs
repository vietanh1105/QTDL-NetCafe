// Trách nhiệm: màn hình bảo trì và trạng thái máy; dữ liệu chi tiết sẽ mở rộng theo service contract.

namespace NetCafe.App.Views;

public partial class MaintenanceView : AdminDataViewBase
{
    public MaintenanceView()
    {
        InitializeComponent();
        SetTitle("Bảo trì", "Status machine được đồng bộ bởi hr.fn_trg_sync_computer_maintenance.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetComputersAsync();
        SetRows(rows.Where(row => row.ComputerStatus is "maintenance" or "broken"), "Máy đang bảo trì hoặc hỏng");
    }
}
