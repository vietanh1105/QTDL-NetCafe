// Trách nhiệm: bảng màu và trạng thái điều khiển dùng chung cho toàn bộ Admin.
// Liên kết: AdminViewBase, AdminDataViewBase, MainForm và các card Admin.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public static class CyberTheme
{
    public static Color Background { get; } = Color.FromArgb(8, 15, 32);
    public static Color Surface { get; } = Color.FromArgb(15, 23, 42);
    public static Color SurfaceRaised { get; } = Color.FromArgb(30, 41, 59);
    public static Color Border { get; } = Color.FromArgb(71, 85, 105);
    public static Color Text { get; } = Color.FromArgb(226, 232, 240);
    public static Color MutedText { get; } = Color.FromArgb(148, 163, 184);
    public static Color Primary { get; } = Color.FromArgb(14, 165, 233);
    public static Color Success { get; } = Color.FromArgb(34, 197, 94);
    public static Color Warning { get; } = Color.FromArgb(245, 158, 11);
    public static Color Danger { get; } = Color.FromArgb(239, 68, 68);

    public static void StyleButton(Button button, Color? accent = null)
    {
        var color = accent ?? Primary;
        button.BackColor = color;
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.12F);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(color, 0.12F);
        button.FlatAppearance.CheckedBackColor = color;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
    }
}
