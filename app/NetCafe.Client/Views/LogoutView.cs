// Trách nhiệm: placeholder control giữ contract mở rộng cho màn hình xác nhận logout.
// MainForm hiện gọi logout trực tiếp từ SessionView để không nhân đôi transaction.

namespace NetCafe.Client.Views;

public partial class LogoutView : UserControl
{
    public LogoutView()
    {
        InitializeComponent();
    }
}
