// Trách nhiệm: bảng màu và trạng thái điều khiển dùng chung cho toàn bộ Admin (Light Orange Theme).
// Liên kết: AdminViewBase, AdminDataViewBase, MainForm và các card Admin.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public static class CyberTheme
{
    public static Color Background { get; } = Color.FromArgb(248, 250, 252); // #F8FAFC - crisp light workspace
    public static Color Surface { get; } = Color.FromArgb(255, 255, 255); // #FFFFFF - pure white
    public static Color SurfaceRaised { get; } = Color.FromArgb(255, 247, 237); // #FFF7ED - soft warm orange tint
    public static Color Border { get; } = Color.FromArgb(226, 232, 240); // #E2E8F0 - crisp light border
    public static Color Text { get; } = Color.FromArgb(15, 23, 42); // #0F172A - deep slate readable text
    public static Color MutedText { get; } = Color.FromArgb(100, 116, 139); // #64748B - secondary text
    public static Color Primary { get; } = Color.FromArgb(234, 88, 12); // #EA580C - vibrant orange
    public static Color Success { get; } = Color.FromArgb(22, 163, 74); // #16A34A - emerald green
    public static Color Warning { get; } = Color.FromArgb(217, 119, 6); // #D97706 - amber
    public static Color Danger { get; } = Color.FromArgb(220, 38, 38); // #DC2626 - coral red

    public static void StyleButton(Button button, Color? accent = null)
    {
        var color = accent ?? Primary;
        button.BackColor = color;
        var isLight = (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) > 160;
        button.ForeColor = isLight ? Text : Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = isLight ? Color.FromArgb(254, 215, 170) : Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.12F);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(color, 0.12F);
        button.FlatAppearance.CheckedBackColor = color;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
    }
}
