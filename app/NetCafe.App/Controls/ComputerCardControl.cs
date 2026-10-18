// Trách nhiệm: card trạng thái một máy và phát event click ở toàn bộ vùng card.
// Liên kết: ComputerGridItem từ NetCafe.Data; ComputersView quản lý collection theo ComputerId.

using System.Drawing;
using NetCafe.Data;

namespace NetCafe.App.Controls;

public partial class ComputerCardControl : UserControl
{
    private Color _baseColor = Color.White;
    private bool _selected;
    public ComputerGridItem? Model { get; private set; }
    public event EventHandler<ComputerGridItem>? CardSelected;

    public ComputerCardControl()
    {
        InitializeComponent();
        WireHover(this, this);
        WireClicks(this, this);
        _detailsButton.Click += (_, _) => SelectCard();
    }

    public void Bind(ComputerGridItem model)
    {
        Model = model;
        _codeLabel.Text = model.ComputerCode;
        _nameLabel.Text = model.ComputerName;
        _roomLabel.Text = $"Phòng: {model.RoomName}";
        _typeLabel.Text = $"{model.TypeName} · {model.HourlyRate:N0}đ/giờ";
        var statusColor = model.ComputerStatus switch
        {
            "available" => Color.FromArgb(22, 163, 74),
            "in_use" => Color.FromArgb(220, 38, 38),
            "maintenance" => Color.FromArgb(234, 88, 12),
            "broken" => Color.FromArgb(107, 114, 128),
            _ => Color.FromArgb(71, 85, 105)
        };
        _statusLabel.Text = model.ComputerStatus switch
        {
            "available" => "● Sẵn sàng",
            "in_use" => "● Đang sử dụng",
            "maintenance" => "● Bảo trì",
            "broken" => "● Hỏng",
            _ => $"● {model.ComputerStatus}"
        };
        _statusLabel.ForeColor = statusColor;
        _customerLabel.Text = string.IsNullOrWhiteSpace(model.CurrentCustomerName) ? "Chưa có khách" : model.CurrentCustomerName;
        _costLabel.Text = model.PlayedMinutes is null ? "Chưa có phiên" : $"~{model.HourlyRate * model.PlayedMinutes.Value / 60:N0}đ / {model.PlayedMinutes:N0} phút";
        _baseColor = Color.White;
        SetSelected(_selected);
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        BackColor = selected ? Color.FromArgb(219, 234, 254) : _baseColor;
        Invalidate();
    }

    private void SelectCard()
    {
        if (Model is not null)
        {
            CardSelected?.Invoke(this, Model);
        }
    }

    private static void WireHover(Control control, ComputerCardControl card)
    {
        control.MouseEnter += (_, _) =>
        {
            card.SetHovered(true);
        };
        control.MouseLeave += (_, _) =>
        {
            var pointer = card.PointToClient(Cursor.Position);
            if (!card.ClientRectangle.Contains(pointer))
            {
                card.SetHovered(false);
            }
        };
        foreach (Control child in control.Controls)
        {
            WireHover(child, card);
        }
    }

    private void SetHovered(bool hovered)
    {
        if (!_selected)
        {
            BackColor = hovered ? Color.FromArgb(239, 246, 255) : _baseColor;
        }
    }

    private static void WireClicks(Control control, ComputerCardControl card)
    {
        if (control is not Button)
        {
            control.Click += (_, _) => card.SelectCard();
        }
        foreach (Control child in control.Controls)
        {
            WireClicks(child, card);
        }
    }
}
