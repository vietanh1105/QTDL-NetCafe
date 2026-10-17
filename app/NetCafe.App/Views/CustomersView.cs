// Trách nhiệm: danh sách hội viên và điểm nối cho CRUD/top-up Admin.
// Liên kết: core.customer, billing.sp_top_up và DbRepository.GetCustomersAsync.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public partial class CustomersView : AdminDataViewBase
{
    private readonly TextBox _searchBox = new();
    private readonly TextBox _topUpBox = new();
    private readonly Button _topUpButton = new();
    private readonly Button _createButton = new();
    private readonly Button _editButton = new();
    private readonly Button _toggleButton = new();
    private readonly Button _resetButton = new();

    public CustomersView()
    {
        InitializeComponent();
        SetTitle("Hội viên và ví", "Tìm kiếm không dấu; top-up dùng function database và ghi payment.");
        _searchBox.Width = 190;
        _searchBox.PlaceholderText = "Tên / SĐT";
        _topUpBox.Width = 100;
        _topUpBox.Text = "50000";
        _topUpButton.Text = "Nạp ví";
        _topUpButton.Width = 80;
        _topUpButton.BackColor = Color.FromArgb(16, 185, 129);
        _topUpButton.ForeColor = Color.White;
        _topUpButton.FlatStyle = FlatStyle.Flat;
        _topUpButton.FlatAppearance.BorderSize = 0;
        ConfigureActionButton(_createButton, "Thêm", Color.FromArgb(37, 99, 235));
        ConfigureActionButton(_editButton, "Sửa", Color.FromArgb(71, 85, 105));
        ConfigureActionButton(_toggleButton, "Bật/tắt", Color.FromArgb(234, 88, 12));
        ConfigureActionButton(_resetButton, "Đổi mật khẩu", Color.FromArgb(124, 58, 237));
        Toolbar.Controls.Add(_resetButton);
        Toolbar.Controls.Add(_toggleButton);
        Toolbar.Controls.Add(_editButton);
        Toolbar.Controls.Add(_createButton);
        Toolbar.Controls.Add(_topUpButton);
        Toolbar.Controls.Add(_topUpBox);
        Toolbar.Controls.Add(_searchBox);
        _searchBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) await RefreshAsync();
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
        SetRows(rows, $"{rows.Count} hội viên");
    }

    private async Task TopUpAsync()
    {
        if (RequiredContext.CurrentEmployeeId <= 0)
        {
            ReportError(new InvalidOperationException("Role hiện tại chưa ánh xạ employee_id; không thể ghi nhận nạp ví."));
            return;
        }

        if (Grid.CurrentRow?.DataBoundItem is not NetCafe.Data.CustomerItem customer ||
            !decimal.TryParse(_topUpBox.Text, out var amount) || amount <= 0)
        {
            return;
        }
        try
        {
            await RequiredContext.Repository.TopUpAsync(customer.CustomerId, RequiredContext.CurrentEmployeeId, amount);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private NetCafe.Data.CustomerItem? SelectedCustomer => Grid.CurrentRow?.DataBoundItem as NetCafe.Data.CustomerItem;

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
        button.Width = text.Length > 5 ? 105 : 72;
        button.BackColor = color;
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
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
        Text = customer is null ? "Thêm hội viên" : "Sửa hội viên";
        Width = 380;
        Height = customer is null ? 330 : 260;
        StartPosition = FormStartPosition.CenterParent;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = customer is null ? 6 : 4 };
        AddField(layout, "Họ tên", _fullName, 0);
        AddField(layout, "Điện thoại", _phone, 1);
        AddField(layout, "Email", _email, 2);
        if (customer is null)
        {
            AddField(layout, "Username", _username, 3);
            AddField(layout, "Mật khẩu", _password, 4);
            _password.UseSystemPasswordChar = true;
        }
        var save = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
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
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 2 };
        panel.Controls.Add(new Label { Text = "Mật khẩu", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        panel.Controls.Add(_password, 1, 0);
        var save = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        panel.Controls.Add(save, 1, 1);
        Controls.Add(panel);
        AcceptButton = save;
    }
}
