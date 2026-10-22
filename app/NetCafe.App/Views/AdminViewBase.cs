// Trách nhiệm: nền tảng UI chung, màu sắc và thông báo lỗi cho các Admin view.
// Liên kết: các UserControl trong thư mục Views và MainForm shell.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public abstract class AdminViewBase : UserControl, IAdminView
{
    protected AdminViewContext? Context { get; private set; }

    protected AdminViewContext RequiredContext => Context ?? throw new InvalidOperationException("Admin view chưa được gắn context.");

    protected static Color SurfaceColor => CyberTheme.Surface;
    protected static Color BackgroundColor => CyberTheme.Background;
    protected static Color PrimaryColor => CyberTheme.Primary;

    public virtual void SetContext(AdminViewContext context)
    {
        Context = context;
    }

    public abstract Task RefreshAsync(CancellationToken cancellationToken = default);

    protected void ReportError(Exception exception)
    {
        RequiredContext.ShowError(exception);
    }

    protected static void StyleGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 36;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 237, 213);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(154, 52, 18);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = CyberTheme.Text;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 215, 170);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(255, 251, 245);
    }
}
