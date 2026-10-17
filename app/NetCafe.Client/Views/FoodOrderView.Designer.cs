// Designer layout cho gọi món station.

using System.Windows.Forms;

namespace NetCafe.Client.Views;

public partial class FoodOrderView
{
    private ComboBox _productBox = null!;
    private TextBox _quantityBox = null!;
    private Button _orderButton = null!;
    private Button _refreshOrdersButton = null!;
    private Button _backButton = null!;
    private Label _messageLabel = null!;
    private DataGridView _ordersGrid = null!;

    private void InitializeComponent()
    {
        _productBox = new ComboBox { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
        _quantityBox = new TextBox { Width = 80, Text = "1" };
        _orderButton = new Button { Text = "Đặt món", Width = 120, Height = 36 };
        _refreshOrdersButton = new Button { Text = "Cập nhật đơn", Width = 140, Height = 36 };
        _backButton = new Button { Text = "Quay lại", Width = 120, Height = 36 };
        _messageLabel = new Label { AutoSize = true };
        _ordersGrid = new DataGridView
        {
            Width = 560,
            Height = 170,
            ReadOnly = true,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        _ordersGrid.Columns.Add("OrderId", "Đơn");
        _ordersGrid.Columns.Add("Status", "Trạng thái");
        _ordersGrid.Columns.Add("OrderedAt", "Thời gian");
        _ordersGrid.Columns.Add("TotalAmount", "Tổng tiền");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(32) };
        panel.Controls.Add(new Label { Text = "Menu station", AutoSize = true });
        panel.Controls.Add(_productBox);
        panel.Controls.Add(_quantityBox);
        panel.Controls.Add(_orderButton);
        panel.Controls.Add(_refreshOrdersButton);
        panel.Controls.Add(new Label { Text = "Đơn đã gọi", AutoSize = true });
        panel.Controls.Add(_ordersGrid);
        panel.Controls.Add(_backButton);
        panel.Controls.Add(_messageLabel);
        Controls.Add(panel);
        Dock = DockStyle.Fill;
    }
}
