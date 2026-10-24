// Trách nhiệm: hộp thoại chốt tính tiền phiên máy trạm, chọn mã khuyến mãi và phương thức thanh toán.
// Liên kết: ComputerActionDialog, billing.promotion và sp_close_session_and_pay.

using System.Drawing;
using System.Windows.Forms;
using NetCafe.App.Views;
using NetCafe.Data;

namespace NetCafe.App.Controls;

public sealed class SessionCheckoutDialog : Form
{
    private readonly ComputerGridItem _computer;
    private readonly decimal _playAmount;
    private readonly decimal _serviceAmount;
    private readonly IReadOnlyList<ActivePromotionItem> _promotions;

    private readonly ComboBox _promoCombo = new();
    private readonly ComboBox _paymentMethodCombo = new();
    private readonly Label _discountLabel = new();
    private readonly Label _totalLabel = new();
    private readonly Label _methodNoteLabel = new();

    public string? SelectedPromoCode { get; private set; }
    public string SelectedPaymentMethod { get; private set; } = "cash";
    public decimal DiscountAmount { get; private set; }
    public decimal FinalTotal { get; private set; }

    public SessionCheckoutDialog(
        ComputerGridItem computer,
        decimal playAmount,
        decimal serviceAmount,
        IReadOnlyList<ActivePromotionItem> promotions)
    {
        _computer = computer;
        _playAmount = playAmount;
        _serviceAmount = serviceAmount;
        _promotions = promotions;

        Text = $"Tính tiền & Đóng máy {_computer.ComputerCode}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 460;
        Height = 490;
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;

        BuildLayout();
        Recalculate();
    }

    private void BuildLayout()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(16),
            BackColor = CyberTheme.Surface
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));

        AddRow(panel, "Máy trạm:", $"{_computer.ComputerCode} ({_computer.RoomName})");
        AddRow(panel, "Khách hàng:", string.IsNullOrWhiteSpace(_computer.CurrentCustomerName) ? "Khách vãng lai" : _computer.CurrentCustomerName);
        AddRow(panel, "Thời gian chơi:", $"{_computer.PlayedMinutes ?? 0:N0} phút");
        AddRow(panel, "Tiền giờ:", $"{_playAmount:N0} đ");
        AddRow(panel, "Tiền dịch vụ/món:", $"{_serviceAmount:N0} đ");

        // Promo combo
        _promoCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _promoCombo.Dock = DockStyle.Fill;
        _promoCombo.Items.Add("(Không áp dụng mã)");
        foreach (var p in _promotions)
        {
            var desc = p.PromoType switch
            {
                "percent" => $"-{p.Value:N0}% tiền giờ",
                "happy_hour" => $"-{p.Value:N0}% giờ vàng",
                "fixed" => $"-{p.Value:N0} đ",
                _ => $"{p.Value:N0}"
            };
            _promoCombo.Items.Add($"{p.Code} ({desc})");
        }
        _promoCombo.SelectedIndex = 0;
        _promoCombo.SelectedIndexChanged += (_, _) => Recalculate();
        AddControlRow(panel, "Mã khuyến mãi:", _promoCombo);

        _discountLabel.Text = "0 đ";
        _discountLabel.ForeColor = CyberTheme.Success;
        _discountLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        AddControlRow(panel, "Giảm giá tiền giờ:", _discountLabel);

        _totalLabel.Text = $"{_playAmount + _serviceAmount:N0} đ";
        _totalLabel.ForeColor = CyberTheme.Primary;
        _totalLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        AddControlRow(panel, "TỔNG CỘNG:", _totalLabel);

        _paymentMethodCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _paymentMethodCombo.Dock = DockStyle.Fill;
        var hasSufficientBalance = (_computer.CurrentBalance ?? 0) >= (_playAmount + _serviceAmount);
        _paymentMethodCombo.Items.Add("Trừ vào số dư ví hội viên");
        _paymentMethodCombo.Items.Add("Tiền mặt tại quầy");
        _paymentMethodCombo.SelectedIndex = hasSufficientBalance ? 0 : 1;
        _paymentMethodCombo.SelectedIndexChanged += (_, _) => UpdateMethodNote();
        AddControlRow(panel, "Hình thức trả:", _paymentMethodCombo);

        _methodNoteLabel.Text = $"Số dư ví hiện tại: {_computer.CurrentBalance ?? 0:N0} đ";
        _methodNoteLabel.ForeColor = CyberTheme.MutedText;
        _methodNoteLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
        AddControlRow(panel, "", _methodNoteLabel);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 6, 0, 0),
            BackColor = CyberTheme.Background
        };

        var cancelButton = new Button { Text = "Hủy bỏ", Width = 95, Height = 32 };
        CyberTheme.StyleButton(cancelButton, CyberTheme.SurfaceRaised);
        cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var confirmButton = new Button { Text = "✓ Chốt tính tiền", Width = 145, Height = 32 };
        CyberTheme.StyleButton(confirmButton, CyberTheme.Danger);
        confirmButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(confirmButton);

        Controls.Add(panel);
        Controls.Add(buttonPanel);
    }

    private void Recalculate()
    {
        DiscountAmount = 0;
        SelectedPromoCode = null;

        if (_promoCombo.SelectedIndex > 0 && _promoCombo.SelectedIndex - 1 < _promotions.Count)
        {
            var promo = _promotions[_promoCombo.SelectedIndex - 1];
            SelectedPromoCode = promo.Code;
            if (promo.PromoType is "percent" or "happy_hour")
            {
                DiscountAmount = Math.Round((_playAmount * promo.Value) / 100.0m);
            }
            else if (promo.PromoType == "fixed")
            {
                DiscountAmount = Math.Min(promo.Value, _playAmount);
            }
        }

        FinalTotal = Math.Max(0, _playAmount + _serviceAmount - DiscountAmount);
        _discountLabel.Text = DiscountAmount > 0 ? $"-{DiscountAmount:N0} đ" : "0 đ";
        _totalLabel.Text = $"{FinalTotal:N0} đ";

        UpdateMethodNote();
    }

    private void UpdateMethodNote()
    {
        var isBalance = _paymentMethodCombo.SelectedIndex == 0;
        SelectedPaymentMethod = isBalance ? "balance" : "cash";

        var balance = _computer.CurrentBalance ?? 0;
        if (isBalance && balance < FinalTotal)
        {
            _methodNoteLabel.Text = $"⚠️ Số dư ví ({balance:N0} đ) không đủ trả {FinalTotal:N0} đ! Sẽ thu tiền mặt.";
            _methodNoteLabel.ForeColor = CyberTheme.Warning;
            SelectedPaymentMethod = "cash";
        }
        else
        {
            _methodNoteLabel.Text = $"Số dư ví hội viên: {balance:N0} đ";
            _methodNoteLabel.ForeColor = CyberTheme.MutedText;
        }
    }

    private static void AddRow(TableLayoutPanel table, string label, string value)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            ForeColor = CyberTheme.MutedText,
            Padding = new Padding(0, 4, 4, 4)
        }, 0, row);
        table.Controls.Add(new Label
        {
            Text = value,
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = CyberTheme.Text,
            Padding = new Padding(0, 4, 0, 4)
        }, 1, row);
    }

    private static void AddControlRow(TableLayoutPanel table, string label, Control control)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            ForeColor = CyberTheme.MutedText,
            Padding = new Padding(0, 6, 4, 4)
        }, 0, row);
        table.Controls.Add(control, 1, row);
    }
}

