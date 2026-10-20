// Trách nhiệm: quản lý nhân viên, mapping database và quyền xem dữ liệu lương.
// Liên kết: hr.employee, các function hr.sp_* trong sql/10_admin_operations.sql và AdminViewContext.

using System.Drawing;
using System.Windows.Forms;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class EmployeesView : AdminCardViewBase
{
    private readonly Button _salaryButton = new();
    private readonly Button _createButton = new();
    private readonly Button _editButton = new();
    private readonly Button _toggleButton = new();
    private readonly Button _resetPasswordButton = new();
    private bool _showSalary;

    public EmployeesView()
    {
        InitializeComponent();
        SetTitle("Nhân viên & Phân quyền", "Lương chỉ hiển thị khi PostgreSQL cho phép; Cashier/Technician không đọc được cột salary.");
        ConfigureButton(_salaryButton, "👁 Xem bảng lương", CyberTheme.Warning, 124);
        ConfigureButton(_createButton, "+ Thêm nhân viên", CyberTheme.Primary, 112);
        ConfigureButton(_editButton, "Chỉnh sửa", CyberTheme.SurfaceRaised, 86);
        ConfigureButton(_toggleButton, "Khóa/Mở", CyberTheme.Danger, 78);
        ConfigureButton(_resetPasswordButton, "Đổi mật khẩu", Color.FromArgb(124, 58, 237), 98);
        Toolbar.Controls.Add(_resetPasswordButton);
        Toolbar.Controls.Add(_toggleButton);
        Toolbar.Controls.Add(_editButton);
        Toolbar.Controls.Add(_createButton);
        Toolbar.Controls.Add(_salaryButton);
        _salaryButton.Click += async (_, _) => await ToggleSalaryAsync();
        _createButton.Click += async (_, _) => await CreateEmployeeAsync();
        _editButton.Click += async (_, _) => await EditEmployeeAsync();
        _toggleButton.Click += async (_, _) => await ToggleEmployeeAsync();
        _resetPasswordButton.Click += async (_, _) => await ResetPasswordAsync();
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetEmployeesAsync();
        SetCards(rows, BuildCard, $"{rows.Count} nhân viên đang hoạt động");
    }

    private AdminCardModel BuildCard(EmployeeItem employee)
    {
        var roleText = employee.EmpRole.ToLowerInvariant() switch
        {
            "admin" => "Quản trị viên",
            "manager" => "Quản lý",
            "cashier" => "Thu ngân",
            "staff" => "Nhân viên",
            "technician" => "Kỹ thuật viên",
            _ => employee.EmpRole
        };
        var salaryText = _showSalary && employee.Salary.HasValue
            ? $"\nLương: {employee.Salary.Value:N0} đ"
            : string.Empty;
        return new AdminCardModel(
            employee.FullName,
            $"{roleText} · @{employee.Username}",
            $"SĐT: {employee.Phone}\nCa: {employee.DefaultShift}\nDB: {employee.DbUsername ?? "Chưa gắn"}{salaryText}",
            employee.IsActive ? "Đang hoạt động" : "Đã khóa",
            employee.IsActive ? CyberTheme.Success : CyberTheme.Danger);
    }

    private async Task ToggleSalaryAsync()
    {
        if (!_showSalary)
        {
            try
            {
                var rows = await RequiredContext.Repository.GetEmployeesWithSalaryAsync();
                _showSalary = true;
                SetCards(rows, BuildCard, $"Đã tải lương {rows.Count} nhân viên theo quyền PostgreSQL.");
            }
            catch (Exception exception)
            {
                ReportError(exception);
            }
            return;
        }

        _showSalary = false;
        await RefreshAsync();
    }

    private EmployeeItem? SelectedEmployee => SelectedItem as EmployeeItem;

    private async Task CreateEmployeeAsync()
    {
        using var dialog = new EmployeeEditorDialog(null);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }
        try
        {
            await RequiredContext.Repository.CreateEmployeeAsync(
                dialog.FullName, dialog.Phone, dialog.Username, dialog.DbUsername,
                dialog.Password, dialog.Role, dialog.Shift, dialog.Salary);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task EditEmployeeAsync()
    {
        if (SelectedEmployee is null)
        {
            return;
        }
        using var dialog = new EmployeeEditorDialog(SelectedEmployee);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }
        try
        {
            var employee = SelectedEmployee;
            employee.FullName = dialog.FullName;
            employee.Phone = dialog.Phone;
            employee.DbUsername = dialog.DbUsername;
            employee.EmpRole = dialog.Role;
            employee.DefaultShift = dialog.Shift;
            employee.Salary = dialog.Salary;
            await RequiredContext.Repository.UpdateEmployeeAsync(employee);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task ToggleEmployeeAsync()
    {
        if (SelectedEmployee is null)
        {
            return;
        }
        try
        {
            await RequiredContext.Repository.SetEmployeeActiveAsync(SelectedEmployee.EmployeeId, !SelectedEmployee.IsActive);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task ResetPasswordAsync()
    {
        if (SelectedEmployee is null)
        {
            return;
        }
        using var dialog = new EmployeePasswordDialog();
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }
        try
        {
            await RequiredContext.Repository.ResetEmployeePasswordAsync(SelectedEmployee.EmployeeId, dialog.Password);
            MessageBox.Show(FindForm(), "Đã cập nhật mật khẩu bằng hash PostgreSQL.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private static void ConfigureButton(Button button, string text, Color color, int width)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 30;
        button.Margin = new Padding(3, 0, 3, 0);
        CyberTheme.StyleButton(button, color);
    }
}

internal sealed class EmployeeEditorDialog : Form
{
    private readonly TextBox _fullNameBox = new();
    private readonly TextBox _phoneBox = new();
    private readonly TextBox _usernameBox = new();
    private readonly TextBox _dbUsernameBox = new();
    private readonly TextBox _passwordBox = new() { UseSystemPasswordChar = true };
    private readonly ComboBox _roleBox = new();
    private readonly ComboBox _shiftBox = new();
    private readonly NumericUpDown _salaryBox = new();

    public string FullName => _fullNameBox.Text.Trim();
    public string Phone => _phoneBox.Text.Trim();
    public string Username => _usernameBox.Text.Trim();
    public string DbUsername => _dbUsernameBox.Text.Trim();
    public string Password => _passwordBox.Text;
    public string Role => _roleBox.SelectedItem?.ToString() ?? "staff";
    public string Shift => _shiftBox.SelectedItem?.ToString() ?? "morning";
    public decimal Salary => _salaryBox.Value;

    public EmployeeEditorDialog(EmployeeItem? employee)
    {
        Text = employee is null ? "Thêm nhân viên" : "Chỉnh sửa nhân viên";
        Width = 460;
        Height = employee is null ? 430 : 380;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _roleBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _roleBox.Items.AddRange(new object[] { "admin", "manager", "cashier", "staff", "technician" });
        _roleBox.SelectedItem = employee?.EmpRole ?? "staff";
        _shiftBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _shiftBox.Items.AddRange(new object[] { "morning", "afternoon", "night" });
        _shiftBox.SelectedItem = employee?.DefaultShift ?? "morning";
        _salaryBox.Maximum = 1000000000;
        _salaryBox.Increment = 100000;
        _salaryBox.ThousandsSeparator = true;
        _salaryBox.Value = Math.Min(_salaryBox.Maximum, employee?.Salary ?? 0);
        _usernameBox.ReadOnly = employee is not null;
        if (employee is not null)
        {
            _fullNameBox.Text = employee.FullName;
            _phoneBox.Text = employee.Phone;
            _usernameBox.Text = employee.Username;
            _dbUsernameBox.Text = employee.DbUsername;
        }

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = employee is null ? 9 : 8 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
        AddField(layout, "Họ tên", _fullNameBox, 0);
        AddField(layout, "Số điện thoại", _phoneBox, 1);
        AddField(layout, "Username", _usernameBox, 2);
        AddField(layout, "DB username", _dbUsernameBox, 3);
        AddField(layout, "Vai trò", _roleBox, 4);
        AddField(layout, "Ca", _shiftBox, 5);
        AddField(layout, "Lương", _salaryBox, 6);
        var row = 7;
        if (employee is null)
        {
            AddField(layout, "Mật khẩu", _passwordBox, row);
            row++;
        }
        var save = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        CyberTheme.StyleButton(save, CyberTheme.Primary);
        layout.Controls.Add(save, 1, row);
        Controls.Add(layout);
        AcceptButton = save;
    }

    private static void AddField(TableLayoutPanel layout, string label, Control control, int row)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        layout.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(control, 1, row);
    }
}

internal sealed class EmployeePasswordDialog : Form
{
    private readonly TextBox _passwordBox = new() { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
    public string Password => _passwordBox.Text;

    public EmployeePasswordDialog()
    {
        Text = "Đổi mật khẩu nhân viên";
        Width = 360;
        Height = 150;
        StartPosition = FormStartPosition.CenterParent;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 2 };
        layout.Controls.Add(new Label { Text = "Mật khẩu mới", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        layout.Controls.Add(_passwordBox, 1, 0);
        var save = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        CyberTheme.StyleButton(save, CyberTheme.Primary);
        layout.Controls.Add(save, 1, 1);
        Controls.Add(layout);
        AcceptButton = save;
    }
}
