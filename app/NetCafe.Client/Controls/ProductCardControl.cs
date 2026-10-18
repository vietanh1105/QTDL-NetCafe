// Trách nhiệm: hiển thị một sản phẩm thật của database và phát yêu cầu đặt món.
// Liên kết: StationModels.CustomerMenuItem, ProductImageCatalog và FoodOrderView.

using NetCafe.Data;

namespace NetCafe.Client.Controls;

public partial class ProductCardControl : UserControl
{
    private CustomerMenuItem? _product;

    public event Func<int, int, Task>? OrderRequested;

    public ProductCardControl()
    {
        InitializeComponent();
        _orderButton.Click += async (_, _) =>
        {
            if (_product is null || _product.StockQty <= 0)
            {
                return;
            }

            if (OrderRequested is not null)
            {
                await OrderRequested(_product.ProductId, (int)_quantityBox.Value);
            }
        };
    }

    public void Bind(CustomerMenuItem product)
    {
        _product = product;
        _nameLabel.Text = product.ProductName;
        _priceLabel.Text = $"{product.UnitPrice:N0} đ";
        _stockLabel.Text = product.StockQty > 0 ? $"Còn {product.StockQty}" : "Hết món";
        _quantityBox.Minimum = 1;
        _quantityBox.Maximum = Math.Max(1, product.StockQty);
        _quantityBox.Value = 1;
        _quantityBox.Enabled = product.StockQty > 0;
        _orderButton.Enabled = product.StockQty > 0;
        _orderButton.Text = product.StockQty > 0 ? "Đặt món" : "Hết hàng";

        var nextImage = ProductImageCatalog.Load(product.Sku);
        var previousImage = _productImage.Image;
        _productImage.Image = nextImage;
        previousImage?.Dispose();
    }
}
