// Trách nhiệm: khung UserControl dùng chung cho bảng Admin, giảm trùng layout.
// Designer của từng view chỉ giữ metadata; dữ liệu vẫn do DbRepository cung cấp.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public abstract class AdminDataViewBase : AdminViewBase
{
    protected readonly Panel Toolbar = new();
    protected readonly Label TitleLabel = new();
    protected readonly Label InfoLabel = new();
    protected readonly Button RefreshButton = new();
    protected readonly DataGridView Grid = new();

    protected AdminDataViewBase()
    {
        BackColor = BackgroundColor;
        Dock = DockStyle.Fill;
        Padding = new Padding(12);

        Toolbar.Dock = DockStyle.Top;
        Toolbar.Height = 58;
        Toolbar.BackColor = CyberTheme.Surface;
        Toolbar.Padding = new Padding(12, 8, 12, 8);

        TitleLabel.Dock = DockStyle.Left;
        TitleLabel.AutoSize = true;
        TitleLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        TitleLabel.ForeColor = CyberTheme.Text;
        TitleLabel.TextAlign = ContentAlignment.MiddleLeft;

        InfoLabel.Dock = DockStyle.Bottom;
        InfoLabel.Height = 22;
        InfoLabel.ForeColor = CyberTheme.MutedText;
        InfoLabel.Font = new Font("Segoe UI", 8.5F);

        RefreshButton.Dock = DockStyle.Right;
        RefreshButton.Width = 105;
        RefreshButton.Text = "Làm mới";
        CyberTheme.StyleButton(RefreshButton, PrimaryColor);
        RefreshButton.Click += async (_, _) => await RefreshAsync();

        StyleGrid(Grid);
        Controls.Add(Grid);
        Controls.Add(InfoLabel);
        Controls.Add(Toolbar);
        Toolbar.Controls.Add(RefreshButton);
        Toolbar.Controls.Add(TitleLabel);
    }

    protected void SetTitle(string title, string info = "")
    {
        TitleLabel.Text = title;
        InfoLabel.Text = info;
    }

    protected void SetRows<T>(IEnumerable<T> rows, string info)
    {
        Grid.DataSource = rows.ToList();
        InfoLabel.Text = info;
    }
}
