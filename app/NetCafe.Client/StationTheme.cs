// Trách nhiệm: màu sắc và kiểu điều khiển dùng chung cho giao diện máy trạm.
// Liên kết: MainForm, LoginView, SessionView, FoodOrderView và SupportRequestView.

namespace NetCafe.Client;

public static class StationTheme
{
    // Nền graphite và một accent ấm giúp giao diện gọn như quầy Cyber Net.
    public static readonly Color Background = Color.FromArgb(17, 19, 21);
    public static readonly Color Surface = Color.FromArgb(27, 30, 34);
    public static readonly Color SurfaceRaised = Color.FromArgb(36, 40, 45);
    public static readonly Color Border = Color.FromArgb(58, 65, 73);
    public static readonly Color Primary = Color.FromArgb(245, 158, 11);
    public static readonly Color Cyan = Color.FromArgb(163, 230, 53);
    public static readonly Color Purple = Color.FromArgb(71, 85, 105);
    public static readonly Color Warning = Color.FromArgb(245, 158, 11);
    public static readonly Color Danger = Color.FromArgb(239, 68, 68);
    public static readonly Color Text = Color.FromArgb(246, 247, 249);
    public static readonly Color MutedText = Color.FromArgb(165, 173, 184);

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
            Text = title,
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

}
