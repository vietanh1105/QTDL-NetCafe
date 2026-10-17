// Trách nhiệm: điều phối Login → Session → FoodOrder/Support, không chứa SQL trực tiếp.
// Liên kết: StationSessionController.cs, Views/*.cs và sql/08_station_customer.sql.

using NetCafe.Client.Views;
using NetCafe.Data;

namespace NetCafe.Client;

public partial class MainForm : Form
{
    private readonly StationConfig _config;
    private readonly DbRepository _repository;
    private readonly StationSessionController _sessionController;
    private readonly LoginView _loginView = new();
    private readonly SessionView _sessionView = new();
    private readonly FoodOrderView _foodOrderView = new();
    private readonly SupportRequestView _supportRequestView = new();
    private StationLoginResult? _session;

    public MainForm(StationConfig config)
    {
        _config = config;
        _repository = new DbRepository(config.ConnectionString);
        _sessionController = new StationSessionController(_repository, config);
        InitializeComponent();
        BuildViews();
    }

    private void BuildViews()
    {
        _contentPanel.Controls.AddRange(new Control[] { _loginView, _sessionView, _foodOrderView, _supportRequestView });
        _loginView.LoginRequested += LoginAsync;
        _sessionView.LogoutRequested += LogoutAsync;
        _sessionView.FoodOrderRequested += async () => await ShowFoodOrderAsync();
        _sessionView.SupportRequested += () => ShowView(_supportRequestView);
        _foodOrderView.BackRequested += () => ShowView(_sessionView);
        _supportRequestView.BackRequested += () => ShowView(_sessionView);
        _foodOrderView.OrderRequested += PlaceOrderAsync;
        _foodOrderView.OrdersRefreshRequested += RefreshFoodOrdersAsync;
        _supportRequestView.RequestSubmitted += CreateSupportRequestAsync;
        _sessionController.HeartbeatReceived += result => BeginInvoke(() => HandleHeartbeat(result));
        _sessionController.HeartbeatFailed += exception => BeginInvoke(() => _sessionView.SetHeartbeat(new StationHeartbeatResult
        {
            SessionId = _session?.SessionId ?? 0,
            BalanceRemaining = 0,
            LastHeartbeatAt = DateTimeOffset.Now,
            SessionStatus = $"Lỗi heartbeat: {exception.Message}"
        }));
        ShowView(_loginView);
        _stationLabel.Text = $"Station: {_config.ComputerCode}";
    }

    private async Task LoginAsync(string username, string password)
    {
        try
        {
            _loginView.ShowError(string.Empty);
            _session = await _repository.CustomerLoginAsync(username, password, _config.ComputerCode);
            _sessionView.SetSession(_session);
            _sessionController.Start(_session);
            _loginView.ClearPassword();
            ShowView(_sessionView);
        }
        catch (Exception exception)
        {
            _loginView.ShowError(DbRepository.ParseException(exception).Message);
        }
    }

    private void HandleHeartbeat(StationHeartbeatResult result)
    {
        _sessionView.SetHeartbeat(result);
        if (result.SessionStatus == "finished")
        {
            _sessionController.Stop();
            _session = null;
            ShowView(_loginView);
            _loginView.ShowError("Phiên đã tự đóng vì số dư không còn đủ.");
        }
    }

    private async Task LogoutAsync()
    {
        if (_session is null)
        {
            ShowView(_loginView);
            return;
        }
        if (!LogoutView.Confirm(this))
        {
            return;
        }
        try
        {
            var invoiceId = await _repository.StationLogoutAsync(_session.SessionId, _session.CustomerId, _session.ComputerId);
            _sessionController.Stop();
            _session = null;
            ShowView(_loginView);
            _loginView.ShowError($"Đã đóng phiên và tạo hóa đơn #{invoiceId}.");
        }
        catch (Exception exception)
        {
            _sessionView.SetHeartbeat(new StationHeartbeatResult
            {
                BalanceRemaining = 0,
                LastHeartbeatAt = DateTimeOffset.Now,
                SessionStatus = DbRepository.ParseException(exception).Message
            });
        }
    }

    private async Task ShowFoodOrderAsync()
    {
        try
        {
            var menu = await _repository.GetCustomerMenuAsync();
            _foodOrderView.SetMenu(menu);
            await RefreshFoodOrdersAsync();
            ShowView(_foodOrderView);
        }
        catch (Exception exception)
        {
            _sessionView.SetHeartbeat(new StationHeartbeatResult
            {
                BalanceRemaining = 0,
                LastHeartbeatAt = DateTimeOffset.Now,
                SessionStatus = DbRepository.ParseException(exception).Message
            });
        }
    }

    private async Task PlaceOrderAsync(int productId, int quantity)
    {
        if (_session is null)
        {
            return;
        }
        try
        {
            var orderId = await _repository.CustomerPlaceOrderAsync(_session.SessionId, _session.CustomerId, productId, quantity);
            _foodOrderView.ShowMessage($"Đã gửi đơn #{orderId}; tồn kho và ví được database kiểm tra.");
            await RefreshFoodOrdersAsync();
        }
        catch (Exception exception)
        {
            _foodOrderView.ShowMessage(DbRepository.ParseException(exception).Message);
        }
    }

    private async Task RefreshFoodOrdersAsync()
    {
        if (_session is null)
        {
            return;
        }

        var orders = await _repository.GetCustomerOrderStatusAsync(_session.SessionId, _session.CustomerId);
        _foodOrderView.SetOrders(orders);
    }

    private async Task CreateSupportRequestAsync(string type, string message)
    {
        if (_session is null)
        {
            return;
        }
        try
        {
            var requestId = await _repository.CreateServiceRequestAsync(
                _session.SessionId, _session.CustomerId, _session.ComputerId, type, message);
            _supportRequestView.ShowMessage($"Đã gửi yêu cầu hỗ trợ #{requestId}.");
        }
        catch (Exception exception)
        {
            _supportRequestView.ShowMessage(DbRepository.ParseException(exception).Message);
        }
    }

    private void ShowView(Control view)
    {
        foreach (Control control in _contentPanel.Controls)
        {
            control.Visible = ReferenceEquals(control, view);
        }
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        await _sessionController.DisposeAsync();
        base.OnFormClosing(e);
    }
}
