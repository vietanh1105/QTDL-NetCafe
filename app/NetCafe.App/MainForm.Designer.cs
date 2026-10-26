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
    private SidebarNavigationPanel _navigationPanel = null!;
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
        _navigationPanel = new SidebarNavigationPanel();
        _contentPanel = new Panel();
        _errorBanner = new Panel();
        _errorLabel = new Label();
        _dismissErrorButton = new Button();
        _timer = new System.Windows.Forms.Timer();

        SuspendLayout();
        Text = "NetCafe Management System - Cyber Platform";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        Size = new Size(1440, 900);
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;

        // Header Panel
        _headerPanel.Dock = DockStyle.Top;
        _headerPanel.Height = 70;
        _headerPanel.BackColor = CyberTheme.Surface;
        _headerPanel.Padding = new Padding(18, 8, 18, 8);

        _titleLabel.AutoSize = true;
        _titleLabel.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        _titleLabel.ForeColor = CyberTheme.Primary;
        _titleLabel.Location = new Point(20, 10);
        _titleLabel.Text = "⚡ NETCAFE PRO";

        _activeViewLabel.AutoSize = true;
        _activeViewLabel.Font = new Font("Segoe UI", 8.5F);
        _activeViewLabel.ForeColor = CyberTheme.MutedText;
        _activeViewLabel.Location = new Point(22, 42);
        _activeViewLabel.Text = "Hệ thống  ›  Máy trạm";

        _dbStatusLabel.AutoSize = false;
        _dbStatusLabel.Size = new Size(450, 28);
        _dbStatusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _dbStatusLabel.ForeColor = CyberTheme.Success;
        _dbStatusLabel.Location = new Point(260, 20);
        _dbStatusLabel.AutoEllipsis = true;
        _dbStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _dbStatusLabel.Text = "● CSDL Cloud: Online";
        _dbStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _roleCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _roleCombo.Width = 240;
        _roleCombo.Height = 30;
        _roleCombo.DropDownWidth = 300;
        _roleCombo.Margin = new Padding(8, 8, 0, 8);
        _roleCombo.BackColor = CyberTheme.SurfaceRaised;
        _roleCombo.ForeColor = CyberTheme.Text;
        _roleCombo.FlatStyle = FlatStyle.Flat;

        _refreshButton.Width = 48;
        _refreshButton.Height = 30;
        _refreshButton.Text = "↻";
        _refreshButton.AccessibleName = "Làm mới kết nối";
        _refreshButton.Margin = new Padding(8, 8, 0, 8);
        _refreshButton.FlatStyle = FlatStyle.Flat;
        _refreshButton.BackColor = CyberTheme.SurfaceRaised;
        _refreshButton.ForeColor = CyberTheme.Primary;
        _refreshButton.FlatAppearance.BorderSize = 0;

        _headerActionsPanel.Dock = DockStyle.Right;
        _headerActionsPanel.Width = 360;
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
        _headerPanel.Resize += (_, _) =>
        {
            _dbStatusLabel.Width = Math.Max(180, _headerActionsPanel.Left - _dbStatusLabel.Left - 16);
        };

        // Sidebar Panel
        _sidebarPanel.Dock = DockStyle.Left;
        _sidebarPanel.Width = 255;
        _sidebarPanel.BackColor = Color.FromArgb(28, 31, 42);
        _sidebarPanel.Padding = new Padding(8, 12, 8, 8);

        _navigationPanel.Dock = DockStyle.Fill;
        _navigationPanel.FlowDirection = FlowDirection.TopDown;
        _navigationPanel.WrapContents = false;
        _navigationPanel.AutoScroll = true;
        _navigationPanel.BackColor = Color.FromArgb(28, 31, 42);
        _navigationPanel.HorizontalScroll.Maximum = 0;
        _navigationPanel.HorizontalScroll.Visible = false;
        _navigationPanel.HorizontalScroll.Enabled = false;
        _sidebarPanel.Controls.Add(_navigationPanel);

        // Content Panel
        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.Padding = new Padding(8);
        _contentPanel.BackColor = CyberTheme.Background;

        // Error Banner
        _errorBanner.Dock = DockStyle.Bottom;
        _errorBanner.Height = 40;
        _errorBanner.Visible = false;
        _errorBanner.BackColor = Color.FromArgb(70, 20, 30);
        _errorLabel.Dock = DockStyle.Fill;
        _errorLabel.Padding = new Padding(12, 0, 0, 0);
        _errorLabel.TextAlign = ContentAlignment.MiddleLeft;
        _errorLabel.ForeColor = Color.FromArgb(254, 202, 202);
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

    private sealed class SidebarNavigationPanel : FlowLayoutPanel
    {
        public SidebarNavigationPanel()
        {
            DoubleBuffered = true;
            AutoScroll = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style &= ~0x00100000; // WS_HSCROLL: Triệt tiêu thanh cuộn ngang Windows
                return cp;
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            HorizontalScroll.Visible = false;
            HorizontalScroll.Maximum = 0;
            HorizontalScroll.Enabled = false;
        }
    }
}
