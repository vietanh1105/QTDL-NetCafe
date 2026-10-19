// Trách nhiệm: màn hình bảo trì và trạng thái máy; dữ liệu chi tiết sẽ mở rộng theo service contract.

namespace NetCafe.App.Views;

public partial class MaintenanceView : AdminCardViewBase
{
    public MaintenanceView()
    {
        InitializeComponent();
        SetTitle("Bảo trì thiết bị", "Trạng thái máy được đồng bộ tự động qua trigger CSDL.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetComputersAsync();
        var maintenanceRows = rows.Where(row => row.ComputerStatus is "maintenance" or "broken").ToList();
        SetCards(
            maintenanceRows,
            computer =>
            {
                var statusText = computer.ComputerStatus switch
                {
                    "maintenance" => "Đang bảo trì",
                    "broken" => "Hỏng hóc",
                    _ => computer.ComputerStatus
                };

                return new AdminCardModel(
                    $"{computer.ComputerCode} · {computer.ComputerName}",
                    $"Phòng: {computer.RoomName}",
                    $"Địa chỉ IP: {computer.IpAddress}\nTín hiệu cuối: {computer.LastHeartbeatAt?.ToLocalTime():dd/MM HH:mm:ss}",
                    statusText,
                    computer.ComputerStatus == "broken" ? CyberTheme.Danger : CyberTheme.Warning);
            },
            $"{maintenanceRows.Count} máy đang bảo trì hoặc hỏng");
    }
}
