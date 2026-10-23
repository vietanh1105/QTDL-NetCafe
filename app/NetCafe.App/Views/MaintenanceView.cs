// Trách nhiệm: lập và hoàn tất phiếu bảo trì; trigger database đồng bộ trạng thái máy.
// Liên kết: hr.maintenance, hr.sp_create_maintenance, hr.sp_complete_maintenance và core.computer.

using System.Drawing;
using System.Windows.Forms;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class MaintenanceView : AdminCardViewBase
{
    private readonly Button _createButton = new();
    private readonly Button _completeButton = new();

    public MaintenanceView()
    {
        InitializeComponent();
        SetTitle("Bảo trì thiết bị", "Phiếu reported/in_progress chuyển máy sang maintenance; fixed trả máy về available qua trigger CSDL.");
        ConfigureButton(_createButton, "+ Lập phiếu bảo trì", CyberTheme.Primary, 150);
        ConfigureButton(_completeButton, "✅ Hoàn tất sửa chữa", CyberTheme.Success, 160);
        ActionsPanel.Controls.Add(_createButton);
        ActionsPanel.Controls.Add(_completeButton);

        _createButton.Click += async (_, _) => await CreateTicketAsync();
        _completeButton.Click += async (_, _) => await CompleteTicketAsync();
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetMaintenanceAsync(cancellationToken);
        SetCards(
            rows,
            maintenance => new AdminCardModel(
                $"{maintenance.ComputerCode} · Phiếu #{maintenance.MaintenanceId}",
                $"Kỹ thuật viên: {maintenance.TechnicianName}",
                $"{maintenance.IssueDescription}\nBắt đầu: {maintenance.StartedAt:dd/MM/yyyy HH:mm}\nChi phí: {maintenance.Cost:N0} đ",
                maintenance.Status switch
                {
                    "fixed" => "Đã hoàn thành",
                    "unrepairable" => "Không thể sửa",
                    "in_progress" => "Đang xử lý",
                    _ => "Mới tiếp nhận"
                },
                maintenance.Status switch
                {
                    "fixed" => CyberTheme.Success,
                    "unrepairable" => CyberTheme.Danger,
                    _ => CyberTheme.Warning
                }),
            $"{rows.Count} phiếu bảo trì");
    }

    private MaintenanceItem? SelectedMaintenance => SelectedItem as MaintenanceItem;

    private async Task CreateTicketAsync()
    {
        var computers = await RequiredContext.Repository.GetComputersAsync();
        var employees = (await RequiredContext.Repository.GetEmployeesAsync())
            .Where(employee => employee.EmpRole.Equals("technician", StringComparison.OrdinalIgnoreCase))
            .ToList();
        using var dialog = new MaintenanceEditorDialog(computers, employees);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.SelectedComputer is null || dialog.SelectedTechnician is null)
        {
            return;
        }
        try
        {
            await RequiredContext.Repository.CreateMaintenanceAsync(
                dialog.SelectedComputer.ComputerId,
                dialog.SelectedTechnician.EmployeeId,
                dialog.IssueDescription);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private async Task CompleteTicketAsync()
    {
        if (SelectedMaintenance is null || SelectedMaintenance.Status is "fixed" or "unrepairable")
        {
            MessageBox.Show(FindForm(), "Chọn một phiếu đang xử lý để hoàn tất.", "Chưa chọn phiếu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new MaintenanceCompleteDialog();
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }
        try
        {
            await RequiredContext.Repository.CompleteMaintenanceAsync(SelectedMaintenance.MaintenanceId, dialog.Status, dialog.Cost);
            await RefreshAsync();
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

internal sealed class MaintenanceEditorDialog : Form
{
    private readonly ComboBox _computerBox = new();
    private readonly ComboBox _technicianBox = new();
    private readonly TextBox _issueBox = new();
    public ComputerGridItem? SelectedComputer => (_computerBox.SelectedItem as ComputerOption)?.Computer;
    public EmployeeItem? SelectedTechnician => (_technicianBox.SelectedItem as EmployeeOption)?.Employee;
    public string IssueDescription => _issueBox.Text.Trim();

    public MaintenanceEditorDialog(IReadOnlyList<ComputerGridItem> computers, IReadOnlyList<EmployeeItem> technicians)
    {
        Text = "Lập phiếu bảo trì";
        Width = 480;
        Height = 270;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;
        _computerBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _technicianBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _computerBox.Items.AddRange(computers.Where(computer => computer.ComputerStatus != "in_use").Select(computer => new ComputerOption(computer)).ToArray());
        _technicianBox.Items.AddRange(technicians.Select(employee => new EmployeeOption(employee)).ToArray());
        if (_computerBox.Items.Count > 0) _computerBox.SelectedIndex = 0;
        if (_technicianBox.Items.Count > 0) _technicianBox.SelectedIndex = 0;
        _issueBox.Multiline = true;
        _issueBox.Height = 70;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 4 };
        layout.Controls.Add(new Label { Text = "Máy", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        layout.Controls.Add(_computerBox, 1, 0);
        layout.Controls.Add(new Label { Text = "Kỹ thuật viên", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        layout.Controls.Add(_technicianBox, 1, 1);
        layout.Controls.Add(new Label { Text = "Lý do", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        layout.Controls.Add(_issueBox, 1, 2);
        var save = new Button { Text = "Lưu phiếu", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        CyberTheme.StyleButton(save, CyberTheme.Primary);
        layout.Controls.Add(save, 1, 3);
        Controls.Add(layout);
        AcceptButton = save;
    }

    private sealed record ComputerOption(ComputerGridItem Computer)
    {
        public override string ToString() => $"{Computer.ComputerCode} · {Computer.ComputerName}";
    }

    private sealed record EmployeeOption(EmployeeItem Employee)
    {
        public override string ToString() => $"{Employee.FullName} · @{Employee.Username}";
    }
}

internal sealed class MaintenanceCompleteDialog : Form
{
    private readonly ComboBox _statusBox = new();
    private readonly NumericUpDown _costBox = new();
    public string Status => _statusBox.SelectedItem?.ToString() ?? "fixed";
    public decimal Cost => _costBox.Value;

    public MaintenanceCompleteDialog()
    {
        Text = "Hoàn thành bảo trì";
        Width = 360;
        Height = 190;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;
        _statusBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusBox.Items.AddRange(new object[] { "fixed", "unrepairable" });
        _statusBox.SelectedIndex = 0;
        _costBox.Maximum = 1000000000;
        _costBox.Increment = 10000;
        _costBox.ThousandsSeparator = true;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 3 };
        layout.Controls.Add(new Label { Text = "Kết quả", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        layout.Controls.Add(_statusBox, 1, 0);
        layout.Controls.Add(new Label { Text = "Chi phí", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        layout.Controls.Add(_costBox, 1, 1);
        var save = new Button { Text = "Cập nhật", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        CyberTheme.StyleButton(save, CyberTheme.Success);
        layout.Controls.Add(save, 1, 2);
        Controls.Add(layout);
        AcceptButton = save;
    }
}
