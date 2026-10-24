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
        SetTitle("Quản lý Hội viên · Ví tài khoản", "Tìm kiếm theo tài khoản/tên/SĐT; nạp tiền qua hàm CSDL có tự động ghi nhận thanh toán.");

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
        _topUpButton.BackColor = CyberTheme.Success;
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
        CardDoubleClicked += async (_, _) => await EditCustomerAsync();
    }

    public override void SetContext(AdminViewContext context)
    {
        base.SetContext(context);
        UpdateRolePermissions(context);
    }

    private void UpdateRolePermissions(AdminViewContext context)
    {
        if (context.IsAdmin || context.IsManager)
        {
            _topUpButton.Enabled = true;
            _createButton.Enabled = true;
            _editButton.Enabled = true;
            _toggleButton.Enabled = true;
            _resetButton.Enabled = true;
        }
        else if (context.IsCashier)
        {
            _topUpButton.Enabled = true;
            _createButton.Enabled = true;
            _editButton.Enabled = false;
            _toggleButton.Enabled = false;
            _resetButton.Enabled = false;
        }
        else // Kỹ thuật viên: Cấm truy cập ví
        {
            _topUpButton.Enabled = false;
            _createButton.Enabled = false;
            _editButton.Enabled = false;
            _toggleButton.Enabled = false;
            _resetButton.Enabled = false;
        }
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetCustomersAsync(_searchBox.Text);
        var isTech = RequiredContext.IsTechnician;
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

                var walletText = isTech ? "Số dư ví: 🔒 Cấm truy cập ví" : $"Số dư ví: {customer.Balance:N0} đ";
                return new AdminCardModel(
                    customer.FullName,
                    $"@{customer.Username} | SĐT: {customer.Phone}",
                    $"{walletText}\nĐiểm thưởng: {customer.LoyaltyPoints:N0}",
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

        if (RequiredContext.IsTechnician)
        {
            MessageBox.Show(
                FindForm(),
                "Vai trò Kỹ thuật viên không có quyền truy cập ví hoặc nạp tiền (Cơ chế CSDL: billing.sp_top_up).",
                "Từ chối quyền truy cập ví",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
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
        var customer = SelectedCustomer;
        using var dialog = new CustomerEditorDialog(customer);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            if (dialog.ShouldTopUp)
            {
                if (RequiredContext.CurrentEmployeeId <= 0)
                {
                    ReportError(new InvalidOperationException("Vai trò hiện tại chưa gắn mã nhân viên; không thể ghi nhận nạp ví."));
                    return;
                }

                var newBalance = await RequiredContext.Repository.UpdateCustomerAndTopUpAsync(
                    customer.CustomerId,
                    dialog.FullName,
                    dialog.Phone,
                    dialog.Email,
                    RequiredContext.CurrentEmployeeId,
                    dialog.TopUpAmount,
                    dialog.PaymentMethod,
                    string.IsNullOrWhiteSpace(dialog.TopUpNote) ? "admin_ui_customer_edit_topup" : dialog.TopUpNote);
                MessageBox.Show(
                    FindForm(),
                    $"Đã lưu thông tin và nạp {dialog.TopUpAmount:N0} đ cho {dialog.FullName}.\nSố dư mới: {newBalance:N0} đ",
                    "Cập nhật hội viên thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                await RequiredContext.Repository.UpdateCustomerAsync(customer.CustomerId, dialog.FullName, dialog.Phone, dialog.Email);
            }
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
    private readonly NumericUpDown _topUpAmount = new();
    private readonly ComboBox _paymentMethod = new();
    private readonly TextBox _topUpNote = new();

    public string FullName => _fullName.Text.Trim();
    public string Phone => _phone.Text.Trim();
    public string Email => _email.Text.Trim();
    public string Username => _username.Text.Trim();
    public string Password => _password.Text;
    public bool ShouldTopUp { get; private set; }
    public decimal TopUpAmount => _topUpAmount.Value;
    public string PaymentMethod => _paymentMethod.SelectedItem?.ToString() == "Chuyển khoản (transfer)" ? "transfer" : "cash";
    public string TopUpNote => _topUpNote.Text.Trim();

    public CustomerEditorDialog(NetCafe.Data.CustomerItem? customer)
    {
        Text = customer is null ? "Thêm hội viên" : $"Chỉnh sửa hội viên · {customer.FullName}";
        ClientSize = customer is null ? new Size(430, 350) : new Size(560, 570);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;
        if (customer is not null)
        {
            _fullName.Text = customer.FullName;
            _phone.Text = customer.Phone;
            _email.Text = customer.Email;
            _username.ReadOnly = true;
        }

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(12, 6),
            BackColor = CyberTheme.Background,
            ForeColor = CyberTheme.Text
        };
        var infoTab = new TabPage("Thông tin hội viên")
        {
            BackColor = CyberTheme.Background,
            ForeColor = CyberTheme.Text,
            Padding = new Padding(4)
        };
        var saveButton = BuildInfoTab(infoTab, customer);
        tabs.TabPages.Add(infoTab);

        if (customer is not null)
        {
            var topUpTab = new TabPage("Nạp tiền")
            {
                BackColor = CyberTheme.Background,
                ForeColor = CyberTheme.Text,
                Padding = new Padding(4)
            };
            var topUpButton = BuildTopUpTab(topUpTab, customer);
            tabs.TabPages.Insert(0, topUpTab);
            tabs.SelectedIndex = 0;
            AcceptButton = topUpButton;
            tabs.SelectedIndexChanged += (_, _) => AcceptButton = tabs.SelectedIndex == 0 ? topUpButton : saveButton;
        }
        else
        {
            AcceptButton = saveButton;
        }

        Controls.Add(tabs);
        CancelButton = null;
    }

    private Button BuildInfoTab(TabPage tab, NetCafe.Data.CustomerItem? customer)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = customer is null ? 6 : 5,
            BackColor = CyberTheme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, customer is null ? 112 : 132));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        var row = 0;
        AddField(layout, "Họ và tên", _fullName, row++);
        AddField(layout, "Số điện thoại", _phone, row++);
        AddField(layout, "Email", _email, row++);
        if (customer is null)
        {
            AddField(layout, "Tên đăng nhập", _username, row++);
            AddField(layout, "Mật khẩu", _password, row++);
            _password.UseSystemPasswordChar = true;
        }

        var actionPanel = CreateActionPanel();
        var save = new Button
        {
            Text = customer is null ? "Lưu hội viên" : "Lưu thông tin",
            Width = customer is null ? 132 : 126,
            Height = 34
        };
        CyberTheme.StyleButton(save, CyberTheme.Primary);
        save.Click += (_, _) =>
        {
            ShouldTopUp = false;
            DialogResult = DialogResult.OK;
        };
        actionPanel.Controls.Add(save);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.Controls.Add(actionPanel, 0, row);
        layout.SetColumnSpan(actionPanel, 2);
        tab.Controls.Add(layout);
        return save;
    }

    private Button BuildTopUpTab(TabPage tab, NetCafe.Data.CustomerItem customer)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = CyberTheme.Background
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.Controls.Add(CreateWalletPanel(customer), 0, 0);

        var actionPanel = CreateActionPanel();
        var confirm = new Button { Text = "Xác nhận nạp tiền", Width = 150, Height = 34 };
        CyberTheme.StyleButton(confirm, CyberTheme.Success);
        confirm.Click += (_, _) =>
        {
            if (_topUpAmount.Value <= 0)
            {
                MessageBox.Show(this, "Nhập số tiền cần nạp trước khi xác nhận.", "Thiếu số tiền", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!customer.IsActive)
            {
                MessageBox.Show(this, "Hội viên đang bị khóa, không thể nạp tiền.", "Tài khoản bị khóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShouldTopUp = true;
            DialogResult = DialogResult.OK;
        };
        actionPanel.Controls.Add(confirm);
        layout.Controls.Add(actionPanel, 0, 1);
        tab.Controls.Add(layout);
        return confirm;
    }

    private FlowLayoutPanel CreateActionPanel()
    {
        var cancel = new Button { Text = "Hủy", Width = 82, Height = 34, DialogResult = DialogResult.Cancel };
        CyberTheme.StyleButton(cancel, CyberTheme.SurfaceRaised);
        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };
        actionPanel.Controls.Add(cancel);
        return actionPanel;
    }

    private GroupBox CreateWalletPanel(NetCafe.Data.CustomerItem customer)
    {
        var walletGroup = new GroupBox
        {
            Text = "Ví hội viên",
            Dock = DockStyle.Fill,
            ForeColor = CyberTheme.Primary,
            Padding = new Padding(10),
            BackColor = CyberTheme.Surface
        };
        var walletLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            BackColor = CyberTheme.Surface
        };
        walletLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 156));
        walletLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        walletLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        walletLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        walletLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        walletLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        walletLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var balanceLabel = new Label
        {
            Text = $"Số dư hiện tại: {customer.Balance:N0} đ  ·  @{customer.Username}",
            Dock = DockStyle.Fill,
            ForeColor = CyberTheme.Text,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        walletLayout.Controls.Add(balanceLabel, 0, 0);
        walletLayout.SetColumnSpan(balanceLabel, 2);

        _topUpAmount.Minimum = 0;
        _topUpAmount.Maximum = 50000000;
        _topUpAmount.Increment = 10000;
        _topUpAmount.ThousandsSeparator = true;
        _topUpAmount.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _topUpAmount.Dock = DockStyle.Fill;
        _topUpAmount.Margin = new Padding(0, 2, 0, 2);
        AddWalletField(walletLayout, "Số tiền nạp (VND)", _topUpAmount, 1);

        _paymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        _paymentMethod.Items.AddRange(new object[] { "Tiền mặt (cash)", "Chuyển khoản (transfer)" });
        _paymentMethod.SelectedIndex = 0;
        _paymentMethod.Dock = DockStyle.Fill;
        _paymentMethod.Margin = new Padding(0, 2, 0, 2);
        AddWalletField(walletLayout, "Phương thức", _paymentMethod, 2);

        _topUpNote.PlaceholderText = "Mã tham chiếu hoặc ghi chú giao dịch";
        _topUpNote.Dock = DockStyle.Fill;
        _topUpNote.Margin = new Padding(0, 2, 0, 2);
        AddWalletField(walletLayout, "Ghi chú", _topUpNote, 3);

        var quickButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 2, 0, 0)
        };
        foreach (var amount in new[] { 20000, 50000, 100000, 200000, 500000 })
        {
            var button = new Button
            {
                Text = $"+{amount / 1000}k",
                Width = 70,
                Height = 30,
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = CyberTheme.SurfaceRaised,
                ForeColor = CyberTheme.Primary
            };
            button.FlatAppearance.BorderColor = CyberTheme.Border;
            button.Click += (_, _) => _topUpAmount.Value = amount;
            quickButtons.Controls.Add(button);
        }
        walletLayout.Controls.Add(quickButtons, 0, 4);
        walletLayout.SetColumnSpan(quickButtons, 2);
        walletGroup.Controls.Add(walletLayout);
        return walletGroup;
    }

    private static void AddWalletField(TableLayoutPanel layout, string label, Control control, int row)
    {
        var labelControl = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = CyberTheme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(labelControl, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void AddField(TableLayoutPanel layout, string label, TextBox box, int row)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = CyberTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        layout.Controls.Add(box, 1, row);
        box.Dock = DockStyle.Fill;
        box.BackColor = CyberTheme.SurfaceRaised;
        box.ForeColor = CyberTheme.Text;
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
        ForeColor = CyberTheme.Text;
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
        ForeColor = CyberTheme.Text;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var infoCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = CyberTheme.Surface,
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
                BackColor = CyberTheme.Surface,
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
