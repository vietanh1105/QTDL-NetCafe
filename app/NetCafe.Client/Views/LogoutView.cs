// Trách nhiệm: xác nhận đóng phiên trước khi MainForm gọi transaction logout.
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
            "Bạn có chắc muốn đóng phiên và thanh toán bằng ví không?",
            "Xác nhận đóng phiên",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;
    }
}
