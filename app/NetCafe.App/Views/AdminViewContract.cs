// Trách nhiệm: contract navigation và context dùng chung cho mọi Admin UserControl.
// Liên kết: MainForm.cs điều phối lifecycle; DbRepository.cs cung cấp data access.

using NetCafe.Data;

namespace NetCafe.App.Views;

public sealed class AdminViewContext
{
    public required DbRepository Repository { get; init; }
    public required Func<Task> RefreshComputersAsync { get; init; }
    public required Action<Exception> ShowError { get; init; }
    public string CurrentUser { get; set; } = "postgres";
    public int CurrentEmployeeId { get; set; }
    public string ConnectionSummary { get; set; } = string.Empty;
    public bool CanManageCustomers { get; set; }
    public bool CanManageSessions { get; set; }
}

public interface IAdminView
{
    Task RefreshAsync(CancellationToken cancellationToken = default);
    void SetContext(AdminViewContext context);
}

public sealed class IdTextOption
{
    public int Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public override string ToString() => Text;
}

public sealed class LongTextOption
{
    public long Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public override string ToString() => Text;
}
