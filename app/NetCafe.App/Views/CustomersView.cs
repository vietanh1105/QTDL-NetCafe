// Trách nhiệm: danh sách hội viên và điểm nối cho CRUD/top-up Admin.
// Liên kết: core.customer, billing.sp_top_up và DbRepository.GetCustomersAsync.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public partial class CustomersView : AdminCardViewBase
{
    private readonly TextBox _searchBox = new();
    private readonly Button _searchButton = new();
    private readonly Button _clearSearchButton = new();
    private readonly Button _topUpButton = new();
    private readonly Button _createButton = new();
    private readonly Button _editButton = new();
    private readonly Button _toggleButton = new();
    private readonly Button _resetButton = new();

    public CustomersView()
    {
        InitializeComponent();
        SetTitle("Quản lý Hội viên & Ví tài khoản", "Tìm kiếm theo tài khoản/tên/SĐT; nạp tiền qua hàm CSDL có tự động ghi nhận thanh toán.");

        // Cấu hình thanh tìm kiếm tài khoản
        _searchBox.Width = 250;
        _searchBox.Height = 32;
        _searchBox.PlaceholderText = "Tài khoản (username), tên, SĐT...";
        _searchBox.Font = new Font("Segoe UI", 9.5F);
        _searchBox.Margin = new Padding(0, 4, 4, 4);

        _searchButton.Text = "🔍 Tìm";
        _searchButton.Width = 65;
        _searchButton.Height = 32;
        _searchButton.BackColor = CyberTheme.Primary;
        _searchButton.ForeColor = Color.White;
        _searchButton.FlatStyle = FlatStyle.Flat;
        _searchButton.FlatAppearance.BorderSize = 0;
        _searchButton.Margin = new Padding(0, 4, 4, 4);

        _clearSearchButton.Text = "✕ Tất cả";
        _clearSearchButton.Width = 75;
        _clearSearchButton.Height = 32;
        _clearSearchButton.BackColor = CyberTheme.SurfaceRaised;
        _clearSearchButton.ForeColor = CyberTheme.Text;
        _clearSearchButton.FlatStyle = FlatStyle.Flat;
        _clearSearchButton.FlatAppearance.BorderSize = 0;
        _clearSearchButton.Margin = new Padding(0, 4, 12, 4);

        // Nút nạp tiền nổi bật
        _topUpButton.Text = "💰 Nạp tiền";
        _topUpButton.Width = 100;
        _topUpButton.Height = 32;
        _topUpButton.BackColor = Color.FromArgb(16, 185, 129);
        _topUpButton.ForeColor = Color.White;
        _topUpButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _topUpButton.FlatStyle = FlatStyle.Flat;
        _topUpButton.FlatAppearance.BorderSize = 0;
        _topUpButton.Margin = new Padding(0, 4, 6, 4);

        ConfigureActionButton(_createButton, "+ Thêm mới", CyberTheme.Primary);
        ConfigureActionButton(_editButton, "Chỉnh sửa", CyberTheme.SurfaceRaised);
        ConfigureActionButton(_toggleButton, "Khóa/Mở", CyberTheme.Warning);
        ConfigureActionButton(_resetButton, "Đổi MK", CyberTheme.SurfaceRaised);

        ActionsPanel.Controls.Add(_searchBox);
        ActionsPanel.Controls.Add(_searchButton);
        ActionsPanel.Controls.Add(_clearSearchButton);
        ActionsPanel.Controls.Add(_topUpButton);
        ActionsPanel.Controls.Add(_createButton);
        ActionsPanel.Controls.Add(_editButton);
        ActionsPanel.Controls.Add(_toggleButton);
        ActionsPanel.Controls.Add(_resetButton);








        _searchBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) await RefreshAsync();
        };
        _searchButton.Click += async (_, _) => await RefreshAsync();
        _clearSearchButton.Click += async (_, _) =>
        {
            _searchBox.Clear();
            await RefreshAsync();
        };

        _topUpButton.Click += async (_, _) => await TopUpAsync();
        _createButton.Click += async (_, _) => await CreateCustomerAsync();
        _editButton.Click += async (_, _) => await EditCustomerAsync();
        _toggleButton.Click += async (_, _) => await ToggleCustomerAsync();
        _resetButton.Click += async (_, _) => await ResetPasswordAsync();
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetCustomersAsync(_searchBox.Text);
        SetCards(
            rows,
            customer =>
            {
                var tierText = customer.MembershipTier?.ToLowerInvariant() switch
                {
                    "vip" => "Hạng VIP",
                    "diamond" => "Hạng Kim Cương",
                    "standard" or "regular" => "Hạng Chuẩn",
                    _ => string.IsNullOrWhiteSpace(customer.MembershipTier) ? "Hạng Chuẩn" : customer.MembershipTier
                };

                return new AdminCardModel(
                    customer.FullName,
                    $"@{customer.Username} | SĐT: {customer.Phone}",
                    $"Số dư ví: {customer.Balance:N0} đ\nĐiểm thưởng: {customer.LoyaltyPoints:N0}",
                    customer.IsActive ? $"Đang hoạt động · {tierText}" : "Đã khóa tài khoản",
                    customer.IsActive ? CyberTheme.Success : CyberTheme.Danger);
            },
            $"{rows.Count} hội viên");
    }

    private async Task TopUpAsync()
    {
        if (RequiredContext.CurrentEmployeeId <= 0)
        {
            ReportError(new InvalidOperationException("Vai trò hiện tại chưa gắn mã nhân viên; không thể ghi nhận nạp ví."));
            return;
        }

        var customer = SelectedCustomer;
        if (customer is null)
        {
            MessageBox.Show(FindForm(), "Vui lòng click chọn một thẻ hội viên trong danh sách bên dưới để nạp tiền!", "Chưa chọn hội viên", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!customer.IsActive)
        {
            MessageBox.Show(FindForm(), "Hội viên đang bị khóa, không thể nạp tiền.", "Tài khoản bị khóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new TopUpDialog(customer);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var newBalance = await RequiredContext.Repository.TopUpAsync(
                customer.CustomerId,
                RequiredContext.CurrentEmployeeId,
                dialog.Amount,
                dialog.PaymentMethod,
                string.IsNullOrWhiteSpace(dialog.Note) ? "admin_ui_topup" : dialog.Note);

            MessageBox.Show(FindForm(),
                $"Nạp thành công {dialog.Amount:N0} đ cho hội viên {customer.FullName} (@{customer.Username}).\nSố dư mới: {newBalance:N0} đ",
                "Nạp tiền thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private NetCafe.Data.CustomerItem? SelectedCustomer => SelectedItem as NetCafe.Data.CustomerItem;

    private async Task CreateCustomerAsync()
    {
        if (!RequiredContext.CanManageCustomers) return;
        using var dialog = new CustomerEditorDialog(null);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            await RequiredContext.Repository.CreateCustomerAsync(dialog.FullName, dialog.Phone, dialog.Email, dialog.Username, dialog.Password);
            await RefreshAsync();
        }
        catch (Exception exception) { ReportError(exception); }
    }

    private async Task EditCustomerAsync()
    {
        if (!RequiredContext.CanManageCustomers) return;
        if (SelectedCustomer is null) return;
        using var dialog = new CustomerEditorDialog(SelectedCustomer);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            await RequiredContext.Repository.UpdateCustomerAsync(SelectedCustomer.CustomerId, dialog.FullName, dialog.Phone, dialog.Email);
            await RefreshAsync();
        }
        catch (Exception exception) { ReportError(exception); }
    }

    private async Task ToggleCustomerAsync()
    {
        if (!RequiredContext.CanManageCustomers) return;
        if (SelectedCustomer is null) return;
        try
        {
            await RequiredContext.Repository.SetCustomerActiveAsync(SelectedCustomer.CustomerId, !SelectedCustomer.IsActive);
            await RefreshAsync();
        }
        catch (Exception exception) { ReportError(exception); }
    }

    private async Task ResetPasswordAsync()
    {
        if (!RequiredContext.CanManageCustomers) return;
        if (SelectedCustomer is null) return;
        using var dialog = new PasswordDialog();
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            await RequiredContext.Repository.ResetCustomerPasswordAsync(SelectedCustomer.CustomerId, dialog.Password);
        }
        catch (Exception exception) { ReportError(exception); }
    }

    private static void ConfigureActionButton(Button button, string text, Color color)
    {
        button.Text = text;
        button.Width = text.Length > 6 ? 95 : 78;
        button.Height = 32;
        CyberTheme.StyleButton(button, color);



        button.Margin = new Padding(0, 4, 6, 4);
    }
}

