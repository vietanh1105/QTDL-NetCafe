// Trách nhiệm: xác nhận đăng xuất trước khi MainForm gọi transaction logout.
// Liên kết: SessionView phát yêu cầu và MainForm thực hiện sp_station_logout.

using System.Windows.Forms;

namespace NetCafe.Client.Views;

public partial class LogoutView : UserControl
{
    public LogoutView()
    {
        InitializeComponent();
    }

    public static bool Confirm(IWin32Window? owner)
    {
        return MessageBox.Show(
            owner,
            "Bạn có chắc muốn đăng xuất và thanh toán bằng ví không?",
            "Xác nhận đăng xuất",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;
    }
}
