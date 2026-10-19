// Designer layout cho menu Station: hai cột card, đơn đã gọi nằm bên dưới và cuộn nội bộ.

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
        _scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(243, 246, 251) };
        _menuGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(4),
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
            Padding = new Padding(4)
        };
        _refreshOrdersButton = new Button
        {
            Text = "Làm mới đơn",
            Width = 132,
            Height = 34,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _backButton = new Button { Text = "Quay lại", Width = 112, Height = 34 };
        _refreshOrdersButton.FlatAppearance.BorderSize = 0;
        _messageLabel = new Label { Dock = DockStyle.Top, Height = 38, ForeColor = Color.FromArgb(30, 64, 175), Padding = new Padding(4, 8, 4, 0) };
        var title = new Label { Text = "Thực đơn món ăn & đồ uống", Dock = DockStyle.Top, Height = 36, Font = new Font("Segoe UI", 14F, FontStyle.Bold), Padding = new Padding(4, 6, 4, 0) };
        var ordersTitle = new Label { Text = "Danh sách đơn đã gọi", Dock = DockStyle.Top, Height = 34, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Padding = new Padding(4, 6, 4, 0) };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(4, 4, 4, 4) };
        actions.Controls.Add(_refreshOrdersButton);
        actions.Controls.Add(_backButton);
        var content = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(14) };
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
