// Designer layout cho session station.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.Client.Views;

public partial class SessionView
{
    private Label _customerLabel = null!;
    private Label _sessionLabel = null!;
    private Label _balanceLabel = null!;
    private Label _costLabel = null!;
    private Label _statusLabel = null!;
    private Button _foodButton = null!;
    private Button _supportButton = null!;
    private Button _logoutButton = null!;

    private void InitializeComponent()
    {
        _customerLabel = new Label { AutoSize = true, Font = new Font("Segoe UI", 14F, FontStyle.Bold) };
        _sessionLabel = new Label { AutoSize = true };
        _balanceLabel = new Label { AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.DarkGreen };
        _costLabel = new Label { AutoSize = true };
        _statusLabel = new Label { AutoSize = true, ForeColor = Color.DimGray };
        _foodButton = new Button { Text = "Gọi món", Width = 120, Height = 36 };
        _supportButton = new Button { Text = "Gọi hỗ trợ", Width = 120, Height = 36 };
        _logoutButton = new Button { Text = "Đóng phiên", Width = 120, Height = 36, BackColor = Color.Firebrick, ForeColor = Color.White };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(32), BackColor = Color.FromArgb(243, 246, 251) };
        panel.Controls.Add(_customerLabel);
        panel.Controls.Add(_sessionLabel);
        panel.Controls.Add(_balanceLabel);
        panel.Controls.Add(_costLabel);
        panel.Controls.Add(_statusLabel);
        panel.Controls.Add(_foodButton);
        panel.Controls.Add(_supportButton);
        panel.Controls.Add(_logoutButton);
        Controls.Add(panel);
        Dock = DockStyle.Fill;
    }
}
