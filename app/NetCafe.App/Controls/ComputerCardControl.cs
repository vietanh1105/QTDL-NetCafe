// Trách nhiệm: card trạng thái một máy và event mở chi tiết.
// Liên kết: ComputerGridItem từ NetCafe.Data; ComputersView quản lý collection theo ComputerId.

using System.Drawing;
using NetCafe.Data;

namespace NetCafe.App.Controls;

public partial class ComputerCardControl : UserControl
{
    private Color _baseColor = Color.White;
    public ComputerGridItem? Model { get; private set; }
    public event EventHandler<ComputerGridItem>? CardSelected;

    public ComputerCardControl()
    {
        InitializeComponent();
        WireHover(this);
        _detailsButton.Click += (_, _) => SelectCard();
        Click += (_, _) => SelectCard();
    }

    public void Bind(ComputerGridItem model)
    {
        Model = model;
        _codeLabel.Text = model.ComputerCode;
        _nameLabel.Text = model.ComputerName;
        _roomLabel.Text = $"Phòng: {model.RoomName}";
        _typeLabel.Text = $"{model.TypeName} · {model.HourlyRate:N0}đ/giờ";
        _statusLabel.Text = model.ComputerStatus switch
        {
            "available" => "● Sẵn sàng",
            "in_use" => "● Đang sử dụng",
            "maintenance" => "● Bảo trì",
            "broken" => "● Hỏng",
            _ => $"● {model.ComputerStatus}"
        };
        var statusColor = model.ComputerStatus switch
        {
            "available" => Color.FromArgb(22, 163, 74),
            "in_use" => Color.FromArgb(220, 38, 38),
            "maintenance" => Color.FromArgb(234, 88, 12),
            "broken" => Color.FromArgb(107, 114, 128),
            _ => Color.FromArgb(71, 85, 105)
        };
        _statusLabel.ForeColor = statusColor;
        _customerLabel.Text = string.IsNullOrWhiteSpace(model.CurrentCustomerName) ? "Chưa có khách" : model.CurrentCustomerName;
        _costLabel.Text = model.PlayedMinutes is null
            ? "Chưa có phiên"
            : $"~{model.HourlyRate * model.PlayedMinutes.Value / 60:N0}đ / {model.PlayedMinutes:N0} phút";
        _baseColor = Color.FromArgb(255, 255, 255);
        BackColor = _baseColor;
    }

    private void SelectCard()
    {
        if (Model is not null)
        {
            CardSelected?.Invoke(this, Model);
        }
    }

    private static void WireHover(Control control)
    {
        control.MouseEnter += (_, _) => control.BackColor = Color.FromArgb(239, 246, 255);
        control.MouseLeave += (_, _) => control.BackColor = control is ComputerCardControl card ? card._baseColor : Color.White;
        foreach (Control child in control.Controls)
        {
            WireHover(child);
        }
    }
}
