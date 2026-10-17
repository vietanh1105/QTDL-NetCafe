// Trách nhiệm: nền tảng UI chung, màu sắc và thông báo lỗi cho các Admin view.
// Liên kết: các UserControl trong thư mục Views và MainForm shell.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public abstract class AdminViewBase : UserControl, IAdminView
{
    protected AdminViewContext? Context { get; private set; }

    protected AdminViewContext RequiredContext => Context ?? throw new InvalidOperationException("Admin view chưa được gắn context.");

    protected static Color SurfaceColor => Color.White;
    protected static Color BackgroundColor => Color.FromArgb(243, 246, 251);
    protected static Color PrimaryColor => Color.FromArgb(37, 99, 235);

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
        grid.BackgroundColor = SurfaceColor;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 36;
        grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryColor;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 58, 138);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
    }
}
