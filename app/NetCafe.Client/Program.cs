// Trách nhiệm: entry point của station customer; chỉ khởi tạo config và MainForm.
// Liên kết: StationConfig.cs và MainForm.cs.

namespace NetCafe.Client;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var viCulture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.GetCultureInfo("vi-VN").Clone();
        viCulture.NumberFormat.NumberGroupSeparator = ".";
        viCulture.NumberFormat.NumberDecimalSeparator = ",";
        viCulture.NumberFormat.CurrencyGroupSeparator = ".";
        viCulture.NumberFormat.CurrencyDecimalSeparator = ",";
        viCulture.NumberFormat.CurrencySymbol = "đ";

        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = viCulture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = viCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = viCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = viCulture;
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(StationConfig.Load()));
    }
}
