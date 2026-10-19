// Trách nhiệm: shell điều phối navigation, role thật, listener và refresh Admin.
// Liên kết: MainForm.Designer.cs, Views/*.cs, AppConfig.cs và DbRepository.

using System.Drawing;
using NetCafe.App.Views;
using NetCafe.Data;

namespace NetCafe.App;

public partial class MainForm : Form
{
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly Dictionary<string, IAdminView> _views = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _viewLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _navigationButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _pendingNotificationChannels = new(StringComparer.OrdinalIgnoreCase);
    private DbRepository _repository = null!;
    private CancellationTokenSource _listenerCancellation = new();
    private AdminViewContext _viewContext = null!;
    private string _currentUser = string.Empty;
    private int _currentEmployeeId;
    private IAdminView? _activeView;
    private string _activeKey = string.Empty;
    private bool _loadingRole;
    private bool _reconcileAvailable;
    private bool _databaseContractReady;
    private string _runtimeSummary = "CSDL chưa kiểm tra";
    private string _runtimeContractStatus = string.Empty;
    private string _listenerStatus = "Chưa khởi động";
    private readonly System.Windows.Forms.Timer _notificationDebounceTimer = new() { Interval = 250 };

    public MainForm()
    {
        AppConfig.LoadEnv();
        _currentUser = AppConfig.Username;
        _currentEmployeeId = AppConfig.AdminEmployeeId;
        _repository = new DbRepository(AppConfig.GetConnectionString());
        InitializeComponent();
        InitializeShell();
        Shown += async (_, _) => await StartAsync();
    }

    private void InitializeShell()
    {
        _roleCombo.Items.AddRange(AppConfig.GetConfiguredRoles().Cast<object>().ToArray());
        if (_roleCombo.Items.Count > 0)
        {
            _roleCombo.SelectedItem = _roleCombo.Items.Contains(_currentUser) ? _currentUser : _roleCombo.Items[0];
        }
        _roleCombo.SelectedIndexChanged += async (_, _) =>
        {
            if (!_loadingRole && _roleCombo.SelectedItem is string user)
            {
                await SwitchRoleAsync(user);
            }
        };

        _refreshButton.Click += async (_, _) =>
        {
            if (!_databaseContractReady)
            {
                AppConfig.LoadEnv();
                _currentUser = AppConfig.Username;
                _currentEmployeeId = AppConfig.AdminEmployeeId;
                _repository = new DbRepository(AppConfig.GetConnectionString(_currentUser, AppConfig.GetConfiguredRolePassword(_currentUser)));
                await RefreshDatabaseStatusAsync();
                if (_databaseContractReady)
                {
                    _timer.Start();
                    await RestartListenerAsync();
                }
            }
            await RefreshVisibleAsync();
        };
        _dismissErrorButton.Click += (_, _) => _errorBanner.Visible = false;
        _timer.Tick += async (_, _) => await ReconcileAndRefreshAsync();
        _notificationDebounceTimer.Tick += async (_, _) =>
        {
            _notificationDebounceTimer.Stop();
            await RefreshNotificationViewsAsync();
        };
    }

    private void BuildViews()
    {
        AddNavigationSeparator("VẬN HÀNH");
        AddView("Computers", "Máy trạm", new ComputersView());
        AddView("Customers", "Hội viên", new CustomersView());
        AddView("Orders", "Đặt món", new OrdersView());
        AddView("Invoices", "Hóa đơn", new InvoicesView());
        AddNavigationSeparator("BÁO CÁO VÀ NHÂN SỰ");
        AddView("Reports", "Báo cáo", new ReportsView());
        AddView("Employees", "Nhân viên", new EmployeesView());
        AddView("Maintenance", "Bảo trì", new MaintenanceView());
        AddNavigationSeparator("HỖ TRỢ VÀ HỆ THỐNG");
        AddView("AiCenter", "Trung tâm AI", new AiCenterView());
        AddView("SupportQueue", "Hàng đợi hỗ trợ", new SupportQueueView());
        AddView("Settings", "Cài đặt", new SettingsView());
        ApplyContext();
        SwitchView("Computers");
    }

