// Designer layout cho màn hình đăng nhập Station; không chứa credential mặc định.
// Liên kết: LoginView.cs phát LoginRequested cho MainForm.cs.

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
        _usernameBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Height = 36,
            PlaceholderText = "Tên đăng nhập"
        };
        _passwordBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Height = 36,
            UseSystemPasswordChar = true,
            PlaceholderText = "Mật khẩu"
        };
        _loginButton = new Button
        {
            Dock = DockStyle.Fill,
            Height = 44,
            Text = "Vào phiên chơi"
        };
        _errorLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = StationTheme.Danger,
            TextAlign = ContentAlignment.MiddleLeft
        };
        StationTheme.StyleTextBox(_usernameBox);
        StationTheme.StyleTextBox(_passwordBox);
        StationTheme.StyleButton(_loginButton, StationTheme.Primary);

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = StationTheme.Background,
            Padding = new Padding(18, 28, 18, 18)
        };
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = StationTheme.Surface,
            Padding = new Padding(22),
            ColumnCount = 1,
            RowCount = 7
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        card.Controls.Add(new Label
        {
            Text = "NETCAFE  /  MÁY TRẠM",
            Dock = DockStyle.Fill,
            ForeColor = StationTheme.Text,
            Font = new Font("Segoe UI", 17F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        card.Controls.Add(new Label
        {
            Text = "Đăng nhập tài khoản hội viên để bắt đầu phiên",
            Dock = DockStyle.Fill,
            ForeColor = StationTheme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);
        card.Controls.Add(_usernameBox, 0, 2);
        card.Controls.Add(_passwordBox, 0, 3);
        card.Controls.Add(_loginButton, 0, 4);
        card.Controls.Add(_errorLabel, 0, 5);
        card.Controls.Add(new Label
        {
            Text = "Tính phí theo thời gian thực • cần đủ số dư để duy trì phiên",
            Dock = DockStyle.Fill,
            ForeColor = StationTheme.MutedText,
            Font = new Font("Segoe UI", 8F),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 6);
        panel.Controls.Add(card);
        Controls.Add(panel);
        Dock = DockStyle.Fill;
    }
}
