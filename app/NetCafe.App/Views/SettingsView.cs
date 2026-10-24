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
        if (context.IsAdmin)
        {
            _vacuumButton.Visible = true;
            _vacuumButton.Enabled = true;
            _vacuumButton.Text = "🧹 VACUUM ANALYZE (DBA)";
            CyberTheme.StyleButton(_vacuumButton, CyberTheme.Warning);
        }
        else if (context.IsManager)
        {
            _vacuumButton.Visible = true;
            _vacuumButton.Enabled = false;
            _vacuumButton.Text = "🔒 VACUUM (Chỉ Admin/DBA)";
            CyberTheme.StyleButton(_vacuumButton, CyberTheme.MutedText);
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
        if (context.IsCashier || context.IsTechnician)
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
                $"Tỉ lệ trúng: {dbaStats.CacheHitRatio:N1}%",
                "Tỉ lệ truy vấn đọc trực tiếp từ bộ nhớ đệm Shared Buffers trong RAM PostgreSQL. Đạt chuẩn tối ưu QTDL (> 99%).",
                dbaStats.CacheHitRatio >= 95 ? "Tối ưu xuất sắc" : "Cần tăng RAM",
                CyberTheme.Success),
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
                "Pool hoạt động tốt",
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
                "Autovacuum do PostgreSQL quản lý. Chỉ Chủ quán (role_admin / DBA) có quyền chạy VACUUM thủ công.",
                context.IsAdmin ? "Quyền DBA" : "Chỉ xem",
                context.IsAdmin ? CyberTheme.Warning : CyberTheme.SurfaceRaised)
        };
        SetCards(cards, card => card, $"{cards.Length} nhóm thông tin và chỉ số DBA hệ thống");
    }

    private async Task RunVacuumAsync()
    {
        if (!RequiredContext.IsAdmin)
        {
            MessageBox.Show(
                FindForm(),
                "Chỉ Chủ quán (nhóm DBA / role_admin) mới có quyền chạy lệnh VACUUM ANALYZE trên CSDL.",
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
                "Đã chạy VACUUM ANALYZE cho các bảng vận hành thành công.\nCác dead tuples đã được dọn sạch và chỉ số thống kê bộ lập lịch truy vấn đã được cập nhật.",
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
