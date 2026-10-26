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
    protected override int CardHeight => _showSalary ? 215 : 160;

    public EmployeesView()
    {
        InitializeComponent();
        SetTitle("Nhân viên & Phân quyền", "Lương chỉ hiển thị khi PostgreSQL cho phép; Cashier/Technician không đọc được cột salary.");
        ConfigureButton(_salaryButton, "👁️ Xem bảng lương & Thông tin nhạy cảm", CyberTheme.Warning, 250);
        ConfigureButton(_createButton, "+ Thêm NV / Phân quyền", CyberTheme.Primary, 165);
        ConfigureButton(_editButton, "Chỉnh sửa", CyberTheme.SurfaceRaised, 86);
        ConfigureButton(_toggleButton, "Khóa/Mở", CyberTheme.Danger, 78);
        ConfigureButton(_resetPasswordButton, "Đổi mật khẩu", CyberTheme.SurfaceRaised, 98);
        ActionsPanel.Controls.Add(_salaryButton);
        ActionsPanel.Controls.Add(_createButton);
        ActionsPanel.Controls.Add(_editButton);
        ActionsPanel.Controls.Add(_toggleButton);
        ActionsPanel.Controls.Add(_resetPasswordButton);
        _salaryButton.Click += async (_, _) => await ToggleSalaryAsync();
        _createButton.Click += async (_, _) => await CreateEmployeeAsync();
        _editButton.Click += async (_, _) => await EditEmployeeAsync();
        _toggleButton.Click += async (_, _) => await ToggleEmployeeAsync();
        _resetPasswordButton.Click += async (_, _) => await ResetPasswordAsync();
        CardDoubleClicked += async (_, _) => await EditEmployeeAsync();
    }

    public override void SetContext(AdminViewContext context)
    {
        base.SetContext(context);
        UpdateRolePermissions(context);
    }

    private void UpdateRolePermissions(AdminViewContext context)
    {
        if (_showSalary && !context.IsAdmin)
        {
            _showSalary = false;
        }

        var canManage = context.IsAdmin || context.IsManager;
        _createButton.Enabled = canManage;
        _editButton.Enabled = canManage;
        _toggleButton.Enabled = canManage;
        _resetPasswordButton.Enabled = context.IsAdmin;

        if (context.IsAdmin)
        {
            _salaryButton.Visible = true;
            _salaryButton.Enabled = true;
            _salaryButton.Text = _showSalary ? "🙈 Ẩn thông tin lương" : "👁️ Xem bảng lương & Thông tin nhạy cảm";
            CyberTheme.StyleButton(_salaryButton, _showSalary ? CyberTheme.SurfaceRaised : CyberTheme.Warning);
        }
        else
        {
            _salaryButton.Visible = true;
            _salaryButton.Enabled = false;
            _salaryButton.Text = "🔒 Bảng lương (Chỉ Chủ quán / DBA)";
            CyberTheme.StyleButton(_salaryButton, CyberTheme.SurfaceRaised);
        }
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
            ? $"\n💰 Lương cơ bản: {employee.Salary.Value:N0} đ"
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
                _salaryButton.Text = "🙈 Ẩn thông tin lương";
                CyberTheme.StyleButton(_salaryButton, CyberTheme.SurfaceRaised);
                SetCards(rows, BuildCard, $"Đã tải lương {rows.Count} nhân viên theo quyền PostgreSQL (role_admin).");

                using var salaryDialog = new EmployeeSalaryDialog(rows);
                salaryDialog.ShowDialog(FindForm());
            }
            catch (Exception exception)
            {
                ReportError(exception);
                MessageBox.Show(
                    FindForm(),
                    $"PostgreSQL đã chặn truy cập thông tin bảng lương:\n\n{exception.Message}\n\nQuy tắc bảo mật: Chỉ tài khoản thuộc nhóm role_admin mới có quyền đọc cột lương (salary) theo cơ chế Column-Level Security.",
                    "Từ chối quyền truy cập CSDL (42501)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            return;
        }

        _showSalary = false;
        _salaryButton.Text = "👁️ Xem bảng lương & Thông tin nhạy cảm";
        CyberTheme.StyleButton(_salaryButton, CyberTheme.Warning);
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
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;
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
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 2 };
        layout.Controls.Add(new Label { Text = "Mật khẩu mới", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        layout.Controls.Add(_passwordBox, 1, 0);
        var savePwd = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        CyberTheme.StyleButton(savePwd, CyberTheme.Primary);
        layout.Controls.Add(savePwd, 1, 1);
        Controls.Add(layout);
        AcceptButton = savePwd;
    }
}

