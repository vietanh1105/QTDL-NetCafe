// Trách nhiệm: panel thao tác máy đang chọn, thay MessageBox bằng giao diện có thông tin và nút rõ ràng.
// Liên kết: ComputerGridItem, AdminViewContext và DbRepository.

using System.Drawing;
using System.Windows.Forms;
using NetCafe.App.Views;
using NetCafe.Data;

namespace NetCafe.App.Controls;

public sealed class ComputerActionDialog : Form
{
    private readonly ComputerGridItem _computer;
    private readonly AdminViewContext _context;
    private readonly Label _messageLabel = new();
    private readonly Button _primaryActionButton = new();
    private readonly Button _customerButton = new();
    private readonly Button _maintenanceButton = new();
    private readonly Button _refreshButton = new();

    public ComputerActionDialog(ComputerGridItem computer, AdminViewContext context)
    {
        _computer = computer;
        _context = context;
        Text = $"Chi tiết {_computer.ComputerCode} ({_computer.RoomName})";
        StartPosition = FormStartPosition.CenterParent;
        Width = 460;
        Height = 650;
        MinimumSize = new Size(420, 580);
        BackColor = Color.FromArgb(248, 250, 252);
        BuildLayout();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(14)
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

        var statusText = _computer.ComputerStatus switch
        {
            "available" => "⚪ Sẵn sàng (Máy trống)",
            "in_use" => "🟢 Đang sử dụng (Đang chơi)",
            "maintenance" => "🔴 Đang bảo trì",
            "broken" => "🔴 Bị hỏng",
            _ => _computer.ComputerStatus
        };

        AddDetail(details, "Mã máy", _computer.ComputerCode);
        AddDetail(details, "Tên phòng", _computer.RoomName);
        AddDetail(details, "Trạng thái", statusText);
        AddDetail(details, "Giá giờ", $"{_computer.HourlyRate:N0} đ / giờ");
        AddDetail(details, "Khách đang ngồi", string.IsNullOrWhiteSpace(_computer.CurrentCustomerName) ? "Chưa có" : _computer.CurrentCustomerName);
        AddDetail(details, "Mã phiên chơi", _computer.CurrentSessionId?.ToString() ?? "Không có phiên");
        AddDetail(details, "Thời gian đã chơi", _computer.PlayedMinutes is null ? "Chưa có" : $"{_computer.PlayedMinutes:N0} phút");
        AddDetail(details, "Tiền giờ tạm tính", _computer.CurrentPlayAmount is null ? EstimatePlayAmount() : $"{_computer.CurrentPlayAmount:N0} đ");
        AddDetail(details, "Tiền dịch vụ", _computer.CurrentServiceAmount is null ? "0 đ" : $"{_computer.CurrentServiceAmount:N0} đ");
        AddDetail(details, "Số dư tài khoản", _computer.CurrentBalance is null ? "Chưa có" : $"{_computer.CurrentBalance:N0} đ");
        AddDetail(details, "Tín hiệu máy (Heartbeat)", _computer.LastHeartbeatAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") ?? "Chưa có dữ liệu");
        root.Controls.Add(details, 0, 0);

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(0, 8, 0, 0)
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        if (_computer.ComputerStatus == "in_use")
        {
            _primaryActionButton.Text = "⏹ Kết thúc & Tính tiền";
            _primaryActionButton.BackColor = Color.FromArgb(220, 38, 38);
            _primaryActionButton.ForeColor = Color.White;
            _primaryActionButton.Enabled = _computer.CurrentSessionId.HasValue && _context.CanManageSessions && _context.CurrentEmployeeId > 0;
            _primaryActionButton.Click += async (_, _) => await CloseSessionAsync();
        }
        else if (_computer.ComputerStatus == "available")
        {
            _primaryActionButton.Text = "▶ Mở máy cho khách";
            _primaryActionButton.BackColor = Color.FromArgb(16, 185, 129);
            _primaryActionButton.ForeColor = Color.White;
            _primaryActionButton.Enabled = _context.CanManageSessions && _context.CurrentEmployeeId > 0;
            _primaryActionButton.Click += async (_, _) => await OpenSessionForCustomerAsync();
        }
        else
        {
            _primaryActionButton.Text = "⚪ Máy không khả dụng";
            _primaryActionButton.BackColor = Color.FromArgb(148, 163, 184);
            _primaryActionButton.ForeColor = Color.White;
            _primaryActionButton.Enabled = false;
        }

        _customerButton.Text = "👤 Quản lý hội viên";
        _customerButton.BackColor = Color.FromArgb(234, 88, 12);
        _customerButton.ForeColor = Color.White;

        if (_computer.ComputerStatus is "maintenance" or "broken")
        {
            _maintenanceButton.Text = "✅ Hoàn tất / Mở máy";
            _maintenanceButton.BackColor = Color.FromArgb(5, 150, 105);
            _maintenanceButton.ForeColor = Color.White;
        }
        else
        {
            _maintenanceButton.Text = "🛠️ Báo hỏng / Bảo trì";
            _maintenanceButton.BackColor = Color.FromArgb(217, 119, 6);
            _maintenanceButton.ForeColor = Color.White;
        }

        _refreshButton.Text = "🔄 Làm mới";
        _refreshButton.BackColor = Color.FromArgb(100, 116, 139);
        _refreshButton.ForeColor = Color.White;

        foreach (var button in new[] { _primaryActionButton, _customerButton, _maintenanceButton, _refreshButton })
        {
            button.Dock = DockStyle.Fill;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Margin = new Padding(3);
        }

        actions.Controls.Add(_primaryActionButton, 0, 0);
        actions.Controls.Add(_customerButton, 1, 0);
        actions.Controls.Add(_maintenanceButton, 0, 1);
        actions.Controls.Add(_refreshButton, 1, 1);
        root.Controls.Add(actions, 0, 1);

        _messageLabel.Dock = DockStyle.Fill;
        _messageLabel.ForeColor = Color.FromArgb(234, 88, 12);
        _messageLabel.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
        root.Controls.Add(_messageLabel, 0, 2);

        Controls.Add(root);

        _customerButton.Click += (_, _) =>
        {
            Close();
            _context.NavigateToView?.Invoke("Customers");
        };
        _maintenanceButton.Click += async (_, _) => await ToggleMaintenanceAsync();
        _refreshButton.Click += async (_, _) => await RefreshAsync();
        _maintenanceButton.Enabled = _context.CanManageSessions && _computer.ComputerStatus != "in_use";
    }

    private static void AddDetail(TableLayoutPanel table, string key, string value)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label
        {
            Text = key,
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139),
            Padding = new Padding(0, 4, 4, 4)
        }, 0, row);
        table.Controls.Add(new Label
        {
            Text = value,
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(0, 4, 0, 4)
        }, 1, row);
    }

    private string EstimatePlayAmount() =>
        _computer.PlayedMinutes is null ? "Chưa có" : $"{_computer.HourlyRate * _computer.PlayedMinutes.Value / 60:N0} đ";

    private async Task OpenSessionForCustomerAsync()
    {
        try
        {
            var customers = await _context.Repository.GetCustomersAsync();
            var activeCustomers = customers.Where(c => c.IsActive && c.Balance >= _computer.HourlyRate).ToList();

            if (activeCustomers.Count == 0)
            {
                MessageBox.Show(this,
                    "Không có hội viên khả dụng nào có đủ số dư để mở máy. Vui lòng nạp tiền vào tài khoản hội viên trước.",
                    "Không thể mở máy",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using var picker = new OpenSessionCustomerPicker(activeCustomers, _computer.ComputerCode);
            if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedCustomer is not null)
            {
                var sessionId = await _context.Repository.OpenSessionAsync(
                    picker.SelectedCustomer.CustomerId,
                    _computer.ComputerId,
                    _context.CurrentEmployeeId);

                _messageLabel.Text = $"Đã mở máy thành công cho hội viên {picker.SelectedCustomer.FullName} (Mã phiên: {sessionId}).";
                await _context.RefreshComputersAsync();
                _primaryActionButton.Enabled = false;
            }
        }
        catch (Exception exception)
        {
            _messageLabel.Text = DbRepository.ParseException(exception).Message;
        }
    }

    private async Task CloseSessionAsync()
    {
        if (!_computer.CurrentSessionId.HasValue || !_context.CanManageSessions || _context.CurrentEmployeeId <= 0)
        {
            _messageLabel.Text = "Tài khoản hiện tại không có quyền đóng phiên máy.";
            return;
        }
        try
        {
            var estTotal = (_computer.CurrentPlayAmount ?? 0) + (_computer.CurrentServiceAmount ?? 0);
            var method = (_computer.CurrentBalance ?? 0) >= estTotal ? "balance" : "cash";
            await _context.Repository.CloseSessionAndPayAsync(_computer.CurrentSessionId.Value, _context.CurrentEmployeeId, method);
            _messageLabel.Text = "Đã kết thúc phiên chơi và lập hóa đơn thanh toán thành công.";
            await _context.RefreshComputersAsync();
            _primaryActionButton.Enabled = false;
        }
        catch (Exception exception)
        {
            _messageLabel.Text = DbRepository.ParseException(exception).Message;
            _primaryActionButton.Enabled = true;
        }
    }

    private async Task ToggleMaintenanceAsync()
    {
        try
        {
            var targetStatus = _computer.ComputerStatus is "maintenance" or "broken" ? "available" : "maintenance";
            await _context.Repository.SetComputerStatusAsync(_computer.ComputerId, targetStatus);
            _messageLabel.Text = targetStatus == "maintenance"
                ? "Đã chuyển máy sang trạng thái bảo trì."
                : "Đã hoàn tất bảo trì và mở lại máy.";
            await _context.RefreshComputersAsync();
            _maintenanceButton.Enabled = false;
        }
        catch (Exception exception)
        {
            _messageLabel.Text = DbRepository.ParseException(exception).Message;
        }
    }

    private async Task RefreshAsync()
    {
        await _context.RefreshComputersAsync();
        _messageLabel.Text = "Đã cập nhật trạng thái mới nhất từ CSDL.";
    }
}

