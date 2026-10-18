// Trách nhiệm: ánh xạ SKU sản phẩm thật sang ảnh trong repo và tạo fallback ổn định.
// Liên kết: ProductCardControl.cs nhận SKU từ inventory.fn_customer_menu_v2().

using System.Drawing;
using System.Drawing.Drawing2D;

namespace NetCafe.Client;

public static class ProductImageCatalog
{
    private static readonly Dictionary<string, Color> Palette = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NUOC-NGOT"] = Color.FromArgb(239, 68, 68),
        ["CAFE-DEN"] = Color.FromArgb(120, 53, 15),
        ["TRA-DAO"] = Color.FromArgb(234, 88, 12),
        ["MI-TRUNG"] = Color.FromArgb(245, 158, 11),
        ["BIM-BIM-KHOAI"] = Color.FromArgb(250, 204, 21),
    };

    public static Image Load(string sku)
    {
        var assetPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Products", $"{sku}.png");
        if (File.Exists(assetPath))
        {
            using var source = Image.FromFile(assetPath);
            return new Bitmap(source);
        }

        // Fallback vẽ tại chỗ để SKU mới không tạo ô ảnh rỗng hoặc lỗi file.
        var bitmap = new Bitmap(160, 112);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Palette.GetValueOrDefault(sku, Color.FromArgb(37, 99, 235)));
        using var brush = new SolidBrush(Color.FromArgb(60, Color.White));
        graphics.FillEllipse(brush, 34, 12, 92, 92);
        using var pen = new Pen(Color.FromArgb(210, Color.White), 3F);
        graphics.DrawEllipse(pen, 34, 12, 92, 92);
        return bitmap;
    }
}
