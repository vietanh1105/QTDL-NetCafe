// Trách nhiệm: queue yêu cầu hỗ trợ từ station; trạng thái do Admin acknowledge/resolve.

using System.Drawing;
using System.Windows.Forms;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class SupportQueueView : AdminCardViewBase
{
    private readonly Button _acknowledgeButton = CreateActionButton("Tiếp nhận");
    private readonly Button _resolveButton = CreateActionButton("Hoàn thành");
    private readonly Button _cancelButton = CreateActionButton("Hủy yêu cầu");

    public SupportQueueView()
    {
        InitializeComponent();
        SetTitle("Yêu cầu hỗ trợ", "Máy trạm gửi yêu cầu qua PostgreSQL NOTIFY; chọn card để xử lý theo luồng Mới → Tiếp nhận → Hoàn thành.");
        Toolbar.Controls.Add(_cancelButton);
        Toolbar.Controls.Add(_resolveButton);
        Toolbar.Controls.Add(_acknowledgeButton);
        _acknowledgeButton.Click += async (_, _) => await ChangeStatusAsync("acknowledged");
        _resolveButton.Click += async (_, _) => await ChangeStatusAsync("resolved");
        _cancelButton.Click += async (_, _) => await ChangeStatusAsync("cancelled");
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetServiceRequestsAsync(cancellationToken);
        SetCards(rows, BuildCard, $"{rows.Count} yêu cầu hỗ trợ; {rows.Count(row => row.Status == "new")} yêu cầu mới.");
    }

    private async Task ChangeStatusAsync(string status)
    {
        if (RequiredContext.CurrentEmployeeId <= 0)
        {
            ReportError(new InvalidOperationException("Vai trò hiện tại chưa gắn mã nhân viên; không thể xử lý hàng đợi hỗ trợ."));
            return;
        }

        if (SelectedItem is not ServiceRequestItem request)
        {
            return;
        }

        try
        {
            await RequiredContext.Repository.UpdateServiceRequestStatusAsync(
                request.ServiceRequestId, status, RequiredContext.CurrentEmployeeId);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private static AdminCardModel BuildCard(ServiceRequestItem request)
    {
        var (status, accent) = request.Status switch
        {
            "new" => ("🔴 Mới tiếp nhận", CyberTheme.Danger),
            "acknowledged" => ("🟡 Đang xử lý", CyberTheme.Warning),
            "resolved" => ("🟢 Hoàn thành", CyberTheme.Success),
            "cancelled" => ("⚪ Đã hủy", CyberTheme.MutedText),
            _ => (request.Status, CyberTheme.Primary)
        };

        return new AdminCardModel(
            $"#{request.ServiceRequestId} · {request.ComputerCode}",
            $"{request.CustomerName} · {request.RequestType}",
            $"{request.Message}\nGửi lúc: {request.CreatedAt.LocalDateTime:dd/MM HH:mm:ss}",
            status,
            accent);
    }

    private static Button CreateActionButton(string text)
    {
        return new Button
        {
            Text = text,
            Width = 92,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White
        };
    }
}
