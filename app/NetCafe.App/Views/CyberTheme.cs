// Trách nhiệm: bảng màu và trạng thái điều khiển dùng chung cho toàn bộ Admin.
// Liên kết: AdminViewBase, AdminDataViewBase, MainForm và các card Admin.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public static class CyberTheme
{
    public static Color Background { get; } = Color.FromArgb(26, 29, 39); // #1A1D27 - graphite workspace
    public static Color Surface { get; } = Color.FromArgb(36, 40, 54); // #242836 - graphite cards
    public static Color SurfaceRaised { get; } = Color.FromArgb(52, 54, 79); // #34364F - purple-blue raised surface
    public static Color Border { get; } = Color.FromArgb(80, 83, 111); // #50536F - muted indigo border
    public static Color Text { get; } = Color.FromArgb(244, 245, 255); // #F4F5FF - cool white text
    public static Color MutedText { get; } = Color.FromArgb(177, 184, 205); // #B1B8CD - secondary text
    public static Color Primary { get; } = Color.FromArgb(129, 140, 248); // #818CF8 - indigo primary
    public static Color Success { get; } = Color.FromArgb(52, 211, 153); // #34D399 - service teal
    public static Color Warning { get; } = Color.FromArgb(251, 191, 36); // #FBBF24 - warning amber
    public static Color Danger { get; } = Color.FromArgb(251, 113, 133); // #FB7185 - danger pink red

    public static void StyleButton(Button button, Color? accent = null)
    {
        var color = accent ?? Primary;
        button.BackColor = color;
        var isLight = (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) > 160;
        button.ForeColor = isLight ? Text : Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.12F);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(color, 0.12F);
        button.FlatAppearance.CheckedBackColor = color;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
    }
}