    private void AddView(string key, string label, IAdminView view)
    {
        _views.Add(key, view);
        _viewLabels[key] = label;
        if (view is not Control control)
        {
            throw new InvalidOperationException($"View {key} không phải Control.");
        }

        control.Visible = false;
        control.Dock = DockStyle.Fill;
        _contentPanel.Controls.Add(control);
        var button = new Button
        {
            Text = label,
            Width = 190,
            Height = 38,
            Margin = new Padding(4),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(51, 65, 85),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        _navigationButtons[key] = button;
        button.Click += async (_, _) =>
        {
            SwitchView(key);
            await RefreshVisibleAsync();
        };
        button.MouseEnter += (_, _) =>
        {
            if (!string.Equals(_activeKey, key, StringComparison.OrdinalIgnoreCase))
            {
                button.BackColor = Color.FromArgb(239, 246, 255);
            }
        };
        button.MouseLeave += (_, _) =>
        {
            if (!string.Equals(_activeKey, key, StringComparison.OrdinalIgnoreCase))
            {
                button.BackColor = Color.White;
            }
        };
        _navigationPanel.Controls.Add(button);
    }

    private void AddNavigationSeparator(string title)
    {
        _navigationPanel.Controls.Add(new Label
        {
            Text = title,
            Width = 190,
            Height = 26,
            Margin = new Padding(4, 8, 4, 0),
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });
    }

    private void SwitchView(string key)
    {
        if (!_views.TryGetValue(key, out var view))
        {
            return;
        }

        _activeKey = key;
        foreach (var candidate in _views.Values)
        {
            if (candidate is Control control)
            {
                control.Visible = ReferenceEquals(candidate, view);
            }
        }
        foreach (var item in _navigationButtons)
        {
            var selected = string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase);
            item.Value.BackColor = selected ? Color.FromArgb(219, 234, 254) : Color.White;
            item.Value.ForeColor = selected ? Color.FromArgb(30, 64, 175) : Color.FromArgb(51, 65, 85);
        }
        _activeView = view;
        var displayTitle = _viewLabels.TryGetValue(key, out var label) ? label : key;
        _activeViewLabel.Text = $"Đang mở: {displayTitle}";
    }

    private async Task StartAsync()
    {
        BuildViews();
        _timer.Interval = 15000;
        await RefreshDatabaseStatusAsync();
        if (_databaseContractReady)
        {
            _timer.Start();
            await RestartListenerAsync();
            await RefreshVisibleAsync();
        }
    }

    private void ApplyContext()
    {
        _viewContext = new AdminViewContext
        {
            Repository = _repository,
            RefreshComputersAsync = async () => await RefreshViewAsync("Computers"),
            ShowError = ShowError,
            NavigateToView = SwitchView,
            CurrentUser = _currentUser,
            CurrentEmployeeId = _currentEmployeeId,
            ConnectionSummary = AppConfig.ConnectionSummary(_currentUser),
            ListenerStatus = _listenerStatus,
            CanManageCustomers = _currentEmployeeId > 0,
            CanManageSessions = _currentEmployeeId > 0
        };
        foreach (var view in _views.Values)
        {
            view.SetContext(_viewContext);
        }
    }

    private async Task RefreshDatabaseStatusAsync()
    {
        if (string.IsNullOrWhiteSpace(AppConfig.Password))
        {
            _databaseContractReady = false;
            _reconcileAvailable = false;
            _runtimeSummary = "Chưa có mật khẩu kết nối CSDL";
            UpdateHeaderStatus();
            ShowError(new InvalidOperationException("Chưa cấu hình mật khẩu CSDL (DB_PASSWORD). Vui lòng tạo file .env hoặc cấu hình trong .env.example."));
            return;
        }

        try
        {
            var runtime = await _repository.GetDatabaseRuntimeInfoAsync(_lifetimeCancellation.Token);
            _runtimeSummary = runtime.Summary;
            _runtimeContractStatus = runtime.ContractStatus;
            _databaseContractReady = runtime.IsStationContractReady;
            _reconcileAvailable = runtime.HasReconcileFunction && _databaseContractReady;
            if (_currentEmployeeId <= 0)
            {
                var employees = await _repository.GetEmployeesAsync();
                var employee = employees.FirstOrDefault(item => string.Equals(item.DbUsername, _currentUser, StringComparison.OrdinalIgnoreCase));
                _currentEmployeeId = employee?.EmployeeId ?? 0;
            }
            ApplyContext();
            UpdateHeaderStatus();
            if (!_databaseContractReady)
            {
                ShowError(new InvalidOperationException(_runtimeContractStatus));
            }
        }
        catch (Exception exception)
        {
            _databaseContractReady = false;
            _reconcileAvailable = false;
            _runtimeSummary = "Không thể kết nối CSDL";
            UpdateHeaderStatus();
            ShowError(exception);
        }
    }

