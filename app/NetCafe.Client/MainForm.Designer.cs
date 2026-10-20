// Designer shell station: giữ kích thước 450px và tạo vùng cuộn cho các view.
// Liên kết: MainForm.cs điều phối LoginView, SessionView, FoodOrderView và SupportRequestView.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.Client;

public partial class MainForm
{
    private Panel _headerPanel = null!;
    private Label _stationLabel = null!;
    private Label _stationHint = null!;
    private Panel _contentPanel = null!;

    private void InitializeComponent()
    {
        _headerPanel = new Panel();
        _stationLabel = new Label();
        _stationHint = new Label();
        _contentPanel = new Panel();
        SuspendLayout();
        Text = "NetCafe - Máy Trạm";
        Size = new Size(450, 1350);
        MinimumSize = new Size(450, 500);
        MaximumSize = new Size(450, Screen.PrimaryScreen?.WorkingArea.Height ?? 1350);
        StartPosition = FormStartPosition.CenterScreen;
        _headerPanel.Dock = DockStyle.Top;
        BackColor = StationTheme.Background;
        _headerPanel.Height = 66;
        _headerPanel.BackColor = StationTheme.Surface;
        _stationLabel.AutoSize = true;
        _stationLabel.Location = new Point(18, 10);
        _stationLabel.ForeColor = StationTheme.Text;
        _stationLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _stationHint.AutoSize = true;
        _stationHint.Location = new Point(19, 38);
        _stationHint.Text = "CYBER STATION  •  KẾT NỐI AN TOÀN";
        _stationHint.ForeColor = StationTheme.Cyan;
        _stationHint.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        _headerPanel.Controls.Add(_stationLabel);
        _headerPanel.Controls.Add(_stationHint);
        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.AutoScroll = true;
        _contentPanel.BackColor = StationTheme.Background;
        Controls.Add(_contentPanel);
        Controls.Add(_headerPanel);
        ResumeLayout(false);
    }
}
