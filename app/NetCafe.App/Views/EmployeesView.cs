// Trách nhiệm: danh sách nhân viên và role/shift; salary vẫn được bảo vệ bởi column privilege.

namespace NetCafe.App.Views;

public partial class EmployeesView : AdminDataViewBase
{
    public EmployeesView()
    {
        InitializeComponent();
        SetTitle("Nhân viên", "Cột salary/password_hash không được đưa vào DTO cấp thấp.");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetEmployeesAsync();
        SetRows(rows, $"{rows.Count} nhân viên active");
    }
}
