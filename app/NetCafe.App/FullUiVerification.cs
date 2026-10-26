using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetCafe.App;

internal static class FullUiVerification
{
    public static void Run()
    {
        AppConfig.LoadEnv();
        ApplicationConfiguration.Initialize();

        using var form = new MainForm();
        form.Size = new Size(1366, 820);
        form.StartPosition = FormStartPosition.CenterScreen;
        form.Show();
        Application.DoEvents();

        // Chờ MainForm.StartAsync hoàn thành nạp dữ liệu ban đầu
        var waitStart = DateTime.UtcNow;
        while ((DateTime.UtcNow - waitStart).TotalSeconds < 7)
        {
            Application.DoEvents();
            System.Threading.Thread.Sleep(50);
        }

        var outDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            @".gemini\antigravity\brain\ae94d5fa-5717-4e9a-9076-a7e423e82306\scratch"
        );
        Directory.CreateDirectory(outDir);

        var switchViewMethod = typeof(MainForm).GetMethod(
            "SwitchView",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var refreshVisibleMethod = typeof(MainForm).GetMethod(
            "RefreshVisibleAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var errorBannerField = typeof(MainForm).GetField(
            "_errorBanner",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var errorLabelField = typeof(MainForm).GetField(
            "_errorLabel",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        var viewsToCapture = new[]
        {
            ("Settings", "full_ui_07_settings.png"),
            ("Computers", "full_ui_02_computers.png")
        };

        foreach (var (viewKey, fileName) in viewsToCapture)
        {
            switchViewMethod.Invoke(form, new object[] { viewKey });
            var task = (Task)refreshVisibleMethod.Invoke(form, new object[] { true })!;
            while (!task.IsCompleted)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(30);
            }
            Application.DoEvents();
            System.Threading.Thread.Sleep(300);
            Application.DoEvents();

            var errorBanner = (Control)errorBannerField.GetValue(form)!;
            var errorLabel = (Label)errorLabelField.GetValue(form)!;
            Console.WriteLine(
                $"[VIEW {viewKey}] ErrorBanner Visible: {errorBanner.Visible}, Error: '{errorLabel.Text}'"
            );

            using var bmp = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
            var filePath = Path.Combine(outDir, fileName);
            bmp.Save(filePath, ImageFormat.Png);
            Console.WriteLine($"[CAPTURED] Saved {filePath}");
        }

        form.Close();
        Console.WriteLine("[ALL VIEWS VERIFIED] Verification complete.");
        Environment.Exit(0);
    }
}
