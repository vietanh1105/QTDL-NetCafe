// Trách nhiệm: ánh xạ SKU sản phẩm sang ảnh với bộ nhớ đệm (cache) trong RAM để chống giật lag.
// Liên kết: ProductCardControl.cs nhận SKU từ inventory.fn_customer_menu_v2().

using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NetCafe.Client;

public static class ProductImageCatalog
{
    private static readonly ConcurrentDictionary<string, Bitmap> MasterCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, Color> Palette = new(StringComparer.OrdinalIgnoreCase)
    {
        ["STING-RED"] = Color.FromArgb(220, 38, 38),
        ["REDBULL"] = Color.FromArgb(30, 64, 175),
        ["COCA-ZERO"] = Color.FromArgb(15, 23, 42),
        ["AQUA-500"] = Color.FromArgb(14, 165, 233),
        ["CF-SUA-DA"] = Color.FromArgb(120, 53, 15),
        ["TRA-DAO"] = Color.FromArgb(234, 88, 12),
        ["MI-TRUNG"] = Color.FromArgb(245, 158, 11),
        ["COM-XAO-BO"] = Color.FromArgb(161, 98, 7),
        ["BANH-MI-PATE"] = Color.FromArgb(180, 83, 9),
        ["BIM-BIM-KHOAI"] = Color.FromArgb(250, 204, 21),
    };

    public static Image Load(string sku)
    {
        var master = MasterCache.GetOrAdd(sku, CreateProductBitmap);
        lock (master)
        {
            return new Bitmap(master);
        }
    }

    private static Bitmap CreateProductBitmap(string sku)
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
