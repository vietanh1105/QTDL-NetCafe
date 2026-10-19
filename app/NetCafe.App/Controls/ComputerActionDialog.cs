// Trách nhiệm: panel thao tác máy đang chọn, thay MessageBox bằng giao diện có thông tin và nút rõ ràng.
// Liên kết: ComputerGridItem, AdminViewContext và DbRepository.

using System.Drawing;
using NetCafe.App.Views;
using NetCafe.Data;

namespace NetCafe.App.Controls;

public sealed class ComputerActionDialog : Form
{
    private readonly ComputerGridItem _computer;
    private readonly AdminViewContext _context;
    private readonly Label _messageLabel = new();
    private readonly Button _closeButton = new();
    private readonly Button _customerButton = new();
    private readonly Button _maintenanceButton = new();
    private readonly Button _refreshButton = new();

    public ComputerActionDialog(ComputerGridItem computer, AdminViewContext context)
    {
        _computer = computer;
        _context = context;
        Text = $"Chi tiết {_computer.ComputerCode}";
        StartPosition = FormStartPosition.CenterParent;
        Width = 430;
        Height = 760;
        MinimumSize = new Size(380, 620);
        BackColor = Color.FromArgb(243, 246, 251);
        BuildLayout();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(16), BackColor = BackColor };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        var details = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true, BackColor = Color.White, Padding = new Padding(14) };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        var statusText = _computer.ComputerStatus switch
        {
            "available" => "Sẵn sàng",
            "in_use" => "Đang sử dụng",
            "maintenance" => "Đang bảo trì",
            "broken" => "Hỏng hóc",
            _ => _computer.ComputerStatus
        };

        AddDetail(details, "Mã máy", _computer.ComputerCode);
        AddDetail(details, "Trạng thái", statusText);
        AddDetail(details, "Khách hàng", string.IsNullOrWhiteSpace(_computer.CurrentCustomerName) ? "Chưa có" : _computer.CurrentCustomerName);
        AddDetail(details, "Mã phiên", _computer.CurrentSessionId?.ToString() ?? "Chưa có");
        AddDetail(details, "Thời gian chơi", _computer.PlayedMinutes is null ? "Chưa có" : $"{_computer.PlayedMinutes:N0} phút");
        AddDetail(details, "Tiền giờ", _computer.CurrentPlayAmount is null ? EstimatePlayAmount() : $"{_computer.CurrentPlayAmount:N0} đ");
        AddDetail(details, "Dịch vụ F&B", _computer.CurrentServiceAmount is null ? "0 đ" : $"{_computer.CurrentServiceAmount:N0} đ");
        AddDetail(details, "Số dư tài khoản", _computer.CurrentBalance is null ? "Chưa có dữ liệu" : $"{_computer.CurrentBalance:N0} đ");
        AddDetail(details, "Tín hiệu máy", _computer.LastHeartbeatAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") ?? "Chưa có dữ liệu");
        root.Controls.Add(details, 0, 0);
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(0, 8, 0, 0) };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _closeButton.Text = "Đăng xuất máy";
        _closeButton.BackColor = Color.Firebrick;
        _closeButton.ForeColor = Color.White;
        _customerButton.Text = "Xem hội viên";
        _maintenanceButton.Text = "Đưa vào bảo trì";
        _refreshButton.Text = "Làm mới";
        foreach (var button in new[] { _closeButton, _customerButton, _maintenanceButton, _refreshButton })
        {
            button.Dock = DockStyle.Fill;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Margin = new Padding(4);
        }
        actions.Controls.Add(_closeButton, 0, 0);
        actions.Controls.Add(_customerButton, 1, 0);
        actions.Controls.Add(_maintenanceButton, 0, 1);
        actions.Controls.Add(_refreshButton, 1, 1);
        root.Controls.Add(actions, 0, 1);
        _messageLabel.Dock = DockStyle.Fill;
        _messageLabel.ForeColor = Color.FromArgb(30, 64, 175);
        root.Controls.Add(_messageLabel, 0, 2);
        Controls.Add(root);
        _closeButton.Click += async (_, _) => await CloseSessionAsync();
        _customerButton.Click += (_, _) => _context.NavigateToView?.Invoke("Customers");
        _maintenanceButton.Click += async (_, _) => await SetMaintenanceAsync();
        _refreshButton.Click += async (_, _) => await RefreshAsync();
        _closeButton.Enabled = _computer.CurrentSessionId.HasValue && _context.CanManageSessions && _context.CurrentEmployeeId > 0;
        _maintenanceButton.Enabled = _context.CanManageSessions && _computer.ComputerStatus != "in_use";
    }

    private static void AddDetail(TableLayoutPanel table, string key, string value)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label { Text = key, AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139), Padding = new Padding(0, 4, 4, 4) }, 0, row);
        table.Controls.Add(new Label { Text = value, AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Padding = new Padding(0, 4, 0, 4) }, 1, row);
    }

    private string EstimatePlayAmount() => _computer.PlayedMinutes is null ? "Chưa có" : $"{_computer.HourlyRate * _computer.PlayedMinutes.Value / 60:N0} đ";

    private async Task CloseSessionAsync()
    {
        if (!_computer.CurrentSessionId.HasValue || !_context.CanManageSessions || _context.CurrentEmployeeId <= 0)
        {
            _messageLabel.Text = "Tài khoản hiện tại không có quyền đăng xuất máy.";
            return;
        }
        try
        {
            await _context.Repository.CloseSessionAndPayAsync(_computer.CurrentSessionId.Value, _context.CurrentEmployeeId);
            _messageLabel.Text = "Đã đăng xuất máy và tạo hóa đơn thanh toán thành công.";
            await _context.RefreshComputersAsync();
            _closeButton.Enabled = false;
        }
        catch (Exception exception)
        {
            _messageLabel.Text = DbRepository.ParseException(exception).Message;
            _closeButton.Enabled = true;
        }
    }

    private async Task SetMaintenanceAsync()
    {
        try
        {
            await _context.Repository.SetComputerStatusAsync(_computer.ComputerId, "maintenance");
            _messageLabel.Text = "Đã chuyển máy sang trạng thái bảo trì.";
            await _context.RefreshComputersAsync();
        }
        catch (Exception exception)
        {
            _messageLabel.Text = DbRepository.ParseException(exception).Message;
        }
    }

    private async Task RefreshAsync()
    {
        await _context.RefreshComputersAsync();
        _messageLabel.Text = "Đã cập nhật trạng thái mới nhất.";
    }
}
