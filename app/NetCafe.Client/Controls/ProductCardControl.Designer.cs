// Designer layout cho card món Station; FoodOrderView điều chỉnh card theo hai cột.
// Liên kết: ProductCardControl.cs bind SKU, tồn kho và event gọi món.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.Client.Controls;

public partial class ProductCardControl
{
    private PictureBox _productImage = null!;
    private Label _nameLabel = null!;
    private Label _priceLabel = null!;
    private Label _stockLabel = null!;
    private NumericUpDown _quantityBox = null!;
    private Button _orderButton = null!;

    private void InitializeComponent()
    {
        _productImage = new PictureBox
        {
            Dock = DockStyle.Top,
            Height = 112,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = StationTheme.SurfaceRaised,
            Padding = new Padding(8)
        };
        _nameLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            AutoEllipsis = true,
            ForeColor = StationTheme.Text
        };
        _priceLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = StationTheme.Cyan,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        _stockLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = StationTheme.MutedText
        };
        _quantityBox = new NumericUpDown
        {
            Width = 58,
            Height = 28,
            Minimum = 1,
            Maximum = 1,
            Value = 1
        };
        _orderButton = new Button
        {
            Text = "Đặt món",
            Dock = DockStyle.Fill,
            Height = 30
        };
        StationTheme.StyleButton(_orderButton, StationTheme.Primary);

        var actionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 0)
        };
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        actionPanel.Controls.Add(_quantityBox, 0, 0);
        actionPanel.Controls.Add(_orderButton, 1, 0);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = StationTheme.Surface };
        body.Controls.Add(actionPanel);
        body.Controls.Add(_stockLabel);
        body.Controls.Add(_priceLabel);
        body.Controls.Add(_nameLabel);
        body.Controls.Add(_productImage);

        BackColor = StationTheme.Surface;
        BorderStyle = BorderStyle.FixedSingle;
        Margin = new Padding(5);
        MinimumSize = new Size(150, 250);
        Size = new Size(190, 260);
        Controls.Add(body);
    }
}
