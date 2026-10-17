// Trách nhiệm: entry point của station customer; chỉ khởi tạo config và MainForm.
// Liên kết: StationConfig.cs và MainForm.cs.

namespace NetCafe.Client;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(StationConfig.Load()));
    }
}
