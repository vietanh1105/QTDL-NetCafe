// Trách nhiệm: shell điều phối navigation, repository lifecycle và refresh Admin views.
// Liên kết: MainForm.Designer.cs, Views/*.cs, AppConfig.cs và DbRepository partial.

using System.Drawing;
using NetCafe.App.Views;
using NetCafe.Data;

namespace NetCafe.App;

public partial class MainForm : Form
{
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly Dictionary<string, IAdminView> _views = new(StringComparer.OrdinalIgnoreCase);
    private DbRepository _repository = null!;
    private CancellationTokenSource _listenerCancellation = new();
    private AdminViewContext _viewContext = null!;
    private string _currentUser = "postgres";
    private int _currentEmployeeId;
    private IAdminView? _activeView;
    private bool _loadingRole;

    public MainForm()
    {
        AppConfig.LoadEnv();
        _repository = new DbRepository(AppConfig.GetConnectionString());
        InitializeComponent();
        InitializeShell();
        Shown += async (_, _) => await StartAsync();
    }

    private void InitializeShell()
    {
        _roleCombo.Items.AddRange(new object[] { "postgres", "nv_manager", "nv_thao", "nv_hung" });
        _roleCombo.SelectedItem = _currentUser;
        _roleCombo.SelectedIndexChanged += async (_, _) =>
        {
            if (!_loadingRole)
            {
                await SwitchRoleAsync(_roleCombo.SelectedItem?.ToString() ?? "postgres");
            }
        };

        _refreshButton.Click += async (_, _) => await RefreshVisibleAsync();
        _dismissErrorButton.Click += (_, _) => _errorBanner.Visible = false;
        _timer.Tick += async (_, _) => await ReconcileAndRefreshAsync();
    }

    private void BuildViews()
    {
        AddView("Computers", "Máy trạm", new ComputersView());
        AddView("Customers", "Hội viên", new CustomersView());
        AddView("Booking", "Đặt máy", new BookingView());
        AddView("Orders", "F&B", new OrdersView());
        AddView("Invoices", "Hóa đơn", new InvoicesView());
        AddView("Reports", "Báo cáo", new ReportsView());
        AddView("Employees", "Nhân viên", new EmployeesView());
        AddView("Maintenance", "Bảo trì", new MaintenanceView());
        AddView("AiCenter", "AI Center", new AiCenterView());
        AddView("SupportQueue", "Support queue", new SupportQueueView());
        AddView("Settings", "Cài đặt", new SettingsView());

        _viewContext = new AdminViewContext
        {
            Repository = _repository,
            RefreshComputersAsync = async () => await RefreshViewAsync("Computers"),
            ShowError = ShowError,
            CurrentUser = _currentUser,
            CurrentEmployeeId = _currentEmployeeId
        };

        foreach (var view in _views.Values)
        {
            view.SetContext(_viewContext);
        }

        SwitchView("Computers");
    }

    private void AddView(string key, string label, IAdminView view)
    {
        _views.Add(key, view);
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
        button.Click += async (_, _) =>
        {
            SwitchView(key);
            await RefreshVisibleAsync();
        };
        _navigationPanel.Controls.Add(button);
    }

    private void SwitchView(string key)
    {
        if (!_views.TryGetValue(key, out var view))
        {
            return;
        }

        foreach (var candidate in _views.Values)
        {
            if (candidate is Control control)
            {
                control.Visible = ReferenceEquals(candidate, view);
            }
        }
        _activeView = view;
        _activeViewLabel.Text = $"Đang mở: {key}";
    }

    private async Task StartAsync()
    {
        BuildViews();
        _timer.Interval = 15000;
        _timer.Start();
        await RestartListenerAsync();
        await RefreshVisibleAsync();
    }

    private async Task RefreshVisibleAsync()
    {
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
        if (!_views.TryGetValue(key, out var view) || !ReferenceEquals(view, _activeView))
        {
            return;
        }
        await RefreshVisibleAsync();
    }

    private async Task ReconcileAndRefreshAsync()
    {
        if (_activeView is not ComputersView)
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
            ShowError(exception);
        }
    }

    private async Task SwitchRoleAsync(string user)
    {
        _loadingRole = true;
        try
        {
            var password = AppConfig.GetConfiguredRolePassword(user);
            var nextRepository = new DbRepository(AppConfig.GetConnectionString(user, password));
            var employees = await nextRepository.GetEmployeesAsync();
            var employee = employees.FirstOrDefault(item => string.Equals(item.DbUsername, user, StringComparison.OrdinalIgnoreCase));

            _listenerCancellation.Cancel();
            _listenerCancellation.Dispose();
            _listenerCancellation = new CancellationTokenSource();
            _repository = nextRepository;
            _currentUser = user;
            _currentEmployeeId = employee?.EmployeeId ?? 0;
            _viewContext = new AdminViewContext
            {
                Repository = _repository,
                RefreshComputersAsync = async () => await RefreshViewAsync("Computers"),
                ShowError = ShowError,
                CurrentUser = _currentUser,
                CurrentEmployeeId = _currentEmployeeId
            };
            foreach (var view in _views.Values)
            {
                view.SetContext(_viewContext);
            }
            _dbStatusLabel.Text = $"Role: {_currentUser} | Employee: {_currentEmployeeId}";
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
        _repository.OnNotificationReceived -= HandleNotification;
        _repository.OnNotificationReceived += HandleNotification;
        _ = Task.Run(() => _repository.StartListeningAsync(_listenerCancellation.Token));
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
            _dbStatusLabel.Text = $"🔔 {channel}: cảnh báo mới";
            if (_activeView is AiCenterView)
            {
                _ = RefreshVisibleAsync();
            }
        });
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
        _listenerCancellation.Cancel();
        _lifetimeCancellation.Cancel();
        _refreshGate.Dispose();
        _listenerCancellation.Dispose();
        _lifetimeCancellation.Dispose();
        base.OnFormClosing(e);
    }
}