    private async Task RefreshVisibleAsync()
    {
        if (!_databaseContractReady && (_activeView is ComputersView || _activeView is MaintenanceView))
        {
            return;
        }
        if (_activeView is null || !await _refreshGate.WaitAsync(0))
        {
            return;
        }
        try
        {
            await _activeView.RefreshAsync(_lifetimeCancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task RefreshViewAsync(string key)
    {
        if (_views.TryGetValue(key, out var view) && ReferenceEquals(view, _activeView))
        {
            await RefreshVisibleAsync();
        }
    }

    private async Task RefreshNotificationViewsAsync()
    {
        var channels = _pendingNotificationChannels.ToArray();
        _pendingNotificationChannels.Clear();
        foreach (var channel in channels)
        {
            var target = channel switch
            {
                "computer_state_changed" => "Computers",
                "support_queue_changed" => "SupportQueue",
                "ai_security_alert" => "AiCenter",
                _ => string.Empty
            };
            if (!string.IsNullOrEmpty(target))
            {
                await RefreshViewAsync(target);
            }
        }
    }

    private async Task ReconcileAndRefreshAsync()
    {
        if (!_reconcileAvailable || _activeView is not ComputersView)
        {
            return;
        }
        try
        {
            await _repository.ReconcileStaleSessionsAsync(90, _lifetimeCancellation.Token);
            await RefreshVisibleAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            var error = DbRepository.ParseException(exception);
            if (error.SqlState == "42883")
            {
                _reconcileAvailable = false;
                _dbStatusLabel.Text = "Bản cập nhật 08 chưa có: tạm dừng đồng bộ phiên";
                return;
            }
            ShowError(exception);
        }
    }

    private async Task SwitchRoleAsync(string user)
    {
        _loadingRole = true;
        try
        {
            var password = AppConfig.GetConfiguredRolePassword(user);
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError(new InvalidOperationException($"Vai trò {user} chưa được cấu hình thông tin đăng nhập."));
                return;
            }
            var nextRepository = new DbRepository(AppConfig.GetConnectionString(user, password));
            var employees = await nextRepository.GetEmployeesAsync();
            var employee = employees.FirstOrDefault(item => string.Equals(item.DbUsername, user, StringComparison.OrdinalIgnoreCase));
            _listenerCancellation.Cancel();
            _listenerCancellation.Dispose();
            _listenerCancellation = new CancellationTokenSource();
            _repository = nextRepository;
            _currentUser = user;
            _currentEmployeeId = employee?.EmployeeId ?? 0;
            ApplyContext();
            await RefreshDatabaseStatusAsync();
            await RestartListenerAsync();
            await RefreshVisibleAsync();
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
        finally
        {
            _loadingRole = false;
        }
    }

    private async Task RestartListenerAsync()
    {
        _listenerStatus = "Đang hoạt động";
        UpdateHeaderStatus();
        _repository.OnNotificationReceived -= HandleNotification;
        _repository.OnNotificationReceived += HandleNotification;
        _ = Task.Run(() => _repository.StartListeningAsync(_listenerCancellation.Token));
        ApplyContext();
        await Task.CompletedTask;
    }

    private void HandleNotification(string channel, string payload)
    {
        if (IsDisposed)
        {
            return;
        }
        BeginInvoke(() =>
        {
            _pendingNotificationChannels.Add(channel);
            var notice = channel switch
            {
                "computer_state_changed" => "Đã cập nhật trạng thái máy",
                "support_queue_changed" => "Có yêu cầu hỗ trợ mới",
                "ai_security_alert" => "Có cảnh báo AI mới",
                _ => $"Đã nhận thông báo {channel}"
            };
            UpdateHeaderStatus(notice);
            if (channel is "ai_security_alert" or "computer_state_changed" or "support_queue_changed")
            {
                _notificationDebounceTimer.Stop();
                _notificationDebounceTimer.Start();
            }
        });
    }

    private void UpdateHeaderStatus(string? notice = null)
    {
        if (!string.IsNullOrWhiteSpace(notice))
        {
            _dbStatusLabel.Text = $"{notice} | Bộ lắng nghe CSDL: {_listenerStatus}";
            return;
        }

        var status = $"{_runtimeSummary} | Nhân viên: {_currentEmployeeId} | Bộ lắng nghe CSDL: {_listenerStatus}";
        if (!_databaseContractReady && !string.IsNullOrWhiteSpace(_runtimeContractStatus))
        {
            status += $" | {_runtimeContractStatus}";
        }
        _dbStatusLabel.Text = status;
    }

    private void ShowError(Exception exception)
    {
        var error = DbRepository.ParseException(exception);
        _errorLabel.Text = $"SQLSTATE {error.SqlState}: {error.Message} {error.ConstraintName}";
        _errorBanner.Visible = true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _timer.Stop();
        _notificationDebounceTimer.Stop();
        _listenerCancellation.Cancel();
        _lifetimeCancellation.Cancel();
        _refreshGate.Dispose();
        _listenerCancellation.Dispose();
        _lifetimeCancellation.Dispose();
        base.OnFormClosing(e);
    }
}