internal sealed class CustomerEditorDialog : Form
{
    private readonly TextBox _fullName = new();
    private readonly TextBox _phone = new();
    private readonly TextBox _email = new();
    private readonly TextBox _username = new();
    private readonly TextBox _password = new();
    public string FullName => _fullName.Text.Trim();
    public string Phone => _phone.Text.Trim();
    public string Email => _email.Text.Trim();
    public string Username => _username.Text.Trim();
    public string Password => _password.Text;

    public CustomerEditorDialog(NetCafe.Data.CustomerItem? customer)
    {
        Text = customer is null ? "Thêm hội viên" : "Chỉnh sửa hội viên";
        Width = 380;
        Height = customer is null ? 330 : 260;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = CyberTheme.Background;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = customer is null ? 6 : 4 };
        AddField(layout, "Họ và tên", _fullName, 0);
        AddField(layout, "Số điện thoại", _phone, 1);
        AddField(layout, "Email", _email, 2);
        if (customer is null)
        {
            AddField(layout, "Tên đăng nhập", _username, 3);
            AddField(layout, "Mật khẩu", _password, 4);
            _password.UseSystemPasswordChar = true;
        }
        var save = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        CyberTheme.StyleButton(save, CyberTheme.Primary);
        layout.Controls.Add(save, 1, layout.RowCount - 1);
        Controls.Add(layout);
        AcceptButton = save;
        if (customer is not null)
        {
            _fullName.Text = customer.FullName;
            _phone.Text = customer.Phone;
            _email.Text = customer.Email;
            _username.ReadOnly = true;
        }
    }

    private static void AddField(TableLayoutPanel layout, string label, TextBox box, int row)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        layout.Controls.Add(box, 1, row);
        box.Dock = DockStyle.Fill;
    }
}

