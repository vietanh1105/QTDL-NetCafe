// Trách nhiệm: màu sắc và kiểu điều khiển dùng chung cho giao diện máy trạm.
// Liên kết: MainForm, LoginView, SessionView, FoodOrderView và SupportRequestView.

using System.Drawing.Drawing2D;

namespace NetCafe.Client;

public static class StationTheme
{
    public static readonly Color Background = Color.FromArgb(7, 11, 22);
    public static readonly Color Surface = Color.FromArgb(14, 22, 40);
    public static readonly Color SurfaceRaised = Color.FromArgb(19, 31, 54);
    public static readonly Color Border = Color.FromArgb(36, 56, 88);
    public static readonly Color Primary = Color.FromArgb(91, 140, 255);
    public static readonly Color Cyan = Color.FromArgb(45, 212, 191);
    public static readonly Color Purple = Color.FromArgb(167, 139, 250);
    public static readonly Color Warning = Color.FromArgb(251, 191, 36);
    public static readonly Color Danger = Color.FromArgb(248, 113, 113);
    public static readonly Color Text = Color.FromArgb(242, 247, 255);
    public static readonly Color MutedText = Color.FromArgb(143, 163, 191);

    public static void StyleButton(Button button, Color color)
    {
        button.BackColor = color;
        button.ForeColor = Text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.12F);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(color, 0.12F);
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }

    public static void StyleTextBox(Control control)
    {
        control.BackColor = SurfaceRaised;
        control.ForeColor = Text;
        control.Font = new Font("Segoe UI", 10F);
    }

    public static Panel CreateCard(string title, Control content, Color accent, int height)
    {
        var card = new Panel
        {
            Dock = DockStyle.Top,
            Height = height,
            BackColor = Surface,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(16, 10, 16, 10),
            Margin = new Padding(0, 0, 0, 10)
        };
        var strip = new Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = accent
        };
        var heading = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = title.ToUpperInvariant(),
            ForeColor = accent,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        content.Dock = DockStyle.Fill;
        content.ForeColor = content.ForeColor == SystemColors.ControlText ? Text : content.ForeColor;
        card.Controls.Add(content);
        card.Controls.Add(heading);
        card.Controls.Add(strip);
        return card;
    }

    public static Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = text,
            ForeColor = Text,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 0, 0)
        };
    }
}
