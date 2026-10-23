// Designer shell: header, role selector, navigation, content và error banner.
// MainForm.cs chỉ điều phối lifecycle; các nghiệp vụ nằm trong Views và DbRepository.

using System.Drawing;
using System.Windows.Forms;
using NetCafe.App.Views;

namespace NetCafe.App;

public partial class MainForm
{
    private Panel _headerPanel = null!;
    private Label _titleLabel = null!;
    private Label _activeViewLabel = null!;
    private Label _dbStatusLabel = null!;
    private ComboBox _roleCombo = null!;
    private Button _refreshButton = null!;
    private FlowLayoutPanel _headerActionsPanel = null!;
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
        _headerActionsPanel = new FlowLayoutPanel();
        _sidebarPanel = new Panel();
        _navigationPanel = new FlowLayoutPanel();
        _contentPanel = new Panel();
        _errorBanner = new Panel();
        _errorLabel = new Label();
        _dismissErrorButton = new Button();
        _timer = new System.Windows.Forms.Timer();

        SuspendLayout();
        Text = "NetCafe - Quản Trị Hệ Thống";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        Size = new Size(1440, 900);
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;

        _headerPanel.Dock = DockStyle.Top;
        _headerPanel.Height = 82;
        _headerPanel.BackColor = CyberTheme.SurfaceRaised;
        _headerPanel.Padding = new Padding(18, 10, 18, 10);

        _titleLabel.AutoSize = true;
        _titleLabel.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        _titleLabel.ForeColor = Color.White;
        _titleLabel.Location = new Point(24, 10);
        _titleLabel.Text = "NETCAFE QUẢN TRỊ";

        _activeViewLabel.AutoSize = true;
        _activeViewLabel.ForeColor = Color.FromArgb(199, 210, 254);
        _activeViewLabel.Location = new Point(26, 48);
        _activeViewLabel.Text = "Đang mở: Máy trạm";

        _dbStatusLabel.AutoSize = false;
        _dbStatusLabel.Size = new Size(450, 28);
        _dbStatusLabel.ForeColor = Color.White;
        _dbStatusLabel.Location = new Point(340, 18);
        _dbStatusLabel.AutoEllipsis = true;
        _dbStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _dbStatusLabel.Text = "Vai trò: postgres";

        _roleCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _roleCombo.Width = 150;
        _roleCombo.Height = 30;
        _roleCombo.Margin = new Padding(8, 8, 0, 8);
        _roleCombo.BackColor = CyberTheme.Surface;
        _roleCombo.ForeColor = CyberTheme.Text;
        _roleCombo.FlatStyle = FlatStyle.Flat;

        _refreshButton.Width = 48;
        _refreshButton.Height = 30;
        _refreshButton.Text = "↻";
        _refreshButton.AccessibleName = "Làm mới kết nối";
        _refreshButton.Margin = new Padding(8, 8, 0, 8);
        _refreshButton.FlatStyle = FlatStyle.Flat;
        _refreshButton.BackColor = CyberTheme.Surface;
        _refreshButton.ForeColor = CyberTheme.Primary;
        _refreshButton.FlatAppearance.BorderSize = 0;

        _headerActionsPanel.Dock = DockStyle.Right;
        _headerActionsPanel.Width = 270;
        _headerActionsPanel.FlowDirection = FlowDirection.RightToLeft;
        _headerActionsPanel.WrapContents = false;
        _headerActionsPanel.BackColor = Color.Transparent;
        _headerActionsPanel.Controls.Add(_refreshButton);
        _headerActionsPanel.Controls.Add(_roleCombo);

        _headerPanel.Controls.AddRange(new Control[]
        {
            _headerActionsPanel,
            _titleLabel,
            _activeViewLabel,
            _dbStatusLabel
        });

        _sidebarPanel.Dock = DockStyle.Left;
        _sidebarPanel.Width = 215;
        _sidebarPanel.BackColor = CyberTheme.Surface;
        _sidebarPanel.Padding = new Padding(8, 14, 8, 8);

        _navigationPanel.Dock = DockStyle.Fill;
        _navigationPanel.FlowDirection = FlowDirection.TopDown;
        _navigationPanel.WrapContents = false;
        _navigationPanel.AutoScroll = true;
        _navigationPanel.BackColor = CyberTheme.Surface;
        _sidebarPanel.Controls.Add(_navigationPanel);

        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.Padding = new Padding(8);
        _contentPanel.BackColor = CyberTheme.Background;

        _errorBanner.Dock = DockStyle.Bottom;
        _errorBanner.Height = 42;
        _errorBanner.Visible = false;
        _errorBanner.BackColor = Color.FromArgb(69, 20, 35);
        _errorLabel.Dock = DockStyle.Fill;
        _errorLabel.Padding = new Padding(12, 0, 0, 0);
        _errorLabel.TextAlign = ContentAlignment.MiddleLeft;
        _errorLabel.ForeColor = Color.FromArgb(253, 164, 175);
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
