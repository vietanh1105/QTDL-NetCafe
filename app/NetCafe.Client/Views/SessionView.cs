// Trách nhiệm: hiển thị session, chi phí dự kiến, ví và thao tác logout.
// Heartbeat do StationSessionController điều phối, không nhân dt tại UI.

namespace NetCafe.Client.Views;

public partial class SessionView : UserControl
{
    public event Func<Task>? LogoutRequested;
    public event Action? FoodOrderRequested;
    public event Action? SupportRequested;

    public SessionView()
    {
        InitializeComponent();
        _logoutButton.Click += async (_, _) =>
        {
            if (LogoutRequested is null)
            {
                return;
            }

            _logoutButton.Enabled = false;
            try
            {
                await LogoutRequested();
            }
            finally
            {
                _logoutButton.Enabled = true;
            }
        };
        _foodButton.Click += (_, _) => FoodOrderRequested?.Invoke();
        _supportButton.Click += (_, _) => SupportRequested?.Invoke();
    }

    public void SetSession(NetCafe.Data.StationLoginResult session)
    {
        _customerLabel.Text = $"Khách hàng: {session.CustomerName} | Máy: {session.ComputerCode}";
        _sessionLabel.Text = $"Phiên #{session.SessionId} | Giá giờ: {session.HourlyRate:N0} đ";
        _balanceLabel.Text = $"Số dư: {session.Balance:N0} đ";
    }

    public void SetHeartbeat(NetCafe.Data.StationHeartbeatResult result)
    {
        var statusText = result.SessionStatus.ToLowerInvariant() switch
        {
            "active" => "Đang hoạt động",
            "finished" => "Đã kết thúc",
            _ => result.SessionStatus
        };

        _balanceLabel.Text = $"Số dư: {result.BalanceRemaining:N0} đ";
        _costLabel.Text = $"Tạm tính: {result.ProjectedTotalAmount:N0} đ (Tiền giờ: {result.ProjectedPlayAmount:N0} đ + F&B: {result.ProjectedServiceAmount:N0} đ)";
        _statusLabel.Text = $"Tín hiệu: {result.LastHeartbeatAt:HH:mm:ss} | {statusText}";
    }
}
