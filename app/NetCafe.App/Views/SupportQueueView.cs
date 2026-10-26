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
        SetTitle(
            "Yêu cầu hỗ trợ",
            "Máy trạm gửi yêu cầu qua PostgreSQL NOTIFY; chọn card để xử lý theo luồng Mới → Tiếp nhận → Hoàn thành.");
        ActionsPanel.Controls.Add(_acknowledgeButton);
        ActionsPanel.Controls.Add(_resolveButton);
        ActionsPanel.Controls.Add(_cancelButton);

        _acknowledgeButton.Click += async (_, _) => await ChangeStatusAsync("acknowledged");
        _resolveButton.Click += async (_, _) => await ChangeStatusAsync("resolved");
        _cancelButton.Click += async (_, _) => await ChangeStatusAsync("cancelled");
        CardDoubleClicked += async (_, _) => await OpenRequestDetailsAsync();
    }

    public override void SetContext(AdminViewContext context)
    {
        base.SetContext(context);
        UpdateRoleSubtitle(context);
    }

    private void UpdateRoleSubtitle(AdminViewContext context)
    {
        if (context.IsCashier)
        {
            SetTitle(
                "Yêu cầu hỗ trợ",
                "Nhân viên tiếp nhận: F&B / Nạp ví / Chung (lỗi kỹ thuật do quản lý xử lý).");
        }
        else
        {
            SetTitle(
                "Yêu cầu hỗ trợ",
                "Chủ quán: Giám sát toàn bộ yêu cầu hỗ trợ từ máy trạm.");
        }
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetServiceRequestsAsync(cancellationToken);
        SetCards(rows, BuildCard, $"{rows.Count} yêu cầu hỗ trợ; {rows.Count(row => row.Status == "new")} yêu cầu mới.");
    }

    private bool CanManageRequest(ServiceRequestItem request)
    {
        if (RequiredContext.IsAdmin)
        {
            return true;
        }
        if (RequiredContext.IsCashier)
        {
            return request.RequestType is "food" or "payment" or "general";
        }
        return false;
    }

    private async Task ChangeStatusAsync(string status)
    {
        if (RequiredContext.CurrentEmployeeId <= 0)
        {
            ReportError(new InvalidOperationException(
                "Vai trò hiện tại chưa gắn mã nhân viên; không thể xử lý hàng đợi hỗ trợ."));
            return;
        }

        if (SelectedItem is not ServiceRequestItem request)
        {
            return;
        }

        if (RequiredContext.IsCashier && request.RequestType == "technical")
        {
            MessageBox.Show(
                FindForm(),
                "Nhân viên chỉ tiếp nhận Gọi món (F&B) và Nạp ví.\nYêu cầu kỹ thuật do Quản lý phụ trách.",
                "Phân quyền xử lý",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
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

    private async Task OpenRequestDetailsAsync()
    {
        if (SelectedItem is not ServiceRequestItem request)
        {
            return;
        }

        var canManage = CanManageRequest(request);
        using var dialog = new SupportRequestDialog(request, canManage);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.SelectedStatus is null)
        {
            return;
        }

        await ChangeStatusAsync(dialog.SelectedStatus);
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

        var typeTag = request.RequestType switch
        {
            "technical" => "🛠️ Lỗi phần cứng",
            "food" => "🍔 Gọi món F&B",
            "payment" => "💳 Nạp tiền ví",
            _ => "💬 Hỗ trợ chung"
        };

        return new AdminCardModel(
            $"#{request.ServiceRequestId} · {request.ComputerCode}",
            $"{request.CustomerName} · {typeTag}",
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
            BackColor = CyberTheme.Primary,
            ForeColor = Color.White
        };
    }
}

internal sealed class SupportRequestDialog : Form
{
    private readonly ComboBox _statusBox = new();

    public string? SelectedStatus =>
        _statusBox.Enabled && _statusBox.SelectedItem is SupportStatusOption option
            ? option.Value
            : null;

    public SupportRequestDialog(ServiceRequestItem request, bool canManage = true)
    {
        Text = $"Chi tiết yêu cầu hỗ trợ #{request.ServiceRequestId}";
        StartPosition = FormStartPosition.CenterParent;
        Width = 560;
        Height = 430;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(18)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        AddInfo(root, "Máy trạm", request.ComputerCode, 0);
        AddInfo(root, "Hội viên", request.CustomerName, 1);
        AddInfo(root, "Loại yêu cầu", request.RequestType, 2);

        var messageLabel = new Label
        {
            Text = "Nội dung",
            Dock = DockStyle.Fill,
            ForeColor = CyberTheme.MutedText,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(0, 7, 0, 0)
        };
        var messageBox = new TextBox
        {
            Text = request.Message,
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = CyberTheme.Surface,
            ForeColor = CyberTheme.Text,
            BorderStyle = BorderStyle.FixedSingle
        };
        root.Controls.Add(messageLabel, 0, 3);
        root.Controls.Add(messageBox, 1, 3);

        AddInfo(root, "Gửi lúc", request.CreatedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm:ss"), 4);

        var statusLabel = new Label
        {
            Text = "Chuyển trạng thái",
            Dock = DockStyle.Fill,
            ForeColor = CyberTheme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _statusBox.Dock = DockStyle.Fill;
        _statusBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusBox.BackColor = CyberTheme.Surface;
        _statusBox.ForeColor = CyberTheme.Text;
        var options = GetStatusOptions(request.Status);
        _statusBox.Items.AddRange(options.Cast<object>().ToArray());
        if (_statusBox.Items.Count > 0 && canManage)
        {
            _statusBox.SelectedIndex = 0;
        }
        else
        {
            _statusBox.Items.Add(new SupportStatusOption(request.Status, StatusText(request.Status)));
            _statusBox.SelectedIndex = 0;
            _statusBox.Enabled = false;
        }
        root.Controls.Add(statusLabel, 0, 5);
        root.Controls.Add(_statusBox, 1, 5);

        var cancelButton = new Button
        {
            Text = "Đóng",
            Width = 90,
            Height = 34,
            DialogResult = DialogResult.Cancel
        };
        CyberTheme.StyleButton(cancelButton, CyberTheme.SurfaceRaised);

        var saveButton = new Button
        {
            Text = canManage ? "Lưu trạng thái" : "Không có quyền",
            Width = 130,
            Height = 34,
            Enabled = canManage && _statusBox.Enabled
        };
        CyberTheme.StyleButton(saveButton, canManage ? CyberTheme.Primary : CyberTheme.MutedText);
        saveButton.Click += (_, _) =>
        {
            if (SelectedStatus is not null)
            {
                DialogResult = DialogResult.OK;
            }
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(saveButton);
        root.Controls.Add(buttonPanel, 1, 6);

        Controls.Add(root);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private static void AddInfo(TableLayoutPanel root, string label, string value, int row)
    {
        root.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = CyberTheme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        root.Controls.Add(new Label
        {
            Text = value,
            Dock = DockStyle.Fill,
            ForeColor = CyberTheme.Text,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 1, row);
    }

    private static SupportStatusOption[] GetStatusOptions(string status)
    {
        return status switch
        {
            "new" =>
            [
                new SupportStatusOption("acknowledged", "Tiếp nhận"),
                new SupportStatusOption("cancelled", "Hủy yêu cầu")
            ],
            "acknowledged" =>
            [
                new SupportStatusOption("resolved", "Hoàn thành"),
                new SupportStatusOption("cancelled", "Hủy yêu cầu")
            ],
            _ => []
        };
    }

    private static string StatusText(string status)
    {
        return status switch
        {
            "new" => "Mới tiếp nhận",
            "acknowledged" => "Đang xử lý",
            "resolved" => "Hoàn thành",
            "cancelled" => "Đã hủy",
            _ => status
        };
    }

    private sealed record SupportStatusOption(string Value, string Label)
    {
        public override string ToString() => Label;
    }
}
