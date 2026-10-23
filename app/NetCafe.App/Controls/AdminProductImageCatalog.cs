// Trách nhiệm: đọc ảnh SKU dùng chung với NetCafe.Client cho menu Admin.
// Liên kết: NetCafe.Client/Assets/Products, ProductItem và OrdersView.

using System.Drawing;
using NetCafe.App.Views;

namespace NetCafe.App.Controls;

public static class AdminProductImageCatalog
{
    public static Image Load(string sku)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Products", $"{sku}.png");
        if (File.Exists(path))
        {
            using var source = Image.FromFile(path);
            return new Bitmap(source);
        }

        var fallback = new Bitmap(180, 110);
        using var graphics = Graphics.FromImage(fallback);
        graphics.Clear(CyberTheme.Primary);
        using var brush = new SolidBrush(Color.FromArgb(60, Color.White));
        graphics.FillEllipse(brush, 38, 8, 104, 94);
        return fallback;
    }
}
