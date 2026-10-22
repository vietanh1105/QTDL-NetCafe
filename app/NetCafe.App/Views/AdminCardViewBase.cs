// Trách nhiệm: renderer card responsive cho các màn hình Admin không cần bảng.
// Liên kết: AdminDataViewBase cung cấp toolbar; các view truyền DTO và nội dung card.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public sealed record AdminCardModel(
    string Title,
    string Subtitle,
    string Details,
    string Status,
    Color Accent);

public abstract class AdminCardViewBase : AdminDataViewBase
{
    protected readonly FlowLayoutPanel CardsPanel = new();
    private object? _selectedItem;

    protected object? SelectedItem => _selectedItem;

    protected virtual int CardHeight => 160;

    protected AdminCardViewBase()
    {
        Grid.Visible = false;
        CardsPanel.Dock = DockStyle.Fill;
        CardsPanel.AutoScroll = true;
        CardsPanel.WrapContents = true;
        CardsPanel.FlowDirection = FlowDirection.LeftToRight;
        CardsPanel.Padding = new Padding(12, 12, 24, 24);
        CardsPanel.BackColor = CyberTheme.Background;
        CardsPanel.Resize += (_, _) => ResizeCards();
        Controls.Add(CardsPanel);
        CardsPanel.BringToFront();
    }

    protected void SetCards<T>(
        IEnumerable<T> rows,
        Func<T, AdminCardModel> modelFactory,
        string info)
    {
        _selectedItem = null;
        CardsPanel.SuspendLayout();
        CardsPanel.Controls.Clear();
        foreach (var row in rows)
        {
            var card = CreateCard(modelFactory(row));
            card.Tag = row;
            AttachSelection(card);
            CardsPanel.Controls.Add(card);
        }
        CardsPanel.ResumeLayout();
        ResizeCards();
        InfoLabel.Text = info;
    }

    private Panel CreateCard(AdminCardModel model)
    {
        var card = new Panel
        {
            Width = 290,
            Height = CardHeight,
            Margin = new Padding(0, 0, 16, 16),
            Padding = new Padding(14),
            BackColor = CyberTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle
        };
        var accent = new Panel
        {
            Dock = DockStyle.Left,
            Width = 5,
            BackColor = model.Accent
        };
        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 26,
            Text = model.Title,
            ForeColor = CyberTheme.Text,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            AutoEllipsis = true
        };
        var subtitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = model.Subtitle,
            ForeColor = CyberTheme.MutedText,
            AutoEllipsis = true
        };
        var status = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = model.Status,
            ForeColor = model.Accent,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            AutoEllipsis = true
        };
        var details = new Label
        {
            Dock = DockStyle.Fill,
            Text = model.Details,
            ForeColor = CyberTheme.Text,
            AutoEllipsis = true
        };
        card.Controls.Add(details);
        card.Controls.Add(status);
        card.Controls.Add(subtitle);
        card.Controls.Add(title);
        card.Controls.Add(accent);
        return card;
    }

    private void AttachSelection(Control control)
    {
        control.Click += (_, _) => SelectCard(control);
        control.MouseEnter += (_, _) => SetHover(control, true);
        control.MouseLeave += (_, _) => SetHover(control, false);
        foreach (Control child in control.Controls)
        {
            AttachSelection(child);
        }
    }

    private void SelectCard(Control control)
    {
        var card = GetCardRoot(control);
        _selectedItem = card.Tag;
        foreach (Control candidate in CardsPanel.Controls)
        {
            candidate.BackColor = ReferenceEquals(candidate, card)
                ? CyberTheme.SurfaceRaised
                : CyberTheme.Surface;
        }
    }

    private void SetHover(Control control, bool hovered)
    {
        var card = GetCardRoot(control);
        if (ReferenceEquals(card.Tag, _selectedItem))
        {
            return;
        }

        if (!hovered)
        {
            var pointer = card.PointToClient(Cursor.Position);
            if (card.ClientRectangle.Contains(pointer))
            {
                return;
            }
        }

        card.BackColor = hovered ? CyberTheme.SurfaceRaised : CyberTheme.Surface;
    }

    private Control GetCardRoot(Control control)
    {
        var card = control;
        while (card.Parent is not null && !ReferenceEquals(card.Parent, CardsPanel))
        {
            card = card.Parent;
        }
        return card;
    }

    private void ResizeCards()
    {
        const int minimumCardWidth = 220;
        const int gap = 16;
        var availableWidth = Math.Max(
            1,
            CardsPanel.ClientSize.Width
            - CardsPanel.Padding.Horizontal
            - SystemInformation.VerticalScrollBarWidth);
        var columns = availableWidth >= (minimumCardWidth + gap) * 4
            ? 4
            : availableWidth >= (minimumCardWidth + gap) * 3
                ? 3
                : availableWidth >= (minimumCardWidth + gap) * 2
                    ? 2
                    : 1;
        var cardWidth = Math.Max(
            columns == 1 ? 1 : minimumCardWidth,
            (availableWidth - columns * gap) / columns);
        foreach (Control control in CardsPanel.Controls)
        {
            control.Width = cardWidth;
            control.Margin = new Padding(0, 0, gap, gap);
        }
    }
}
