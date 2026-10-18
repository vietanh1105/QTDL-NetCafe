// Trách nhiệm: danh sách nhân viên và role/shift; salary vẫn được bảo vệ bởi column privilege.

namespace NetCafe.App.Views;

public partial class EmployeesView : AdminCardViewBase
{
    public EmployeesView()
    {
        InitializeComponent();
        SetTitle("Nhân viên", "Cột salary/password_hash không được đưa vào DTO cấp thấp.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetEmployeesAsync();
        SetCards(
            rows,
            employee => new AdminCardModel(
                employee.FullName,
                $"{employee.EmpRole} · {employee.Username}",
                $"Ca mặc định: {employee.DefaultShift}\nDB role: {employee.DbUsername ?? "chưa ánh xạ"}",
                employee.DbUsername is null ? "Chưa ánh xạ database" : "Đang hoạt động",
                employee.DbUsername is null ? CyberTheme.Warning : CyberTheme.Success),
            $"{rows.Count} nhân viên active");
    }
}
