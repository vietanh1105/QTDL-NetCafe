// Trách nhiệm: queue yêu cầu hỗ trợ từ station; trạng thái do Admin acknowledge/resolve.

using System.Drawing;
using System.Windows.Forms;
using NetCafe.Data;

namespace NetCafe.App.Views;

public partial class SupportQueueView : AdminDataViewBase
{
    private readonly Button _acknowledgeButton = CreateActionButton("Acknowledge");
    private readonly Button _resolveButton = CreateActionButton("Resolve");
    private readonly Button _cancelButton = CreateActionButton("Cancel");

    public SupportQueueView()
    {
        InitializeComponent();
        SetTitle("Support queue", "Station ghi request qua function; Admin xử lý lifecycle new → acknowledged → resolved.");
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
        SetRows(rows, $"{rows.Count} yêu cầu hỗ trợ");
    }

    private async Task ChangeStatusAsync(string status)
    {
        if (RequiredContext.CurrentEmployeeId <= 0)
        {
            ReportError(new InvalidOperationException("Role hiện tại chưa ánh xạ employee_id; không thể xử lý support queue."));
            return;
        }

        if (Grid.CurrentRow?.DataBoundItem is not ServiceRequestItem request)
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
