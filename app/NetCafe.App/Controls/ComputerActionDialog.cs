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
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;
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
            BackColor = CyberTheme.Surface,
            Padding = new Padding(14)
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

        var statusText = _computer.ComputerStatus switch
        {
            "available" => "🟡 Sẵn sàng (Máy trống)",
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
        AddDetail(details, "Tiền giờ tạm tính", $"{EstimatePlayAmountValue():N0} đ");
        AddDetail(details, "Tiền dịch vụ", _computer.CurrentServiceAmount is null ? "0 đ" : $"{_computer.CurrentServiceAmount:N0} đ");
        if (_computer.ComputerStatus == "in_use")
        {
            var estSum = EstimatePlayAmountValue() + (_computer.CurrentServiceAmount ?? 0);
            AddDetail(details, "Tổng tạm tính", $"{estSum:N0} đ");
            if ((_computer.CurrentServiceAmount ?? 0) > 0)
            {
                AddDetail(details, "Lưu ý dịch vụ", "Đã lưu vào phiên — Tính gộp khi đóng máy");
            }
        }
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

        if (_context.IsTechnician)
        {
            _primaryActionButton.Text = "🔒 Chỉ xem trạng thái máy";
            CyberTheme.StyleButton(_primaryActionButton, CyberTheme.MutedText);
            _primaryActionButton.Enabled = false;
        }
        else if (_computer.ComputerStatus == "in_use")
        {
            _primaryActionButton.Text = "⏹ Kết thúc & Tính tiền";
            CyberTheme.StyleButton(_primaryActionButton, CyberTheme.Danger);
            _primaryActionButton.Enabled = _computer.CurrentSessionId.HasValue && _context.CanManageSessions && _context.CurrentEmployeeId > 0;
            _primaryActionButton.Click += async (_, _) => await CloseSessionAsync();
        }
        else if (_computer.ComputerStatus == "available")
        {
            _primaryActionButton.Text = "▶ Mở máy cho khách";
            CyberTheme.StyleButton(_primaryActionButton, CyberTheme.Success);
            _primaryActionButton.Enabled = _context.CanManageSessions && _context.CurrentEmployeeId > 0;
            _primaryActionButton.Click += async (_, _) => await OpenSessionForCustomerAsync();
        }
        else
        {
            _primaryActionButton.Text = "⚪ Máy không khả dụng";
            CyberTheme.StyleButton(_primaryActionButton, CyberTheme.MutedText);
            _primaryActionButton.Enabled = false;
        }

        if (_context.IsTechnician)
        {
            _customerButton.Text = "🔒 Cấm truy cập ví";
            CyberTheme.StyleButton(_customerButton, CyberTheme.MutedText);
            _customerButton.Enabled = false;
        }
        else if (_computer.ComputerStatus == "in_use")
        {
            if (_computer.CurrentCustomerId.HasValue)
            {
                _customerButton.Text = "💳 Nạp tiền cho khách";
                CyberTheme.StyleButton(_customerButton, CyberTheme.Success);
                _customerButton.Enabled = _context.CanManageCustomers;
                _customerButton.Click += async (_, _) => await TopUpForCurrentCustomerAsync();
            }
            else
            {
                _customerButton.Text = "👤 Khách vãng lai";
                CyberTheme.StyleButton(_customerButton, CyberTheme.SurfaceRaised);
                _customerButton.Enabled = false;
            }
        }
        else if (_computer.ComputerStatus == "available")
        {
            _customerButton.Text = "👥 Hội viên & Nạp ví";
            CyberTheme.StyleButton(_customerButton, CyberTheme.Primary);
            _customerButton.Enabled = _context.CanManageCustomers;
            _customerButton.Click += async (_, _) => await HandleMemberActionAsync();
        }
        else
        {
            _customerButton.Text = "👤 Hội viên";
            CyberTheme.StyleButton(_customerButton, CyberTheme.MutedText);
            _customerButton.Enabled = false;
        }

        if (_computer.ComputerStatus is "maintenance" or "broken")
        {
            _maintenanceButton.Text = "✅ Hoàn tất / Mở máy";
            CyberTheme.StyleButton(_maintenanceButton, CyberTheme.Success);
        }
        else
        {
            _maintenanceButton.Text = "🛠️ Báo hỏng / Bảo trì";
            CyberTheme.StyleButton(_maintenanceButton, CyberTheme.Warning);
        }

        _refreshButton.Text = "🔄 Làm mới";
        CyberTheme.StyleButton(_refreshButton, CyberTheme.SurfaceRaised);

        foreach (var button in new[] { _primaryActionButton, _customerButton, _maintenanceButton, _refreshButton })
        {
            button.Dock = DockStyle.Fill;
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Margin = new Padding(3);
        }

        actions.Controls.Add(_primaryActionButton, 0, 0);
        actions.Controls.Add(_customerButton, 1, 0);
        actions.Controls.Add(_maintenanceButton, 0, 1);
        actions.Controls.Add(_refreshButton, 1, 1);
        root.Controls.Add(actions, 0, 1);

        _messageLabel.Dock = DockStyle.Fill;
        _messageLabel.ForeColor = CyberTheme.Primary;
        _messageLabel.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
        root.Controls.Add(_messageLabel, 0, 2);

        Controls.Add(root);

        _maintenanceButton.Click += async (_, _) => await ToggleMaintenanceAsync();
        _refreshButton.Click += async (_, _) => await RefreshAsync();
        var canMaintain = (_context.IsAdmin || _context.IsManager || _context.IsTechnician) && _computer.ComputerStatus != "in_use";
        _maintenanceButton.Enabled = canMaintain;
    }

    private static void AddDetail(TableLayoutPanel table, string key, string value)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label
        {
            Text = key,
            AutoSize = true,
            ForeColor = CyberTheme.MutedText,
            Padding = new Padding(0, 4, 4, 4)
        }, 0, row);
        table.Controls.Add(new Label
        {
            Text = value,
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = CyberTheme.Text,
            Padding = new Padding(0, 4, 0, 4)
        }, 1, row);
    }

    private decimal EstimatePlayAmountValue() =>
        SessionCostCalculator.EstimatePlayAmount(
            _computer.HourlyRate,
            _computer.PlayedMinutes,
            _computer.CurrentPlayAmount);

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

    private async Task TopUpForCurrentCustomerAsync()
    {
        if (!_computer.CurrentCustomerId.HasValue || !_context.CanManageCustomers || _context.IsTechnician)
        {
            _messageLabel.Text = "Không thể nạp tiền cho phiên chơi này.";
            return;
        }

        try
        {
            var customer = new CustomerItem
            {
                CustomerId = _computer.CurrentCustomerId.Value,
                FullName = _computer.CurrentCustomerName ?? "Hội viên",
                Username = _computer.CurrentUsername ?? "member",
                Balance = _computer.CurrentBalance ?? 0,
                Phone = "",
                IsActive = true
            };

            using var dialog = new TopUpDialog(customer);
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Amount <= 0)
            {
                return;
            }

            var note = string.IsNullOrWhiteSpace(dialog.Note)
                ? $"Nạp tiền tại máy {_computer.ComputerCode}"
                : dialog.Note;

            var newBalance = await _context.Repository.TopUpAsync(
                customer.CustomerId,
                _context.CurrentEmployeeId,
                dialog.Amount,
                dialog.PaymentMethod,
                note);

            _computer.CurrentBalance = newBalance;
            _messageLabel.Text = $"Đã nạp {dialog.Amount:N0} đ thành công. Số dư mới: {newBalance:N0} đ (Đã lưu vào Sổ quỹ).";
            await _context.RefreshComputersAsync();
        }
        catch (Exception ex)
        {
            _messageLabel.Text = DbRepository.ParseException(ex).Message;
        }
    }

    private async Task HandleMemberActionAsync()
    {
        try
        {
            var customers = await _context.Repository.GetCustomersAsync();
            var activeCustomers = customers.Where(c => c.IsActive).ToList();

            using var picker = new MemberQuickActionDialog(activeCustomers, _computer.ComputerCode);
            var result = picker.ShowDialog(this);
            if (result == DialogResult.OK && picker.SelectedCustomer is not null)
            {
                if (picker.ActionChoice == MemberActionChoice.TopUp)
                {
                    using var topUpDialog = new TopUpDialog(picker.SelectedCustomer);
                    if (topUpDialog.ShowDialog(this) == DialogResult.OK && topUpDialog.Amount > 0)
                    {
                        var note = string.IsNullOrWhiteSpace(topUpDialog.Note)
                            ? $"Nạp tiền hội viên trước khi mở máy {_computer.ComputerCode}"
                            : topUpDialog.Note;

                        var newBalance = await _context.Repository.TopUpAsync(
                            picker.SelectedCustomer.CustomerId,
                            _context.CurrentEmployeeId,
                            topUpDialog.Amount,
                            topUpDialog.PaymentMethod,
                            note);

                        _messageLabel.Text = $"Đã nạp {topUpDialog.Amount:N0} đ cho {picker.SelectedCustomer.FullName}. Số dư mới: {newBalance:N0} đ.";
                        await _context.RefreshComputersAsync();
                    }
                }
                else if (picker.ActionChoice == MemberActionChoice.OpenSession)
                {
                    var sessionId = await _context.Repository.OpenSessionAsync(
                        picker.SelectedCustomer.CustomerId,
                        _computer.ComputerId,
                        _context.CurrentEmployeeId);

                    _messageLabel.Text = $"Đã mở máy thành công cho {picker.SelectedCustomer.FullName} (Mã phiên: {sessionId}).";
                    await _context.RefreshComputersAsync();
                    _primaryActionButton.Enabled = false;
                }
            }
            else if (picker.ActionChoice == MemberActionChoice.NavigateToCustomers)
            {
                Close();
                _context.NavigateToView?.Invoke("Customers");
            }
        }
        catch (Exception ex)
        {
            _messageLabel.Text = DbRepository.ParseException(ex).Message;
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
            var promotions = await _context.Repository.GetActivePromotionsAsync();
            var playEst = EstimatePlayAmountValue();
            var serviceEst = _computer.CurrentServiceAmount ?? 0;

            using var checkout = new SessionCheckoutDialog(_computer, playEst, serviceEst, promotions);
            if (checkout.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var invoiceId = await _context.Repository.CloseSessionAndPayAsync(
                _computer.CurrentSessionId.Value,
                _context.CurrentEmployeeId,
                checkout.SelectedPaymentMethod,
                checkout.SelectedPromoCode);

            var discountMsg = checkout.DiscountAmount > 0 ? $" (Giảm giá: -{checkout.DiscountAmount:N0} đ)" : "";
            _messageLabel.Text = $"Đã chốt phiên và tạo HĐ #{invoiceId}{discountMsg}. Tổng thu: {checkout.FinalTotal:N0} đ.";
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
            if (_computer.ComputerStatus is "maintenance" or "broken")
            {
                var ticket = (await _context.Repository.GetMaintenanceAsync())
                    .Where(item => item.ComputerId == _computer.ComputerId)
                    .Where(item => item.Status is not "fixed" and not "unrepairable")
                    .OrderByDescending(item => item.StartedAt)
                    .FirstOrDefault();
                if (ticket is null)
                {
                    _messageLabel.Text = "Máy chưa có phiếu bảo trì đang xử lý; hãy lập phiếu ở Tab Bảo trì.";
                    return;
                }

                using var completeDialog = new MaintenanceCompleteDialog();
                if (completeDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                await _context.Repository.CompleteMaintenanceAsync(
                    ticket.MaintenanceId,
                    completeDialog.Status,
                    completeDialog.Cost);
                _messageLabel.Text = "Đã hoàn tất phiếu bảo trì và đưa máy về trạng thái sẵn sàng.";
            }
            else
            {
                var technicians = (await _context.Repository.GetEmployeesAsync())
                    .Where(employee => employee.IsActive)
                    .Where(employee => employee.EmpRole.Equals("technician", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (technicians.Count == 0)
                {
                    _messageLabel.Text = "Chưa có kỹ thuật viên đang hoạt động để lập phiếu bảo trì.";
                    return;
                }

                using var createDialog = new MaintenanceEditorDialog(new[] { _computer }, technicians);
                if (createDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                if (string.IsNullOrWhiteSpace(createDialog.IssueDescription) || createDialog.SelectedTechnician is null)
                {
                    _messageLabel.Text = "Cần chọn kỹ thuật viên và nhập lý do bảo trì.";
                    return;
                }

                await _context.Repository.CreateMaintenanceAsync(
                    _computer.ComputerId,
                    createDialog.SelectedTechnician.EmployeeId,
                    createDialog.IssueDescription);
                _messageLabel.Text = "Đã lập phiếu bảo trì; trigger CSDL đã đồng bộ trạng thái máy.";
            }

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
        ForeColor = CyberTheme.Text;

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
