// Trách nhiệm: bảng màu và trạng thái điều khiển dùng chung cho toàn bộ Admin.
// Liên kết: AdminViewBase, AdminDataViewBase, MainForm và các card Admin.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public static class CyberTheme
{
    public static Color Background { get; } = Color.FromArgb(43, 46, 58); // #2B2E3A - sáng hơn, vẫn giữ nền graphite
    public static Color Surface { get; } = Color.FromArgb(53, 58, 74); // #353A4A - card graphite
    public static Color SurfaceRaised { get; } = Color.FromArgb(74, 74, 105); // #4A4A69 - bề mặt tím xanh
    public static Color Border { get; } = Color.FromArgb(116, 122, 152); // #747A98 - viền dễ nhìn
    public static Color Text { get; } = Color.FromArgb(248, 249, 255); // #F8F9FF - chữ sáng
    public static Color MutedText { get; } = Color.FromArgb(196, 201, 219); // #C4C9DB - chữ phụ sáng
    public static Color Primary { get; } = Color.FromArgb(143, 155, 255); // #8F9BFF - tím xanh chủ đạo
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

    public static void FitToolbarButton(Button button, int minimumWidth = 0)
    {
        button.AutoEllipsis = false;
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.Padding = new Padding(8, 0, 8, 0);
        var preferredWidth = button.GetPreferredSize(new Size(0, Math.Max(30, button.Height))).Width + 16;
        button.MinimumSize = new Size(Math.Max(minimumWidth, preferredWidth), Math.Max(30, button.Height));
    }
}
