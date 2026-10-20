// Designer layout cho dashboard session: card thông tin rõ ràng và thao tác dễ chạm.
// Liên kết: SessionView.cs nhận heartbeat từ StationSessionController.

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
        _customerLabel = CreateValueLabel(12F, StationTheme.Text);
        _sessionLabel = CreateValueLabel(9F, StationTheme.MutedText);
        _balanceLabel = CreateValueLabel(17F, StationTheme.Cyan);
        _costLabel = CreateValueLabel(9F, StationTheme.Text);
        _statusLabel = CreateValueLabel(8.5F, StationTheme.MutedText);
        _foodButton = new Button { Text = "Gọi món", Dock = DockStyle.Fill, Height = 44 };
        _supportButton = new Button { Text = "Hỗ trợ", Dock = DockStyle.Fill, Height = 44 };
        _logoutButton = new Button { Text = "Đăng xuất", Dock = DockStyle.Fill, Height = 44 };
        StationTheme.StyleButton(_foodButton, StationTheme.Primary);
        StationTheme.StyleButton(_supportButton, StationTheme.Purple);
        StationTheme.StyleButton(_logoutButton, StationTheme.Danger);

        _scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = StationTheme.Background,
            Padding = new Padding(14, 14, 14, 18)
        };
        var content = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = StationTheme.Background
        };
        var heading = new Panel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(2, 0, 2, 8) };
        heading.Controls.Add(new Label
        {
            Text = "PHIÊN ĐANG HOẠT ĐỘNG",
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = StationTheme.Cyan,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold)
        });
        heading.Controls.Add(new Label
        {
            Text = "Theo dõi thời gian, số dư và dịch vụ tại máy",
            Dock = DockStyle.Bottom,
            Height = 20,
            ForeColor = StationTheme.MutedText,
            Font = new Font("Segoe UI", 8.5F)
        });
        content.Controls.Add(new Label
        {
            Text = "Số dư được kiểm tra liên tục; phiên sẽ tự động kết thúc khi tài khoản không đủ tiền.",
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = StationTheme.MutedText,
            Font = new Font("Segoe UI", 8F),
            Padding = new Padding(2, 8, 2, 0)
        });
        content.Controls.Add(CreateActionRow());
        content.Controls.Add(StationTheme.CreateCard("Trạng thái tín hiệu máy", _statusLabel, StationTheme.Cyan, 74));
        content.Controls.Add(StationTheme.CreateCard("Thời gian và chi phí", _costLabel, StationTheme.Warning, 88));
        content.Controls.Add(StationTheme.CreateCard("Số dư tài khoản", _balanceLabel, StationTheme.Cyan, 86));
        content.Controls.Add(StationTheme.CreateCard("Phiên chơi hiện tại", _sessionLabel, StationTheme.Primary, 70));
        content.Controls.Add(StationTheme.CreateCard("Khách hàng và máy trạm", _customerLabel, StationTheme.Purple, 82));
        content.Controls.Add(heading);
        _scrollPanel.Controls.Add(content);
        Controls.Add(_scrollPanel);
        Dock = DockStyle.Fill;
    }

    private static Label CreateValueLabel(float size, Color color)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = color,
            Font = new Font("Segoe UI", size, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private TableLayoutPanel CreateActionRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 54,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(0, 0, 0, 10)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        row.Controls.Add(_foodButton, 0, 0);
        row.Controls.Add(_supportButton, 1, 0);
        row.Controls.Add(_logoutButton, 2, 0);
        return row;
    }
}
