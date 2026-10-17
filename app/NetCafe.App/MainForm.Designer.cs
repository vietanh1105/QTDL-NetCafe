// Designer shell: header, role selector, navigation, content và error banner.
// MainForm.cs chỉ điều phối lifecycle; các nghiệp vụ nằm trong Views và DbRepository.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App;

public partial class MainForm
{
    private Panel _headerPanel = null!;
    private Label _titleLabel = null!;
    private Label _activeViewLabel = null!;
    private Label _dbStatusLabel = null!;
    private ComboBox _roleCombo = null!;
    private Button _refreshButton = null!;
    private Panel _sidebarPanel = null!;
    private FlowLayoutPanel _navigationPanel = null!;
    private Panel _contentPanel = null!;
    private Panel _errorBanner = null!;
    private Label _errorLabel = null!;
    private Button _dismissErrorButton = null!;
    private System.Windows.Forms.Timer _timer = null!;

    private void InitializeComponent()
    {
        _headerPanel = new Panel();
        _titleLabel = new Label();
        _activeViewLabel = new Label();
        _dbStatusLabel = new Label();
        _roleCombo = new ComboBox();
        _refreshButton = new Button();
        _sidebarPanel = new Panel();
        _navigationPanel = new FlowLayoutPanel();
        _contentPanel = new Panel();
        _errorBanner = new Panel();
        _errorLabel = new Label();
        _dismissErrorButton = new Button();
        _timer = new System.Windows.Forms.Timer();

        SuspendLayout();
        Text = "NetCafe Admin";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        Size = new Size(1440, 900);
        BackColor = Color.FromArgb(243, 246, 251);

        _headerPanel.Dock = DockStyle.Top;
        _headerPanel.Height = 74;
        _headerPanel.BackColor = Color.FromArgb(30, 64, 175);
        _headerPanel.Padding = new Padding(18, 10, 18, 10);

        _titleLabel.AutoSize = true;
        _titleLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        _titleLabel.ForeColor = Color.White;
        _titleLabel.Location = new Point(18, 10);
        _titleLabel.Text = "NETCAFE ADMIN";

        _activeViewLabel.AutoSize = true;
        _activeViewLabel.ForeColor = Color.FromArgb(219, 234, 254);
        _activeViewLabel.Location = new Point(20, 43);
        _activeViewLabel.Text = "Đang mở: Computers";

        _dbStatusLabel.AutoSize = true;
        _dbStatusLabel.ForeColor = Color.White;
        _dbStatusLabel.Location = new Point(330, 16);
        _dbStatusLabel.Text = "Role: postgres";

        _roleCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _roleCombo.Location = new Point(660, 14);
        _roleCombo.Width = 150;

        _refreshButton.Location = new Point(820, 13);
        _refreshButton.Width = 105;
        _refreshButton.Height = 30;
        _refreshButton.Text = "Làm mới";
        _refreshButton.FlatStyle = FlatStyle.Flat;
        _refreshButton.BackColor = Color.White;
        _refreshButton.ForeColor = Color.FromArgb(30, 64, 175);
        _refreshButton.FlatAppearance.BorderSize = 0;

        _headerPanel.Controls.AddRange(new Control[] { _titleLabel, _activeViewLabel, _dbStatusLabel, _roleCombo, _refreshButton });

        _sidebarPanel.Dock = DockStyle.Left;
        _sidebarPanel.Width = 215;
        _sidebarPanel.BackColor = Color.White;
        _sidebarPanel.Padding = new Padding(8, 14, 8, 8);

        _navigationPanel.Dock = DockStyle.Fill;
        _navigationPanel.FlowDirection = FlowDirection.TopDown;
        _navigationPanel.WrapContents = false;
        _navigationPanel.AutoScroll = true;
        _navigationPanel.BackColor = Color.White;
        _sidebarPanel.Controls.Add(_navigationPanel);

        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.Padding = new Padding(8);
        _contentPanel.BackColor = Color.FromArgb(243, 246, 251);

        _errorBanner.Dock = DockStyle.Bottom;
        _errorBanner.Height = 42;
        _errorBanner.Visible = false;
        _errorBanner.BackColor = Color.FromArgb(254, 226, 226);
        _errorLabel.Dock = DockStyle.Fill;
        _errorLabel.Padding = new Padding(12, 0, 0, 0);
        _errorLabel.TextAlign = ContentAlignment.MiddleLeft;
        _errorLabel.ForeColor = Color.FromArgb(185, 28, 28);
        _dismissErrorButton.Dock = DockStyle.Right;
        _dismissErrorButton.Width = 90;
        _dismissErrorButton.Text = "Đóng";
        _errorBanner.Controls.Add(_errorLabel);
        _errorBanner.Controls.Add(_dismissErrorButton);

        Controls.Add(_contentPanel);
        Controls.Add(_sidebarPanel);
        Controls.Add(_errorBanner);
        Controls.Add(_headerPanel);
        ResumeLayout(false);
    }
}