internal sealed class OpenSessionCustomerPicker : Form
{
    private readonly ComboBox _customerCombo = new();
    public CustomerItem? SelectedCustomer { get; private set; }

    public OpenSessionCustomerPicker(IReadOnlyList<CustomerItem> customers, string computerCode)
    {
        Text = $"Mở máy cho khách - {computerCode}";
        StartPosition = FormStartPosition.CenterParent;
        Width = 420;
        Height = 220;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = CyberTheme.Background;

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(16) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var prompt = new Label { Text = "Chọn hội viên để mở máy và bắt đầu tính giờ:", AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _customerCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _customerCombo.Dock = DockStyle.Top;
        _customerCombo.Font = new Font("Segoe UI", 10F);
        foreach (var c in customers)
        {
            _customerCombo.Items.Add(new CustomerOption(c));
        }
        if (_customerCombo.Items.Count > 0) _customerCombo.SelectedIndex = 0;

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.RightToLeft };
        var okButton = new Button
        {
            Text = "Xác nhận mở",
            DialogResult = DialogResult.OK,
            Width = 115,
            Height = 36,
            BackColor = CyberTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        okButton.FlatAppearance.BorderSize = 0;
        okButton.Click += (_, _) =>
        {
            if (_customerCombo.SelectedItem is CustomerOption option)
            {
                SelectedCustomer = option.Customer;
            }
        };

        var cancelButton = new Button { Text = "Hủy bỏ", DialogResult = DialogResult.Cancel, Width = 80, Height = 36 };
        CyberTheme.StyleButton(cancelButton, CyberTheme.SurfaceRaised);
        btnPanel.Controls.Add(cancelButton);
        btnPanel.Controls.Add(okButton);

        panel.Controls.Add(prompt, 0, 0);
        panel.Controls.Add(_customerCombo, 0, 1);
        panel.Controls.Add(btnPanel, 0, 2);
        Controls.Add(panel);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private sealed class CustomerOption
    {
        public CustomerItem Customer { get; }
        public CustomerOption(CustomerItem customer) => Customer = customer;
        public override string ToString() => $"{Customer.FullName} (@{Customer.Username}) - Số dư: {Customer.Balance:N0} đ";
    }
}
