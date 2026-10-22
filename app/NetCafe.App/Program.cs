using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;

namespace NetCafe.App;

static class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_WRITE = 2;
    private const uint OPEN_EXISTING = 3;
    private const int STD_OUTPUT_HANDLE = -11;
    private const int SW_SHOW = 5;

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
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
        // 1. Tách khỏi terminal hiện tại và ép Windows tạo một CỬA SỔ CMD ĐỘC LẬP MỚI
        FreeConsole();
        AllocConsole();

        IntPtr hwnd = GetConsoleWindow();
        if (hwnd != IntPtr.Zero)
        {
            ShowWindow(hwnd, SW_SHOW);
            // Đặt vị trí và kích thước cửa sổ CMD: rộng 950px, cao 520px
            SetWindowPos(hwnd, IntPtr.Zero, 60, 60, 960, 520, 0x0040);
        }

        // 2. Chuyển hướng toàn bộ Console.Out vào cửa sổ CMD mới mở
        try
        {
            IntPtr hOut = CreateFile("CONOUT$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            SetStdHandle(STD_OUTPUT_HANDLE, hOut);

            var safeHandle = new SafeFileHandle(hOut, ownsHandle: false);
            var fs = new FileStream(safeHandle, FileAccess.Write);
            var writer = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };
            Console.SetOut(writer);

            Console.Title = "NetCafe - Cửa sổ Giám sát Truy vấn CSDL PostgreSQL 17 (SQL Real-time Monitor)";
            Console.OutputEncoding = Encoding.UTF8;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════════════════════════════╗
║                NETCAFE MANAGEMENT SYSTEM - POSTGRESQL 17 SQL MONITOR                  ║
║        Cửa sổ giám sát thời gian thực mọi câu lệnh SQL, thời gian thực thi & AI       ║
╚═══════════════════════════════════════════════════════════════════════════════════════╝
");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚡ Hệ thống đã kết nối CSDL và sẵn sàng lắng nghe mọi truy vấn thời gian thực...\n");
            Console.ResetColor();
        }
        catch
        {
            // Bỏ qua lỗi console nếu môi trường đặc biệt
        }

        // 3. Khởi chạy giao diện chính Windows Forms
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }    
}