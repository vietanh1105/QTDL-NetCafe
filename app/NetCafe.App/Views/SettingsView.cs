// Trách nhiệm: hiển thị connection/role/mode hiện tại; không lưu password trong UI.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public partial class SettingsView : AdminViewBase
{
    private readonly Label _details = new();

    public SettingsView()
    {
        InitializeComponent();
        BackColor = BackgroundColor;
        _details.Dock = DockStyle.Fill;
        _details.Padding = new Padding(20);
        _details.Font = new Font("Segoe UI", 11F);
        _details.ForeColor = Color.FromArgb(30, 41, 59);
        Controls.Add(_details);
    }

    public override Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var context = RequiredContext;
        _details.Text = $"Role hiện tại: {context.CurrentUser}\n" +
                        $"Employee ID: {context.CurrentEmployeeId}\n" +
                        "Mode: cấu hình từ environment/.env\n" +
                        "QA: dùng DB_NAME=netcafe_qa; không reset database production.";
        return Task.CompletedTask;
    }
}
