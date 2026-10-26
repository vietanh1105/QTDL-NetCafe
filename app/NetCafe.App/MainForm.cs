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
    private readonly Dictionary<string, int> _employeeIdCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DbRepository> _roleRepositoryCache = new(StringComparer.OrdinalIgnoreCase);
    private DbRepository _repository = null!;
    private DbRepository? _listenerRepository;
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
    private DatabaseRuntimeInfo? _runtimeInfo;
    private string _listenerStatus = "Chưa khởi động";
    private readonly System.Windows.Forms.Timer _notificationDebounceTimer = new() { Interval = 500 };

    public MainForm()
    {
        AppConfig.LoadEnv();
        var configuredRoles = AppConfig.GetConfiguredRoles();
        _currentUser = configuredRoles.Count > 0 ? configuredRoles[0] : AppConfig.Username;
        _currentEmployeeId = AppConfig.AdminEmployeeId;
        _repository = new DbRepository(AppConfig.GetConnectionString(_currentUser, AppConfig.GetConfiguredRolePassword(_currentUser)));
        _roleRepositoryCache[_currentUser] = _repository;
        _employeeIdCache["nv_admin"] = AppConfig.AdminEmployeeId > 0 ? AppConfig.AdminEmployeeId : 1;
        _employeeIdCache["nv_manager"] = 2;
        _employeeIdCache["nv_thao"] = 3;
        _employeeIdCache["nv_hung"] = 6;
        InitializeComponent();
        InitializeShell();
        Shown += async (_, _) => await StartAsync();
    }

    private void InitializeShell()
    {
        var roleOptions = AppConfig.GetConfiguredRoles().Select(CreateRoleOption).Cast<object>().ToArray();
        _roleCombo.Items.AddRange(roleOptions);
        if (_roleCombo.Items.Count > 0)
        {
            _roleCombo.SelectedItem = roleOptions.FirstOrDefault(option => option is RoleOption item && item.User.Equals(_currentUser, StringComparison.OrdinalIgnoreCase)) ?? roleOptions[0];
        }
        _roleCombo.SelectedIndexChanged += async (_, _) =>
        {
            if (!_loadingRole && _roleCombo.SelectedItem is RoleOption option)
            {
                await SwitchRoleAsync(option.User);
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
        AddNavigationSeparator("VẬN HÀNH PHÒNG MÁY");
        AddView("Computers", "🖥️  Máy trạm", new ComputersView());
        AddView("Customers", "👥  Hội viên", new CustomersView());
        AddView("Orders", "🍽️  Gọi món", new OrdersView());
        AddView("Invoices", "🧾  Hóa đơn", new InvoicesView());

        AddNavigationSeparator("BÁO CÁO & NHÂN SỰ");
        AddView("Reports", "📊  Báo cáo doanh thu", new ReportsView());
        AddView("Employees", "👔  Nhân viên", new EmployeesView());
        AddView("Maintenance", "🔧  Bảo trì thiết bị", new MaintenanceView());

        AddNavigationSeparator("HỆ THỐNG & AI");
        AddView("AiCenter", "🧠  Phân tích dữ liệu", new AiCenterView());
        AddView("SupportQueue", "🔔  Yêu cầu hỗ trợ", new SupportQueueView());
        AddView("Settings", "⚙️  Thông tin hệ thống", new SettingsView());
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
            Width = 220,
            Height = 40,
            Margin = new Padding(4, 2, 4, 2),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(28, 31, 42),
            ForeColor = CyberTheme.Text,
            Font = new Font("Segoe UI", 9.5F),
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
                button.BackColor = CyberTheme.Surface;
            }
        };
        button.MouseLeave += (_, _) =>
        {
            if (!string.Equals(_activeKey, key, StringComparison.OrdinalIgnoreCase))
            {
                button.BackColor = Color.FromArgb(28, 31, 42);
            }
        };
        _navigationPanel.Controls.Add(button);
    }

    private void AddNavigationSeparator(string title)
    {
        _navigationPanel.Controls.Add(new Label
        {
            Text = title,
            Width = 220,
            Height = 28,
            Margin = new Padding(4, 10, 4, 2),
            ForeColor = CyberTheme.Primary,
            Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            UseMnemonic = false
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
            item.Value.BackColor = selected ? CyberTheme.SurfaceRaised : Color.FromArgb(28, 31, 42);
            item.Value.ForeColor = selected ? CyberTheme.Primary : CyberTheme.Text;
            item.Value.Font = new Font("Segoe UI", 9.5F, selected ? FontStyle.Bold : FontStyle.Regular);
        }
        UpdateNavigationStates(ResolveRole(_currentUser));
        _activeView = view;
        var displayTitle = _viewLabels.TryGetValue(key, out var label) ? label : key;
        _activeViewLabel.Text = $"Hệ thống  ›  {displayTitle}";
    }

    private async Task StartAsync()
    {
        BuildViews();
        _timer.Interval = 30000;
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
        var role = ResolveRole(_currentUser);
        var isTech = role == "role_technician";
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
            RuntimeInfo = _runtimeInfo,
            UserRole = role,
            CanManageCustomers = !isTech && _currentEmployeeId > 0,
            CanManageSessions = !isTech && _currentEmployeeId > 0
        };
        foreach (var view in _views.Values)
        {
            view.SetContext(_viewContext);
        }
        UpdateNavigationStates(role);
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
            _runtimeInfo = runtime;
            _runtimeSummary = runtime.Summary;
            _runtimeContractStatus = runtime.ContractStatus;
            _databaseContractReady = runtime.IsStationContractReady;
            _reconcileAvailable = runtime.HasReconcileFunction && _databaseContractReady;
            var employees = await _repository.GetEmployeesAsync();
            foreach (var employee in employees)
            {
                if (!string.IsNullOrWhiteSpace(employee.DbUsername))
                {
                    _employeeIdCache[employee.DbUsername] = employee.EmployeeId;
                }
            }
            if (_currentEmployeeId <= 0 && _employeeIdCache.TryGetValue(_currentUser, out var empId))
            {
                _currentEmployeeId = empId;
            }
            ApplyContext();
            UpdateHeaderStatus();
            if (!_databaseContractReady)
            {
                ShowError(new InvalidOperationException(_runtimeContractStatus));
            }
            else
            {
                ClearError();
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

    private async Task RefreshVisibleAsync(bool force = false)
    {
        if (!_databaseContractReady)
        {
            await RefreshDatabaseStatusAsync();
            if (!_databaseContractReady && (_activeView is ComputersView || _activeView is MaintenanceView))
            {
                return;
            }
        }

        if (_activeView is null)
        {
            return;
        }

        if (force)
        {
            await _refreshGate.WaitAsync(_lifetimeCancellation.Token);
        }
        else if (!await _refreshGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            await _activeView.RefreshAsync(_lifetimeCancellation.Token);
            ClearError();
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
                "food_order_changed" => "Orders",
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
        if (!_databaseContractReady)
        {
            await RefreshDatabaseStatusAsync();
            if (!_databaseContractReady)
            {
                return;
            }
        }

        if (!_reconcileAvailable || _activeView is not ComputersView)
        {
            return;
        }

        var canReconcile = _currentUser.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
                           _currentUser.Contains("manager", StringComparison.OrdinalIgnoreCase);
        if (!canReconcile)
        {
            await RefreshVisibleAsync();
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
            if (!DbRepository.IsTransientException(exception))
            {
                ShowError(exception);
            }
        }
    }

    private async Task SwitchRoleAsync(string user)
    {
        if (string.Equals(_currentUser, user, StringComparison.OrdinalIgnoreCase) && _repository is not null)
        {
            return;
        }

        _loadingRole = true;
        try
        {
            var password = AppConfig.GetConfiguredRolePassword(user);
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError(new InvalidOperationException($"Vai trò {user} chưa được cấu hình thông tin đăng nhập."));
                return;
            }

            if (!_roleRepositoryCache.TryGetValue(user, out var nextRepository))
            {
                nextRepository = new DbRepository(AppConfig.GetConnectionString(user, password));
                _roleRepositoryCache[user] = nextRepository;
            }

            if (!_employeeIdCache.TryGetValue(user, out var empId))
            {
                empId = user switch
                {
                    "nv_admin" => AppConfig.AdminEmployeeId > 0 ? AppConfig.AdminEmployeeId : 1,
                    "nv_manager" => 2,
                    "nv_thao" => 3,
                    "nv_hung" => 6,
                    _ => 0
                };
            }

            _repository = nextRepository;
            _currentUser = user;
            _currentEmployeeId = empId;
            if (_runtimeInfo is not null)
            {
                _runtimeInfo = new DatabaseRuntimeInfo
                {
                    Database = _runtimeInfo.Database,
                    User = user,
                    Host = _runtimeInfo.Host,
                    ServerVersion = _runtimeInfo.ServerVersion,
                    HasStationMigration = _runtimeInfo.HasStationMigration,
                    HasReconcileFunction = _runtimeInfo.HasReconcileFunction,
                    HasComputerGridContract = _runtimeInfo.HasComputerGridContract,
                    HasCustomerMenuContract = _runtimeInfo.HasCustomerMenuContract,
                    HasStationLogoutContract = _runtimeInfo.HasStationLogoutContract,
                    CanRunVacuum = _runtimeInfo.CanRunVacuum && ResolveRole(user) == "role_admin"
                };
            }
            _runtimeSummary = $"Máy chủ={AppConfig.Host}:{AppConfig.Port} | CSDL={AppConfig.Database} | Role={user}";
            ClearError();
            ApplyContext();
            var role = ResolveRole(user);
            if (IsTabForbidden(_activeKey, role))
            {
                SwitchView("Computers");
            }
            UpdateHeaderStatus($"Đã chuyển vai trò: {user}");

            await RefreshVisibleAsync(force: true);
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
        if (_listenerRepository is null)
        {
            _listenerRepository = new DbRepository(AppConfig.GetConnectionString());
            _listenerRepository.OnNotificationReceived += HandleNotification;
            _ = Task.Run(() => _listenerRepository.StartListeningAsync(_listenerCancellation.Token));
        }
        _listenerStatus = "Đang hoạt động";
        UpdateHeaderStatus();
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
                "food_order_changed" => "Có đơn món mới hoặc cập nhật",
                _ => $"Đã nhận thông báo {channel}"
            };
            UpdateHeaderStatus(notice);
            if (channel is "ai_security_alert" or "computer_state_changed" or "support_queue_changed" or "food_order_changed")
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
            _dbStatusLabel.Text = $"{notice} | Đồng bộ: {_listenerStatus}";
            return;
        }

        var status = $"● CSDL Cloud: {AppConfig.Database} | Người dùng: {_currentUser} | Đồng bộ: {_listenerStatus}";
        if (!_databaseContractReady && !string.IsNullOrWhiteSpace(_runtimeContractStatus))
        {
            status += $" | {_runtimeContractStatus}";
        }
        _dbStatusLabel.Text = status;
    }

    private void ClearError()
    {
        if (_errorBanner.Visible)
        {
            _errorBanner.Visible = false;
        }
    }

    private void ShowError(Exception exception)
    {
        var error = DbRepository.ParseException(exception);
        _errorLabel.Text = $"SQLSTATE {error.SqlState}: {error.Message} {error.ConstraintName}";
        _errorBanner.Visible = true;
    }

    public static string ResolveRole(string user)
    {
        var u = (user ?? string.Empty).ToLowerInvariant();
        if (u.Contains("manager")) return "role_manager";
        if (u.Contains("thao") || u.Contains("cashier") || u.Contains("nam")) return "role_cashier";
        if (u.Contains("hung") || u.Contains("tech")) return "role_technician";
        return "role_admin";
    }

    public static bool IsTabForbidden(string key, string role)
    {
        if (role == "role_admin") return false;
        if (role == "role_manager")
        {
            return key is "Settings";
        }
        if (role == "role_cashier")
        {
            return key is "Reports" or "Maintenance" or "AiCenter" or "Settings";
        }
        if (role == "role_technician")
        {
            return key is "Orders" or "Invoices" or "Reports" or "AiCenter" or "Settings";
        }
        return false;
    }

    private void UpdateNavigationStates(string role)
    {
        foreach (var (key, button) in _navigationButtons)
        {
            var baseLabel = _viewLabels.TryGetValue(key, out var lbl) ? lbl : key;
            var isForbidden = IsTabForbidden(key, role);
            if (isForbidden)
            {
                button.Text = $"{baseLabel} 🔒";
                button.ForeColor = CyberTheme.MutedText;
            }
            else
            {
                var selected = string.Equals(_activeKey, key, StringComparison.OrdinalIgnoreCase);
                button.Text = baseLabel;
                button.ForeColor = selected ? CyberTheme.Primary : CyberTheme.Text;
            }
        }
    }

    private static RoleOption CreateRoleOption(string user)
    {
        var role = ResolveRole(user);
        var label = role switch
        {
            "role_manager" => "👔 Quản lý (nv_manager)",
            "role_cashier" => "💵 Thu ngân (nv_thao)",
            "role_technician" => "🔧 Kỹ thuật viên (nv_hung)",
            _ => "👑 Chủ quán (nv_admin)"
        };
        return new RoleOption(user, $"{label} [{role}]");
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

internal sealed record RoleOption(string User, string Display)
{
    public override string ToString() => Display;
}