internal sealed class PasswordDialog : Form
{
    private readonly TextBox _password = new() { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
    public string Password => _password.Text;
    public PasswordDialog()
    {
        Text = "Đặt mật khẩu mới";
        Width = 330;
        Height = 130;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = CyberTheme.Background;
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 2 };
        panel.Controls.Add(new Label { Text = "Mật khẩu", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        panel.Controls.Add(_password, 1, 0);
        var save = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        CyberTheme.StyleButton(save, CyberTheme.Primary);
        panel.Controls.Add(save, 1, 1);
        Controls.Add(panel);
        AcceptButton = save;
    }
}

internal sealed class TopUpDialog : Form
{
    private readonly NumericUpDown _amountBox = new();
    private readonly ComboBox _methodCombo = new();
    private readonly TextBox _noteBox = new();

    public decimal Amount => _amountBox.Value;
    public string PaymentMethod => _methodCombo.SelectedItem?.ToString() == "Chuyển khoản (transfer)" ? "transfer" : "cash";
    public string Note => _noteBox.Text.Trim();

    public TopUpDialog(NetCafe.Data.CustomerItem customer)
    {
        Text = $"Nạp tiền ví - {customer.FullName}";
        Width = 450;
        Height = 440;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = CyberTheme.Background;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var infoCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12)
        };
        var nameLabel = new Label
        {
            Text = $"Hội viên: {customer.FullName} (@{customer.Username})",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = CyberTheme.Primary,
            Dock = DockStyle.Top,
            Height = 24
        };
        var balanceLabel = new Label
        {
            Text = $"Số dư ví hiện tại: {customer.Balance:N0} đ | SĐT: {customer.Phone}",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = CyberTheme.MutedText,
            Dock = DockStyle.Top,
            Height = 24
        };
        infoCard.Controls.Add(balanceLabel);
        infoCard.Controls.Add(nameLabel);

        var amountLabel = new Label
        {
            Text = "Số tiền nạp vào ví (VND):",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Location = new Point(16, 100),
            AutoSize = true
        };

        _amountBox.Location = new Point(16, 126);
        _amountBox.Width = 400;
        _amountBox.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _amountBox.Maximum = 50000000;
        _amountBox.Increment = 10000;
        _amountBox.ThousandsSeparator = true;
        _amountBox.Value = 50000;

        var quickButtonsPanel = new FlowLayoutPanel
        {
            Location = new Point(16, 168),
            Size = new Size(400, 40),
            WrapContents = false
        };
        int[] quickAmounts = { 20000, 50000, 100000, 200000, 500000 };
        foreach (var amt in quickAmounts)
        {
            var btn = new Button
            {
                Text = $"+{amt / 1000}k",
                Width = 72,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = CyberTheme.Primary,
                Margin = new Padding(0, 0, 8, 0)
            };
            btn.FlatAppearance.BorderColor = CyberTheme.Border;
            btn.Click += (_, _) => _amountBox.Value = amt;
            quickButtonsPanel.Controls.Add(btn);
        }

        var methodLabel = new Label
        {
            Text = "Phương thức thanh toán:",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Location = new Point(16, 218),
            AutoSize = true
        };
        _methodCombo.Location = new Point(16, 240);
        _methodCombo.Width = 400;
        _methodCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _methodCombo.Items.AddRange(new object[] { "Tiền mặt (cash)", "Chuyển khoản (transfer)" });
        _methodCombo.SelectedIndex = 0;

        var noteLabel = new Label
        {
            Text = "Ghi chú giao dịch:",
            Location = new Point(16, 275),
            AutoSize = true
        };
        _noteBox.Location = new Point(16, 298);
        _noteBox.Width = 400;
        _noteBox.PlaceholderText = "Nhập mã tham chiếu hoặc ghi chú nếu có...";

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft
        };
        var cancelBtn = new Button { Text = "Hủy bỏ", Width = 95, Height = 36, DialogResult = DialogResult.Cancel };
        var okBtn = new Button
        {
            Text = "Xác nhận nạp",
            Width = 130,
            Height = 36,
            DialogResult = DialogResult.OK,
            BackColor = CyberTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        okBtn.FlatAppearance.BorderSize = 0;
        btnPanel.Controls.Add(cancelBtn);
        btnPanel.Controls.Add(okBtn);

        root.Controls.Add(btnPanel);
        root.Controls.Add(_noteBox);
        root.Controls.Add(noteLabel);
        root.Controls.Add(_methodCombo);
        root.Controls.Add(methodLabel);
        root.Controls.Add(quickButtonsPanel);
        root.Controls.Add(_amountBox);
        root.Controls.Add(amountLabel);
        root.Controls.Add(infoCard);
        Controls.Add(root);
        AcceptButton = okBtn;
        CancelButton = cancelBtn;
    }
}
