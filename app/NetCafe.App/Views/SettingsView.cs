// Trách nhiệm: dashboard chẩn đoán hệ thống, quyền hiện tại và listener; không hiển thị password.
// Liên kết: AdminViewContext, DatabaseRuntimeInfo, listener PostgreSQL và VACUUM ANALYZE.

using System.Drawing;
using System.Windows.Forms;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class SettingsView : AdminCardViewBase
{
    private readonly Button _vacuumButton = new();

    public SettingsView()
    {
        InitializeComponent();
        _vacuumButton.Text = "🧹 VACUUM ANALYZE";
        _vacuumButton.Width = 195;
        _vacuumButton.Height = 34;

        CyberTheme.StyleButton(_vacuumButton, CyberTheme.Warning);
        _vacuumButton.Click += async (_, _) => await RunVacuumAsync();
        ActionsPanel.Controls.Add(_vacuumButton);
    }

    public override void SetContext(AdminViewContext context)
    {
        base.SetContext(context);
        UpdateRolePermissions(context);
    }

    private void UpdateRolePermissions(AdminViewContext context)
    {
        var canVacuum = context.IsAdmin && context.RuntimeInfo?.CanRunVacuum == true;
        if (canVacuum)
        {
            _vacuumButton.Visible = true;
            _vacuumButton.Enabled = true;
            _vacuumButton.Text = "🧹 VACUUM ANALYZE (DBA)";
            CyberTheme.StyleButton(_vacuumButton, CyberTheme.Warning);
        }
        else if (context.IsAdmin)
        {
            _vacuumButton.Visible = true;
            _vacuumButton.Enabled = false;
            _vacuumButton.Text = "🔒 VACUUM (Thiếu quyền MAINTAIN)";
            CyberTheme.StyleButton(_vacuumButton, CyberTheme.MutedText);
        }
        else if (context.IsManager)
        {
            _vacuumButton.Visible = false;
            _vacuumButton.Enabled = false;
        }
        else
        {
            _vacuumButton.Visible = false;
            ClearCards(
                "🔒 Vai trò hiện tại không có quyền truy cập Thông tin hệ thống (Cơ chế CSDL: DBA Privileges).");
        }
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var context = RequiredContext;
        if (context.IsCashier)
        {
            UpdateRolePermissions(context);
            return;
        }

        UpdateRolePermissions(context);

        DbaStatisticsInfo dbaStats;
        try
        {
            dbaStats = await context.Repository.GetDbaStatisticsAsync(cancellationToken);
        }
        catch
        {
            dbaStats = new DbaStatisticsInfo();
        }

        var cards = new[]
        {
            new AdminCardModel(
                "CSDL hiện tại",
                "Kết nối đang sử dụng",
                $"{context.ConnectionSummary}\nKhông hiển thị password.",
                "Đang kết nối",
                CyberTheme.Primary),
            new AdminCardModel(
                "Hiệu năng Cache RAM",
                dbaStats.CacheHitRatio.HasValue ? $"Tỉ lệ trúng: {dbaStats.CacheHitRatio.Value:N1}%" : "Chưa có số liệu",
                "Tỉ lệ truy vấn đọc từ Shared Buffers của PostgreSQL; chỉ kết luận đạt >99% sau khi có số liệu thực tế.",
                dbaStats.CacheHitRatio is null ? "Chưa đo" : dbaStats.CacheHitRatio >= 99 ? "Đạt >99%" : "Cần theo dõi",
                dbaStats.CacheHitRatio is null ? CyberTheme.MutedText : dbaStats.CacheHitRatio >= 99 ? CyberTheme.Success : CyberTheme.Warning),
            new AdminCardModel(
                "Lưu trữ & Rác dữ liệu",
                $"Size: {dbaStats.DatabaseSize} · Rác: {dbaStats.TotalDeadTuples:N0} dòng",
                $"Tổng số dead tuples hiện tại: {dbaStats.TotalDeadTuples:N0} dòng.\nBấm nút VACUUM ANALYZE bên trên để thu hồi dung lượng đĩa và cập nhật Query Planner.",
                dbaStats.TotalDeadTuples > 0 ? "Cần VACUUM" : "Đã tối ưu",
                dbaStats.TotalDeadTuples > 0 ? CyberTheme.Warning : CyberTheme.Success),
            new AdminCardModel(
                "Kết nối PostgreSQL",
                $"Tổng: {dbaStats.TotalConnections} kết nối",
                $"Đang chạy truy vấn: {dbaStats.ActiveConnections} · Đang chờ (Idle): {dbaStats.IdleConnections}\nConnection Pooling Npgsql điều phối giữ kết nối tái sử dụng.",
                "Đã quan sát",
                CyberTheme.Primary),
            new AdminCardModel(
                "Tài khoản và quyền",
                $"User: {context.CurrentUser} · Role: {context.UserRole} · Employee ID: {context.CurrentEmployeeId}",
                $"Session: {(context.CanManageSessions ? "được phép" : "bị khóa")}\nCustomer: {(context.CanManageCustomers ? "được phép" : "bị khóa")}",
                "Quyền hiện tại",
                CyberTheme.Success),
            new AdminCardModel(
                "Contract migration",
                "Station/Admin PostgreSQL contract",
                context.RuntimeInfo?.ContractStatus ?? "Chưa đọc metadata",
                context.RuntimeInfo?.IsStationContractReady == true ? "Sẵn sàng" : "Thiếu migration",
                context.RuntimeInfo?.IsStationContractReady == true ? CyberTheme.Success : CyberTheme.Danger),
            new AdminCardModel(
                "LISTEN / NOTIFY",
                $"Trạng thái: {context.ListenerStatus}",
                "Kênh: ai_security_alert, computer_state_changed, support_queue_changed, food_order_changed",
                "Listener hệ thống",
                CyberTheme.Warning),
            new AdminCardModel(
                "Dọn dẹp và bảo mật",
                "VACUUM ANALYZE theo yêu cầu",
                "Autovacuum do PostgreSQL quản lý. VACUUM thủ công chỉ bật khi tài khoản hiện tại có quyền MAINTAIN trên các bảng vận hành.",
                context.RuntimeInfo?.CanRunVacuum == true ? "Có quyền MAINTAIN" : "Chỉ xem",
                context.RuntimeInfo?.CanRunVacuum == true ? CyberTheme.Warning : CyberTheme.SurfaceRaised)
        };
        SetCards(cards, card => card, $"{cards.Length} nhóm thông tin và chỉ số DBA hệ thống");
    }

    private async Task RunVacuumAsync()
    {
        if (!RequiredContext.IsAdmin)
        {
            MessageBox.Show(
                FindForm(),
                "Tài khoản hiện tại chưa có quyền MAINTAIN để chạy VACUUM ANALYZE.",
                "Từ chối quyền DBA",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            await RequiredContext.Repository.RunVacuumAnalyzeAsync();
            MessageBox.Show(
                FindForm(),
                "Đã chạy VACUUM ANALYZE cho các bảng vận hành.\nChỉ số thống kê Query Planner đã được cập nhật; dead tuples sẽ được đo lại khi làm mới màn hình.",
                "Dọn dẹp DBA thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }
}
