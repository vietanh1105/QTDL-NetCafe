// Designer layout cho card món Station; kích thước được FoodOrderView điều chỉnh theo cột.

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
            Height = 104,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White
        };
        _nameLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 36,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            AutoEllipsis = true
        };
        _priceLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(30, 64, 175),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        _stockLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.DimGray
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
            Height = 30,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _orderButton.FlatAppearance.BorderSize = 0;

        var actionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 2, 0, 0)
        };
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        actionPanel.Controls.Add(_quantityBox, 0, 0);
        actionPanel.Controls.Add(_orderButton, 1, 0);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        body.Controls.Add(actionPanel);
        body.Controls.Add(_stockLabel);
        body.Controls.Add(_priceLabel);
        body.Controls.Add(_nameLabel);
        body.Controls.Add(_productImage);

        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;
        Margin = new Padding(6);
        MinimumSize = new Size(170, 220);
        Size = new Size(190, 238);
        Controls.Add(body);
    }
}
