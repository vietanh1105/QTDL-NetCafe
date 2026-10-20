// Trách nhiệm: dashboard chẩn đoán hệ thống, quyền hiện tại và listener; không hiển thị password.
// Liên kết: AdminViewContext, DatabaseRuntimeInfo, listener PostgreSQL và VACUUM ANALYZE.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public partial class SettingsView : AdminViewBase
{
    private readonly FlowLayoutPanel _cards = new();
    private readonly Button _vacuumButton = new();

    public SettingsView()
    {
        InitializeComponent();
        BackColor = BackgroundColor;
        Padding = new Padding(16);
        _cards.Dock = DockStyle.Fill;
        _cards.AutoScroll = true;
        _cards.WrapContents = true;
        _cards.Padding = new Padding(4);
        _cards.BackColor = BackgroundColor;
        _vacuumButton.Text = "🧹 VACUUM ANALYZE";
        _vacuumButton.Width = 170;
        _vacuumButton.Height = 34;
        CyberTheme.StyleButton(_vacuumButton, CyberTheme.Warning);
        _vacuumButton.Click += async (_, _) => await RunVacuumAsync();
        Controls.Add(_cards);
    }

    public override Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var context = RequiredContext;
        _cards.Controls.Clear();
        AddCard("CSDL hiện tại", $"{context.ConnectionSummary}\nKhông hiển thị password.", CyberTheme.Primary);
        AddCard("Tài khoản và quyền", $"User: {context.CurrentUser}\nEmployee ID: {context.CurrentEmployeeId}\nSession: {(context.CanManageSessions ? "được phép" : "bị khóa")}\nCustomer: {(context.CanManageCustomers ? "được phép" : "bị khóa")}", CyberTheme.Success);
        AddCard("Contract migration", context.RuntimeInfo?.ContractStatus ?? "Chưa đọc metadata", context.RuntimeInfo?.IsStationContractReady == true ? CyberTheme.Success : CyberTheme.Danger);
        AddCard("LISTEN / NOTIFY", $"Trạng thái: {context.ListenerStatus}\nKênh: ai_security_alert, computer_state_changed, support_queue_changed, food_order_changed", CyberTheme.Warning);
        AddCard("Dọn dẹp và bảo mật", "Autovacuum do PostgreSQL quản lý. VACUUM ANALYZE chỉ chạy khi Admin bấm nút; không reset dữ liệu.", CyberTheme.SurfaceRaised, _vacuumButton);
        return Task.CompletedTask;
    }

    private void AddCard(string title, string details, Color accent, Control? action = null)
    {
        var card = new Panel
        {
            Width = 360,
            Height = 150,
            Margin = new Padding(0, 0, 16, 16),
            Padding = new Padding(14),
            BackColor = CyberTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle
        };
        var strip = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = accent };
        var body = new Label
        {
            Dock = DockStyle.Fill,
            Text = details,
            ForeColor = CyberTheme.Text,
            AutoEllipsis = true
        };
        var heading = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = title,
            ForeColor = accent,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        card.Controls.Add(body);
        if (action is not null)
        {
            action.Dock = DockStyle.Bottom;
            card.Controls.Add(action);
        }
        card.Controls.Add(heading);
        card.Controls.Add(strip);
        _cards.Controls.Add(card);
    }

    private async Task RunVacuumAsync()
    {
        try
        {
            await RequiredContext.Repository.RunVacuumAnalyzeAsync();
            MessageBox.Show(FindForm(), "Đã chạy VACUUM ANALYZE cho các bảng vận hành.", "Dọn dẹp thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }
}
