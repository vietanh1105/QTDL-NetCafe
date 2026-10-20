// Designer layout cho khu Gọi món: menu hai cột và đơn đã gọi nằm bên dưới.
// Liên kết: FoodOrderView.cs nạp CustomerMenuItem và CustomerOrderStatus.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.Client.Views;

public partial class FoodOrderView
{
    private Panel _scrollPanel = null!;
    private TableLayoutPanel _menuGrid = null!;
    private FlowLayoutPanel _ordersPanel = null!;
    private Button _refreshOrdersButton = null!;
    private Button _backButton = null!;
    private Label _messageLabel = null!;

    private void InitializeComponent()
    {
        _scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = StationTheme.Background,
            Padding = new Padding(14, 14, 14, 18)
        };
        _menuGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 8),
            GrowStyle = TableLayoutPanelGrowStyle.AddRows
        };
        _menuGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _menuGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _ordersPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 8),
            BackColor = StationTheme.Background
        };
        _refreshOrdersButton = new Button
        {
            Text = "Làm mới đơn",
            Width = 132,
            Height = 34,
        };
        _backButton = new Button { Text = "Quay lại phiên", Width = 132, Height = 34 };
        StationTheme.StyleButton(_refreshOrdersButton, StationTheme.Primary);
        StationTheme.StyleButton(_backButton, StationTheme.SurfaceRaised);
        _messageLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 38,
            ForeColor = StationTheme.Cyan,
            Padding = new Padding(2, 8, 2, 0),
            AutoEllipsis = true
        };
        var title = new Label
        {
            Text = "GỌI MÓN  /  THỰC ĐƠN",
            Dock = DockStyle.Top,
            Height = 38,
            ForeColor = StationTheme.Text,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            Padding = new Padding(2, 6, 2, 0)
        };
        var ordersTitle = new Label
        {
            Text = "ĐƠN ĐÃ GỌI",
            Dock = DockStyle.Top,
            Height = 34,
            ForeColor = StationTheme.Text,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Padding = new Padding(2, 6, 2, 0)
        };
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 4, 0, 4)
        };
        actions.Controls.Add(_refreshOrdersButton);
        actions.Controls.Add(_backButton);
        var content = new Panel { Dock = DockStyle.Top, AutoSize = true, BackColor = StationTheme.Background };
        content.Controls.Add(_messageLabel);
        content.Controls.Add(actions);
        content.Controls.Add(_ordersPanel);
        content.Controls.Add(ordersTitle);
        content.Controls.Add(_menuGrid);
        content.Controls.Add(title);
        _scrollPanel.Controls.Add(content);
        Controls.Add(_scrollPanel);
        Dock = DockStyle.Fill;
    }
}
