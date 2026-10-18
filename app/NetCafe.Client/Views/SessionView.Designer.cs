// Designer layout cho dashboard session: các card thông tin và hàng thao tác cân đối.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.Client.Views;

public partial class SessionView
{
    private Panel _scrollPanel = null!;
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
        _customerLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        _sessionLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
        _balanceLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = Color.DarkGreen, TextAlign = ContentAlignment.MiddleLeft };
        _costLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
        _statusLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft };
        _foodButton = new Button { Text = "Gọi món", Dock = DockStyle.Fill, Height = 42, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _supportButton = new Button { Text = "Gọi hỗ trợ", Dock = DockStyle.Fill, Height = 42, BackColor = Color.FromArgb(14, 116, 144), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _logoutButton = new Button { Text = "Đăng xuất", Dock = DockStyle.Fill, Height = 42, BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _foodButton.FlatAppearance.BorderSize = 0;
        _supportButton.FlatAppearance.BorderSize = 0;
        _logoutButton.FlatAppearance.BorderSize = 0;

        _scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(243, 246, 251), Padding = new Padding(14) };
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(2)
        };
        for (var index = 0; index < 7; index++)
        {
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        content.Controls.Add(CreateCard("Thông tin khách và máy", _customerLabel), 0, 0);
        content.Controls.Add(CreateCard("Phiên hiện tại", _sessionLabel), 0, 1);
        content.Controls.Add(CreateCard("Số dư ví", _balanceLabel), 0, 2);
        content.Controls.Add(CreateCard("Thời gian và chi phí", _costLabel), 0, 3);
        content.Controls.Add(CreateCard("Heartbeat", _statusLabel), 0, 4);

        var actionRow = new TableLayoutPanel { Dock = DockStyle.Top, Height = 52, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 8, 0, 0) };
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        actionRow.Controls.Add(_foodButton, 0, 0);
        actionRow.Controls.Add(_supportButton, 1, 0);
        actionRow.Controls.Add(_logoutButton, 2, 0);
        content.Controls.Add(actionRow, 0, 5);
        content.Controls.Add(new Label { Text = "Số dư được kiểm tra ở database; nếu không đủ, phiên sẽ tự đóng.", Dock = DockStyle.Top, Height = 34, ForeColor = Color.DimGray, Padding = new Padding(2, 8, 2, 0) }, 0, 6);
        _scrollPanel.Controls.Add(content);
        Controls.Add(_scrollPanel);
        Dock = DockStyle.Fill;
    }

    private static Panel CreateCard(string title, Control content)
    {
        var card = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 0, 8), Padding = new Padding(12, 4, 12, 4) };
        card.Controls.Add(content);
        card.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 22, ForeColor = Color.FromArgb(30, 64, 175), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) });
        return card;
    }
}
