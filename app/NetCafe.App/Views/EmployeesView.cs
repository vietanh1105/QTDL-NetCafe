// Trách nhiệm: danh sách nhân viên và role/shift; salary vẫn được bảo vệ bởi column privilege.

namespace NetCafe.App.Views;

public partial class EmployeesView : AdminCardViewBase
{
    public EmployeesView()
    {
        InitializeComponent();
        SetTitle("Nhân viên & Phân quyền", "Thông tin lương và mật khẩu được bảo vệ bởi cơ chế phân quyền CSDL.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetEmployeesAsync();
        SetCards(
            rows,
            employee =>
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

                var shiftText = employee.DefaultShift.ToLowerInvariant() switch
                {
                    "morning" => "Ca sáng",
                    "afternoon" => "Ca chiều",
                    "night" => "Ca đêm",
                    _ => string.IsNullOrWhiteSpace(employee.DefaultShift) ? "Linh hoạt" : employee.DefaultShift
                };

                return new AdminCardModel(
                    employee.FullName,
                    $"{roleText} · @{employee.Username}",
                    $"Ca làm việc: {shiftText}\nVai trò CSDL: {employee.DbUsername ?? "Chưa gắn"}",
                    employee.DbUsername is null ? "Chưa gắn tài khoản CSDL" : "Đang hoạt động",
                    employee.DbUsername is null ? CyberTheme.Warning : CyberTheme.Success);
            },
            $"{rows.Count} nhân viên đang hoạt động");
    }
}
