// Trách nhiệm: bảng màu và trạng thái điều khiển dùng chung cho toàn bộ Admin.
// Liên kết: AdminViewBase, AdminDataViewBase, MainForm và các card Admin.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public static class CyberTheme
{
    // Bảng màu Graphite Cyber chuyên nghiệp - Độ tương phản cao, phân cấp khối rõ ràng
    public static Color Background { get; } = Color.FromArgb(24, 26, 34); // #181A22 - Nền chính tối sâu
    public static Color Surface { get; } = Color.FromArgb(34, 38, 50); // #222632 - Card & Toolbar graphite
    public static Color SurfaceRaised { get; } = Color.FromArgb(46, 52, 70); // #2E3446 - Bề mặt nổi / Hover
    public static Color Border { get; } = Color.FromArgb(62, 70, 92); // #3E465C - Viền mỏng phân chia khối
    public static Color Text { get; } = Color.FromArgb(248, 250, 252); // #F8FAFC - Chữ chính trắng sáng
    public static Color MutedText { get; } = Color.FromArgb(160, 170, 195); // #A0AAC3 - Chữ phụ êm mắt
    public static Color Primary { get; } = Color.FromArgb(129, 140, 248); // #818CF8 - Tím xanh Indigo công nghệ
    public static Color Success { get; } = Color.FromArgb(34, 197, 94); // #22C55E - Xanh lá ngọc tươi sáng
    public static Color Warning { get; } = Color.FromArgb(245, 158, 11); // #F59E0B - Vàng hổ phách ấm áp
    public static Color Danger { get; } = Color.FromArgb(239, 68, 68); // #EF4444 - Đỏ cảnh báo nổi bật
    public static Color Info { get; } = Color.FromArgb(14, 165, 233); // #0EA5E9 - Xanh dương băng Sky

    public static void StyleButton(Button button, Color? accent = null)
    {
        var color = accent ?? Primary;
        button.BackColor = color;
        var isLight = (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) > 150;
        button.ForeColor = isLight ? Color.FromArgb(15, 23, 42) : Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = ControlPaint.Dark(color, 0.15F);
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
