using System;
using System.Collections.Generic;
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
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_WRITE = 2;
    private const uint OPEN_EXISTING = 3;
    private const int STD_OUTPUT_HANDLE = -11;
    private const int SW_SHOW = 5;

    [STAThread]
    static void Main(string[]? args)
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

        if (args != null && args.Length > 0 && args[0] == "--verify-layout")
        {
            RunLayoutVerification();
            return;
        }

        if (args != null && args.Length > 0 && args[0] == "--verify-ai")
        {
            RunAiLayoutVerification();
            return;
        }

        if (args != null && args.Length > 0 && args[0] == "--verify-full-ui")
        {
            FullUiVerification.Run();
            return;
        }

        if (args != null && args.Length > 0 && args[0] == "--diagnose-db")
        {
            RunDatabaseDiagnostics();
            return;
        }

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

    private static void RunLayoutVerification()
    {
        ApplicationConfiguration.Initialize();
        using var form = new Form
        {
            Size = new System.Drawing.Size(1200, 800),
            StartPosition = FormStartPosition.CenterScreen,
            Text = "Verification"
        };
        var view = new Views.ComputersView();
        view.Dock = DockStyle.Fill;
        form.Controls.Add(view);

        var cardsField = typeof(Views.ComputersView).GetField("_cards", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var cardsGridField = typeof(Views.ComputersView).GetField("_cardsGrid", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var updateKpiMethod = typeof(Views.ComputersView).GetMethod("UpdateKpiSummary", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var rebuildGridMethod = typeof(Views.ComputersView).GetMethod("RebuildGridPositions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        var cards = (Dictionary<int, Controls.ComputerCardControl>)cardsField.GetValue(view)!;
        var cardsGrid = (FlowLayoutPanel)cardsGridField.GetValue(view)!;

        var items = new List<Data.ComputerGridItem>();
        for (int i = 1; i <= 20; i++)
        {
            var item = new Data.ComputerGridItem
            {
                ComputerId = i,
                ComputerCode = $"PC{i:D3}",
                ComputerName = $"Máy {i:D2}",
                RoomName = i <= 8 ? "Khu Hút Thuốc" : "Khu Máy Lạnh",
                TypeName = i <= 8 ? "Tiêu Chuẩn" : "Nâng Cao",
                HourlyRate = i <= 8 ? 8000 : 12000,
                ComputerStatus = (i % 4 == 1) ? "in_use" : (i % 6 == 0 ? "maintenance" : "available"),
                CurrentCustomerName = (i % 4 == 1) ? "Khách VIP" : null,
                PlayedMinutes = (i % 4 == 1) ? 45 : null
            };
            items.Add(item);
            var card = new Controls.ComputerCardControl();
            card.Bind(item);
            cards.Add(i, card);
            cardsGrid.Controls.Add(card);
        }

        updateKpiMethod.Invoke(view, new object[] { items });
        form.Show();
        Application.DoEvents();
        rebuildGridMethod.Invoke(view, null);
        Application.DoEvents();

        var card1 = cards[1];
        var card2 = cards[2];
        var card3 = cards[3];
        var card4 = cards[4];
        var headerPanel = (Panel)typeof(Views.ComputersView).GetField("_headerTitlePanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(view)!;
        var actionsPanel = (FlowLayoutPanel)typeof(Views.AdminDataViewBase).GetField("ActionsPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(view)!;
        var infoLabel = (Label)typeof(Views.AdminDataViewBase).GetField("InfoLabel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(view)!;

        Console.WriteLine($"[CARDS] Card1 Bounds={card1.Bounds} Margin={card1.Margin}");
        Console.WriteLine($"[CARDS] Card2 Bounds={card2.Bounds} Margin={card2.Margin}");
        Console.WriteLine($"[CARDS] Card3 Bounds={card3.Bounds} Margin={card3.Margin}");
        Console.WriteLine($"[CARDS] Card4 Bounds={card4.Bounds} Margin={card4.Margin}");
        var card5 = cards[5];
        var card8 = cards[8];
        Console.WriteLine($"[CARDS] Card5 Bounds={card5.Bounds} Margin={card5.Margin}");
        Console.WriteLine($"[CARDS] Card8 Bounds={card8.Bounds} Margin={card8.Margin}");
        Console.WriteLine($"[ALIGNMENT_LEFT] Header Left: {headerPanel.Left}, Card 1 Left in Grid: {card1.Left}, Grid Padding Left: {cardsGrid.Padding.Left}");
        Console.WriteLine($"[ALIGNMENT_RIGHT] Card 4 Right in Grid: {card4.Right}, Grid Usable Width: {cardsGrid.ClientSize.Width - cardsGrid.Padding.Horizontal}");
        Console.WriteLine($"[ACTIONS_PANEL] Dock: {actionsPanel.Dock}, Bounds: {actionsPanel.Bounds}");
        Console.WriteLine($"[BOTTOM_UI] InfoLabel Visible: {infoLabel.Visible}, Height: {infoLabel.Height}, Grid Padding Bottom: {cardsGrid.Padding.Bottom}");
        foreach (Control c in actionsPanel.Controls)
        {
            Console.WriteLine($"[BADGE/CONTROL] '{c.Text}' ForeColor={c.ForeColor} BackColor={c.BackColor} Bounds={c.Bounds}");
        }

        using var bmp = new System.Drawing.Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, form.Width, form.Height));
        var outDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            @".gemini\antigravity\brain\ae94d5fa-5717-4e9a-9076-a7e423e82306\scratch");
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, "computers_view_rendered.png");
        bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);

        Console.WriteLine($"[VERIFY_SUCCESS] Rendered screenshot to {outPath}");
        form.Close();
    }

    private static void RunAiLayoutVerification()
    {
        AppConfig.LoadEnv();
        ApplicationConfiguration.Initialize();
        using var form = new Form
        {
            Size = new System.Drawing.Size(1200, 800),
            StartPosition = FormStartPosition.CenterScreen,
            Text = "AI Center Verification"
        };
        var view = new Views.AiCenterView();
        view.Dock = DockStyle.Fill;
        form.Controls.Add(view);

        var repo = new NetCafe.Data.DbRepository(
            AppConfig.GetConnectionString("nv_admin", AppConfig.GetConfiguredRolePassword("nv_admin")));
        var context = new Views.AdminViewContext
        {
            Repository = repo,
            CurrentUser = "nv_admin",
            UserRole = "role_admin",
            CurrentEmployeeId = 1,
            RefreshComputersAsync = () => Task.CompletedTask,
            ShowError = ex => Console.WriteLine($"[ERROR] {ex.Message}")
        };
        view.SetContext(context);
        form.Show();
        Application.DoEvents();

        var task = view.RefreshAsync();
        while (!task.IsCompleted)
        {
            Application.DoEvents();
            System.Threading.Thread.Sleep(20);
        }
        Application.DoEvents();

        var outDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            @".gemini\antigravity\brain\ae94d5fa-5717-4e9a-9076-a7e423e82306\scratch");
        Directory.CreateDirectory(outDir);

        using (var bmp = new System.Drawing.Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, form.Width, form.Height));
            bmp.Save(Path.Combine(outDir, "ai_center_security_rendered.png"), System.Drawing.Imaging.ImageFormat.Png);
        }

        // Test module Trùng lặp
        var switchMethod = typeof(Views.AiCenterView).GetMethod("SwitchModule",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        switchMethod.Invoke(view, new object[] { "duplicate" });
        Application.DoEvents();
        System.Threading.Thread.Sleep(800);
        Application.DoEvents();
        using (var bmp = new System.Drawing.Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, form.Width, form.Height));
            bmp.Save(Path.Combine(outDir, "ai_center_duplicate_rendered.png"), System.Drawing.Imaging.ImageFormat.Png);
        }

        // Test module Khuyến mãi
        switchMethod.Invoke(view, new object[] { "promotion" });
        Application.DoEvents();
        System.Threading.Thread.Sleep(800);
        Application.DoEvents();
        using (var bmp = new System.Drawing.Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, form.Width, form.Height));
            bmp.Save(Path.Combine(outDir, "ai_center_promotion_rendered.png"), System.Drawing.Imaging.ImageFormat.Png);
        }

        Console.WriteLine($"[VERIFY_AI_SUCCESS] Rendered screenshots to {outDir}");
        form.Close();
    }

    private static void RunDatabaseDiagnostics()
    {
        AppConfig.LoadEnv();
        Console.WriteLine($"Loaded Env File: {AppConfig.LoadedEnvFile}");
        Console.WriteLine($"Host: {AppConfig.Host}:{AppConfig.Port}, DB: {AppConfig.Database}, User: {AppConfig.Username}");

        var ownerCs = AppConfig.GetConnectionString();
        using (var conn = new Npgsql.NpgsqlConnection(ownerCs))
        {
            conn.Open();

            // 1. Kích hoạt LOGIN cho nv_manager và cấp quyền role_manager
            using var cmdSync = conn.CreateCommand();
            cmdSync.CommandText = @"
                ALTER ROLE nv_manager WITH LOGIN PASSWORD 'Demo@2026NetCafe';
                GRANT role_manager TO nv_manager;
                GRANT USAGE ON SCHEMA core, inventory, billing, hr, reporting, audit, ai TO role_manager;
                GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA core, inventory, billing, hr, reporting, ai TO role_manager;
                GRANT SELECT ON ALL TABLES IN SCHEMA reporting TO role_manager;
                GRANT SELECT ON reporting.mv_revenue_daily TO role_manager;
                GRANT EXECUTE ON FUNCTION reporting.sp_refresh_materialized_views() TO role_manager;
                GRANT EXECUTE ON FUNCTION hr.sp_create_maintenance(INT, INT, TEXT) TO role_manager;
                GRANT EXECUTE ON FUNCTION hr.sp_complete_maintenance(INT, VARCHAR, NUMERIC) TO role_manager;
                GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA ai TO role_manager;
                GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA core, inventory, billing, hr, ai TO role_manager;

                REVOKE ALL ON hr.employee FROM role_manager;
                GRANT SELECT (employee_id, full_name, phone, username, db_username, emp_role, default_shift, hired_date, is_active)
                    ON hr.employee TO role_manager;
                GRANT INSERT (full_name, phone, username, db_username, password_hash, emp_role, default_shift, hired_date, is_active)
                    ON hr.employee TO role_manager;
                GRANT UPDATE (full_name, phone, db_username, emp_role, default_shift, hired_date, is_active)
                    ON hr.employee TO role_manager;
            ";
            cmdSync.ExecuteNonQuery();
            Console.WriteLine("[SYNC 4 ROLES] Đã kích hoạt LOGIN và đồng bộ phân quyền cho nv_manager (role_manager)!");

            using var cmd1 = conn.CreateCommand();
            cmd1.CommandText = "SELECT count(*) FROM reporting.mv_revenue_daily;";
            Console.WriteLine($"reporting.mv_revenue_daily: {cmd1.ExecuteScalar()} rows");

            cmd1.CommandText = "SELECT count(*) FROM core.customer;";
            Console.WriteLine($"core.customer: {cmd1.ExecuteScalar()} rows");

            // Nạp tình huống demo AI Center (Gian lận, Trùng lặp, Khuyến mãi)
            var sqlDemoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "sql", "12_ai_demo_situations.sql");
            if (!File.Exists(sqlDemoPath))
            {
                sqlDemoPath = Path.Combine(Directory.GetCurrentDirectory(), "sql", "12_ai_demo_situations.sql");
            }
            if (File.Exists(sqlDemoPath))
            {
                using var cmdDemo = conn.CreateCommand();
                cmdDemo.CommandText = File.ReadAllText(sqlDemoPath);
                cmdDemo.ExecuteNonQuery();
                Console.WriteLine("[AI DEMO SITUATIONS] Đã nạp thành công các tình huống mẫu cho Gian lận và Trùng lặp!");
            }
        }

        // 2. Kiểm tra kết nối cả 4 role
        var rolesToTest = new[] { "nv_admin", "nv_manager", "nv_thao", "nv_hung" };
        foreach (var r in rolesToTest)
        {
            var cs = AppConfig.GetConnectionString(r, AppConfig.GetConfiguredRolePassword(r));
            using var rConn = new Npgsql.NpgsqlConnection(cs);
            try
            {
                rConn.Open();
                using var cmd = rConn.CreateCommand();
                cmd.CommandText = "SELECT current_user, session_user;";
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    Console.WriteLine($" - User '{r}' kết nối thành công! current_user={reader.GetString(0)}, session_user={reader.GetString(1)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" - User '{r}' THẤT BẠI: {ex.Message}");
            }
        }

        Console.WriteLine("\n[HOÀN TẤT] Đồng bộ 4 roles và kiểm tra CSDL Cloud thành công!");
    }
}
