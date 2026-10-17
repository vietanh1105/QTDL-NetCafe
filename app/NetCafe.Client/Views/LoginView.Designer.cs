// Designer layout cho login station; không chứa credential mặc định.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.Client.Views;

public partial class LoginView
{
    private TextBox _usernameBox = null!;
    private TextBox _passwordBox = null!;
    private Button _loginButton = null!;
    private Label _errorLabel = null!;

    private void InitializeComponent()
    {
        _usernameBox = new TextBox { Width = 260, PlaceholderText = "Username" };
        _passwordBox = new TextBox { Width = 260, UseSystemPasswordChar = true, PlaceholderText = "Password" };
        _loginButton = new Button { Width = 260, Height = 36, Text = "Đăng nhập" };
        _errorLabel = new Label { Width = 320, Height = 45, ForeColor = Color.Firebrick };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(32), BackColor = Color.FromArgb(243, 246, 251) };
        panel.Controls.Add(new Label { Text = "NETCAFE STATION", Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true });
        panel.Controls.Add(new Label { Text = "Đăng nhập tài khoản khách hàng", AutoSize = true });
        panel.Controls.Add(_usernameBox);
        panel.Controls.Add(_passwordBox);
        panel.Controls.Add(_loginButton);
        panel.Controls.Add(_errorLabel);
        Controls.Add(panel);
        Dock = DockStyle.Fill;
    }
}
