// Trách nhiệm: màn hình bảo trì và trạng thái máy; dữ liệu chi tiết sẽ mở rộng theo service contract.

namespace NetCafe.App.Views;

public partial class MaintenanceView : AdminCardViewBase
{
    public MaintenanceView()
    {
        InitializeComponent();
        SetTitle("Bảo trì", "Status machine được đồng bộ bởi hr.fn_trg_sync_computer_maintenance.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetComputersAsync();
        var maintenanceRows = rows.Where(row => row.ComputerStatus is "maintenance" or "broken").ToList();
        SetCards(
            maintenanceRows,
            computer => new AdminCardModel(
                $"{computer.ComputerCode} · {computer.ComputerName}",
                computer.RoomName,
                $"IP: {computer.IpAddress}\nHeartbeat: {computer.LastHeartbeatAt?.ToLocalTime():dd/MM HH:mm:ss}",
                computer.ComputerStatus,
                computer.ComputerStatus == "broken" ? CyberTheme.Danger : CyberTheme.Warning),
            $"{maintenanceRows.Count} máy đang bảo trì hoặc hỏng");
    }
}