internal sealed class EmployeeSalaryDialog : Form
{
    public EmployeeSalaryDialog(IReadOnlyList<EmployeeItem> employees)
    {
        Text = "👁️ Bảng lương & Dữ liệu nhạy cảm (PostgreSQL Column-Level Security)";
        StartPosition = FormStartPosition.CenterParent;
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;
        Width = 840;
        Height = 460;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ForeColor = CyberTheme.Text;

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 60 };
        var title = new Label
        {
            Text = "🛡️ BẢNG LƯƠNG NHÂN VIÊN (QUYỀN TRUY XUẤT ROLE_ADMIN)",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = CyberTheme.Warning,
            Dock = DockStyle.Top,
            Height = 26
        };
        var subTitle = new Label
        {
            Text = "Cột 'salary' trong quan hệ hr.employee được bảo vệ bởi PostgreSQL Column-Level Security. Chỉ Admin mới có quyền SELECT.",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = CyberTheme.MutedText,
            Dock = DockStyle.Top,
            Height = 28
        };
        topPanel.Controls.Add(subTitle);
        topPanel.Controls.Add(title);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = CyberTheme.Surface,
            BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 36
        };
        grid.ColumnHeadersDefaultCellStyle.BackColor = CyberTheme.SurfaceRaised;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = CyberTheme.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
        grid.DefaultCellStyle.BackColor = CyberTheme.Surface;
        grid.DefaultCellStyle.ForeColor = CyberTheme.Text;
        grid.DefaultCellStyle.SelectionBackColor = CyberTheme.SurfaceRaised;

        grid.Columns.Add("Id", "Mã NV");
        grid.Columns["Id"]!.Width = 60;
        grid.Columns.Add("Name", "Họ và tên");
        grid.Columns["Name"]!.Width = 140;
        grid.Columns.Add("Role", "Chức danh");
        grid.Columns["Role"]!.Width = 100;
        grid.Columns.Add("User", "Tài khoản");
        grid.Columns["User"]!.Width = 90;
        grid.Columns.Add("DbUser", "DB Role");
        grid.Columns["DbUser"]!.Width = 90;
        grid.Columns.Add("Salary", "Lương cơ bản");
        grid.Columns["Salary"]!.Width = 120;

        grid.Columns["Salary"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns["Salary"]!.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        grid.Columns["Salary"]!.DefaultCellStyle.ForeColor = CyberTheme.Success;
        grid.Columns.Add("Shift", "Ca làm việc");
        grid.Columns["Shift"]!.Width = 90;
        grid.Columns.Add("Active", "Trạng thái");
        grid.Columns["Active"]!.Width = 90;

        decimal totalSalary = 0;
        foreach (var emp in employees)
        {
            var roleName = emp.EmpRole.ToLowerInvariant() switch
            {
                "admin" => "Quản trị viên",
                "manager" => "Quản lý",
                "cashier" => "Thu ngân",
                "technician" => "Kỹ thuật viên",
                _ => emp.EmpRole
            };
            var sal = emp.Salary ?? 0;
            totalSalary += sal;
            grid.Rows.Add(
                emp.EmployeeId,
                emp.FullName,
                roleName,
                emp.Username,
                emp.DbUsername ?? "Chưa gán",
                $"{sal:N0} đ",
                emp.DefaultShift,
                emp.IsActive ? "Đang làm" : "Đã khóa");
        }

        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(0, 8, 0, 0) };
        var totalLabel = new Label
        {
            Text = $"Tổng quỹ lương cơ bản ({employees.Count} NV): {totalSalary:N0} đ",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = CyberTheme.Success,
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var closeBtn = new Button
        {
            Text = "Đóng",
            DialogResult = DialogResult.OK,
            Width = 90,
            Height = 34,
            Dock = DockStyle.Right
        };
        CyberTheme.StyleButton(closeBtn, CyberTheme.Primary);
        bottomPanel.Controls.Add(totalLabel);
        bottomPanel.Controls.Add(closeBtn);

        root.Controls.Add(grid);
        root.Controls.Add(bottomPanel);
        root.Controls.Add(topPanel);
        Controls.Add(root);
        AcceptButton = closeBtn;
    }
}
