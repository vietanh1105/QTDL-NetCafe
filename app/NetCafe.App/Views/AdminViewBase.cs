// Trách nhiệm: nền tảng UI chung, màu sắc và thông báo lỗi cho các Admin view.
// Liên kết: các UserControl trong thư mục Views và MainForm shell.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public abstract class AdminViewBase : UserControl, IAdminView
{
    protected AdminViewContext? Context { get; private set; }

    protected AdminViewBase()
    {
        ForeColor = CyberTheme.Text;
    }

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
        grid.BackgroundColor = CyberTheme.Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Color.FromArgb(48, 54, 72);
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 32, 44);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = CyberTheme.Primary;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        grid.RowTemplate.Height = 34;
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
        grid.DefaultCellStyle.BackColor = Color.FromArgb(34, 38, 50);
        grid.DefaultCellStyle.ForeColor = CyberTheme.Text;
        grid.DefaultCellStyle.Padding = new Padding(6, 2, 6, 2);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(79, 70, 229);
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(29, 32, 43);
        grid.AlternatingRowsDefaultCellStyle.ForeColor = CyberTheme.Text;
        grid.AlternatingRowsDefaultCellStyle.Padding = new Padding(6, 2, 6, 2);
    }
}
