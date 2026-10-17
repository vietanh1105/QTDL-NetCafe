// Designer shell station: status machine và vùng UserControl.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.Client;

public partial class MainForm
{
    private Panel _headerPanel = null!;
    private Label _stationLabel = null!;
    private Panel _contentPanel = null!;

    private void InitializeComponent()
    {
        _headerPanel = new Panel();
        _stationLabel = new Label();
        _contentPanel = new Panel();
        SuspendLayout();
        Text = "NetCafe Station";
        Size = new Size(900, 620);
        MinimumSize = new Size(720, 500);
        StartPosition = FormStartPosition.CenterScreen;
        _headerPanel.Dock = DockStyle.Top;
        _headerPanel.Height = 58;
        _headerPanel.BackColor = Color.FromArgb(30, 64, 175);
        _stationLabel.AutoSize = true;
        _stationLabel.Location = new Point(18, 18);
        _stationLabel.ForeColor = Color.White;
        _stationLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _headerPanel.Controls.Add(_stationLabel);
        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.BackColor = Color.FromArgb(243, 246, 251);
        Controls.Add(_contentPanel);
        Controls.Add(_headerPanel);
        ResumeLayout(false);
    }
}
