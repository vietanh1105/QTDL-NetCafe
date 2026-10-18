// Designer metadata cho LogoutView.

using System.Windows.Forms;

namespace NetCafe.Client.Views;

public partial class LogoutView
{
    private void InitializeComponent()
    {
        Controls.Add(new Label { Text = "Đăng xuất được xác nhận ở SessionView.", AutoSize = true, Dock = DockStyle.Top });
        Dock = DockStyle.Fill;
    }
}
