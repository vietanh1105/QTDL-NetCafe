using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using NetCafe.Data;

namespace NetCafe.App
{
    public class MainForm : Form
    {
        private DbRepository _repo;
        private readonly CancellationTokenSource _cts = new();

        // 1. Top Header
        private Guna2CustomGradientPanel _pnlHeader = null!;
        private Label _lblAppTitle = null!;
        private Label _lblSubtitle = null!;
        private Label _lblRolePrompt = null!;
        private Guna2ComboBox _cboRole = null!;
        private Guna2GradientButton _btnRefresh = null!;

        // 2. Left Navigation Sidebar
        private Guna2Panel _pnlSidebar = null!;
        private Label _lblSidebarMenuTitle = null!;
        private Guna2Button _btnNavComputers = null!;
        private Guna2Button _btnNavCustomers = null!;
        private Guna2Button _btnNavBooking = null!;
        private Guna2Button _btnNavOrders = null!;
        private Guna2Button _btnNavInvoices = null!;
        private Guna2Button _btnNavReports = null!;
        private Guna2Panel _pnlDbStatus = null!;
        private Label _lblDbStatus = null!;

        // 3. Main Views Container
        private Panel _pnlBody = null!;
        private Panel _pnlMainContainer = null!;
        private Panel _viewComputers = null!;
        private Panel _viewCustomers = null!;
        private Panel _viewBooking = null!;
        private Panel _viewOrders = null!;
        private Panel _viewInvoices = null!;
        private Panel _viewReports = null!;

        // View 1: Sơ đồ máy (Computers)
        private FlowLayoutPanel _pnlComputerGrid = null!;
        private Guna2Panel _pnlCompDetail = null!;
        private Label _lblCompDetailTitle = null!;
        private Label _lblCompDetailInfo = null!;
        private Guna2ComboBox _cboCustForSession = null!;
        private Guna2GradientButton _btnOpenSession = null!;
        private Guna2GradientButton _btnCloseSessionBalance = null!;
        private Guna2GradientButton _btnCloseSessionCash = null!;
        private ComputerGridItem? _selectedComp;

        // View 2: Khách hàng (Customers)
        private Guna2TextBox _txtSearchCust = null!;
        private Guna2Button _btnSearchCust = null!;
        private Guna2TextBox _txtTopUpAmount = null!;
        private Guna2GradientButton _btnTopUp = null!;
        private Guna2Button _btnTestInvalidPhone = null!;
        private DataGridView _dgvCustomers = null!;

        // View 3: Đặt trước (Booking)
        private Guna2ComboBox _cboBookingComp = null!;
        private Guna2ComboBox _cboBookingCust = null!;
        private DateTimePicker _dtpFrom = null!;
        private DateTimePicker _dtpTo = null!;
        private Guna2GradientButton _btnCreateBooking = null!;
        private Guna2Button _btnTestOverbooking = null!;
        private DataGridView _dgvBookings = null!;

        // View 4: Gọi món F&B (Orders)
        private Guna2ComboBox _cboOrderSession = null!;
        private Guna2ComboBox _cboOrderProduct = null!;
        private NumericUpDown _numOrderQty = null!;
        private Guna2GradientButton _btnPlaceOrder = null!;
        private Guna2Button _btnTestOversell = null!;
        private DataGridView _dgvProducts = null!;

        // View 5: Hóa đơn (Invoices)
        private DataGridView _dgvInvoices = null!;

        // View 6: Báo cáo (Reports)
        private RadioButton _radDailyRev = null!;
        private RadioButton _radCompUsage = null!;
        private RadioButton _radTopCust = null!;
        private DataGridView _dgvReports = null!;

        // 4. Bottom Error Banner
        private Panel _bottomContainer = null!;
        private Guna2Panel _pnlErrorBanner = null!;
        private Label _lblErrorMessage = null!;
        private Guna2Button _btnDismissError = null!;
        private System.Windows.Forms.Timer _refreshTimer = null!;

        public MainForm()
        {
            AppConfig.LoadEnv();
            _repo = new DbRepository(AppConfig.GetConnectionString());
            _repo.OnSqlExecuted += HandleSqlExecuted;
            _repo.OnNotificationReceived += HandleNotification;

            InitializeGunaComponents();
            _ = InitializeDataAsync();
        }

        private void InitializeGunaComponents()
        {
            Text = "NetCafe Management System - Quản trị CSDL (PostgreSQL 17)";
            Size = new Size(1360, 840);
            MinimumSize = new Size(1200, 720);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.FromArgb(243, 246, 251);
            Icon = SystemIcons.Application;

            // Xây dựng các cụm giao diện
            BuildTopHeader();
            BuildBottomSection();
            BuildSidebar();
            BuildMainContainerViews();

            // ====================================================================
            // KIẾN TRÚC DOCKING ĐỘC LẬP - BẢO ĐẢM KHÔNG BAO GIỜ BỊ ĐÈ NHAU
            // ====================================================================
            // 1. _pnlBody nằm giữa Header và Bottom (Dock = Fill).
            // 2. Bên trong _pnlBody: _pnlSidebar (Dock = Left) và _pnlMainContainer (Dock = Fill).
            // Điều này cô lập hoàn toàn nội dung các tab, không thể trượt lên đè Header
            // hay làm mất Sidebar dù có chuyển tab hay vẽ lại màn hình thế nào!
            _pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(243, 246, 251)
            };

            _pnlBody.Controls.Add(_pnlMainContainer);
            _pnlBody.Controls.Add(_pnlSidebar);

            Controls.Add(_pnlBody);
            Controls.Add(_bottomContainer);
            Controls.Add(_pnlHeader);

            // Auto-refresh sơ đồ máy mỗi 3 giây
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            _refreshTimer.Tick += async (s, e) =>
            {
                if (_viewComputers.Visible)
                {
                    await LoadComputerGridAsync(silent: true);
                }
            };
            _refreshTimer.Start();
        }

        // ====================================================================
        // 1. TOP HEADER (CỐ ĐỊNH TRÊN CÙNG)
        // ====================================================================
        private void BuildTopHeader()
        {
            _pnlHeader = new Guna2CustomGradientPanel
            {
                Dock = DockStyle.Top,
                Height = 65,
                FillColor = Color.FromArgb(30, 64, 175),      // Xanh Royal
                FillColor2 = Color.FromArgb(37, 99, 235),     // Xanh Cobalt
                FillColor3 = Color.FromArgb(14, 165, 233),    // Sky Blue
                FillColor4 = Color.FromArgb(16, 185, 129),    // Emerald
                Padding = new Padding(16, 6, 16, 6)
            };

            _lblAppTitle = new Label
            {
                Text = "🎮 NETCAFE MANAGEMENT SYSTEM",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 10),
                BackColor = Color.Transparent
            };

            _lblSubtitle = new Label
            {
                Text = "Đồ án Quản trị CSDL • PostgreSQL 17 • 8 Schemas • In-Database AI",
                Font = new Font("Segoe UI", 8.75F, FontStyle.Regular),
                ForeColor = Color.FromArgb(224, 242, 254),
                AutoSize = true,
                Location = new Point(20, 37),
                BackColor = Color.Transparent
            };

            var pnlRightHeader = new Panel
            {
                Dock = DockStyle.Right,
                Width = 490,
                BackColor = Color.Transparent
            };

            _lblRolePrompt = new Label
            {
                Text = "Vai trò DB:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(10, 20),
                BackColor = Color.Transparent
            };

            _cboRole = new Guna2ComboBox
            {
                Location = new Point(90, 14),
                Width = 230,
                Height = 36,
                BorderRadius = 8,
                BorderColor = Color.FromArgb(186, 230, 253),
                FillColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboRole.Items.AddRange(new object[] {
                "postgres (Superuser)",
                "nv_thao (Thu ngân - RLS)",
                "nv_manager (Quản lý)",
                "nv_hung (Kỹ thuật)"
            });
            _cboRole.SelectedIndex = 0;
            _cboRole.SelectedIndexChanged += async (s, e) => await SwitchRoleAsync();

            _btnRefresh = new Guna2GradientButton
            {
                Text = "🔄 Làm mới (F5)",
                Location = new Point(330, 14),
                Width = 145,
                Height = 36,
                BorderRadius = 8,
                FillColor = Color.FromArgb(255, 255, 255),
                FillColor2 = Color.FromArgb(240, 249, 255),
                ForeColor = Color.FromArgb(3, 105, 161),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnRefresh.Click += async (s, e) =>
            {
                await LoadComputerGridAsync();
                await LoadCustomersAsync();
                await LoadProductsAsync();
                await LoadInvoicesAsync();
                await LoadBookingsAsync();
            };

            pnlRightHeader.Controls.AddRange(new Control[] { _lblRolePrompt, _cboRole, _btnRefresh });

            _pnlHeader.Controls.Add(_lblAppTitle);
            _pnlHeader.Controls.Add(_lblSubtitle);
            _pnlHeader.Controls.Add(pnlRightHeader);
        }

        // ====================================================================
        // 2. LEFT SIDEBAR MENU
        // ====================================================================
        private void BuildSidebar()
        {
            _pnlSidebar = new Guna2Panel
            {
                Dock = DockStyle.Left,
                Width = 225,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                CustomBorderColor = Color.FromArgb(226, 232, 240),
                CustomBorderThickness = new Padding(0, 0, 1, 0),
                Padding = new Padding(8, 10, 8, 10)
            };

            _lblSidebarMenuTitle = new Label
            {
                Text = "MENU CHỨC NĂNG",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Top = 12,
                Left = 14,
                AutoSize = true,
                BackColor = Color.White
            };

            _btnNavComputers = CreateSidebarButton("🖥️  Sơ đồ máy trạm", 36);
            _btnNavCustomers = CreateSidebarButton("👥  Hội viên & Ví", 88);
            _btnNavBooking = CreateSidebarButton("📅  Đặt máy trước", 140);
            _btnNavOrders = CreateSidebarButton("🍔  Dịch vụ & F&B", 192);
            _btnNavInvoices = CreateSidebarButton("🧾  Hóa đơn & Sổ quỹ", 244);
            _btnNavReports = CreateSidebarButton("📊  Báo cáo doanh thu", 296);

            _btnNavComputers.Checked = true;

            _btnNavComputers.Click += (s, e) => SwitchView(_viewComputers, _btnNavComputers);
            _btnNavCustomers.Click += (s, e) => SwitchView(_viewCustomers, _btnNavCustomers);
            _btnNavBooking.Click += async (s, e) => { SwitchView(_viewBooking, _btnNavBooking); await LoadBookingsAsync(); };
            _btnNavOrders.Click += async (s, e) => { SwitchView(_viewOrders, _btnNavOrders); await LoadProductsAsync(); };
            _btnNavInvoices.Click += async (s, e) => { SwitchView(_viewInvoices, _btnNavInvoices); await LoadInvoicesAsync(); };
            _btnNavReports.Click += (s, e) => SwitchView(_viewReports, _btnNavReports);

            _pnlDbStatus = new Guna2Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                FillColor = Color.FromArgb(248, 250, 252),
                BorderRadius = 8,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                Padding = new Padding(8)
            };

            _lblDbStatus = new Label
            {
                Dock = DockStyle.Fill,
                Text = "🟢 PostgreSQL 17\nCSDL: netcafe_db (UTF-8)",
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 101, 52),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            _pnlDbStatus.Controls.Add(_lblDbStatus);

            _pnlSidebar.Controls.AddRange(new Control[] {
                _lblSidebarMenuTitle,
                _btnNavComputers, _btnNavCustomers, _btnNavBooking,
                _btnNavOrders, _btnNavInvoices, _btnNavReports,
                _pnlDbStatus
            });
        }

        private Guna2Button CreateSidebarButton(string title, int top)
        {
            var btn = new Guna2Button
            {
                Text = title.Replace("&&", "&").Replace("&", "&&"),
                Top = top,
                Left = 8,
                Width = 208,
                Height = 44,
                BorderRadius = 8,
                ButtonMode = Guna.UI2.WinForms.Enums.ButtonMode.RadioButton,
                FillColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Left,
                TextOffset = new Point(10, 0),
                Cursor = Cursors.Hand
            };

            btn.CheckedState.FillColor = Color.FromArgb(239, 246, 255);
            btn.CheckedState.ForeColor = Color.FromArgb(37, 99, 235);
            btn.CheckedState.CustomBorderColor = Color.FromArgb(37, 99, 235);
            btn.CustomBorderThickness = new Padding(4, 0, 0, 0);

            btn.HoverState.FillColor = Color.FromArgb(248, 250, 252);
            btn.HoverState.ForeColor = Color.FromArgb(30, 41, 59);

            return btn;
        }

        private void SwitchView(Panel viewToDisplay, Guna2Button navButton)
        {
            _viewComputers.Visible = false;
            _viewCustomers.Visible = false;
            _viewBooking.Visible = false;
            _viewOrders.Visible = false;
            _viewInvoices.Visible = false;
            _viewReports.Visible = false;

            viewToDisplay.Visible = true;

            _btnNavComputers.Checked = navButton == _btnNavComputers;
            _btnNavCustomers.Checked = navButton == _btnNavCustomers;
            _btnNavBooking.Checked = navButton == _btnNavBooking;
            _btnNavOrders.Checked = navButton == _btnNavOrders;
            _btnNavInvoices.Checked = navButton == _btnNavInvoices;
            _btnNavReports.Checked = navButton == _btnNavReports;
        }

        // ====================================================================
        // 3. BOTTOM SECTION: BANNER BẮT LỖI CSDL (KHI CÓ LỖI MỚI HIỆN)
        // ====================================================================
        private void BuildBottomSection()
        {
            _bottomContainer = new Panel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            _pnlErrorBanner = new Guna2Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                FillColor = Color.FromArgb(254, 242, 242),
                BorderColor = Color.FromArgb(252, 165, 165),
                BorderThickness = 1,
                Visible = false,
                Padding = new Padding(12, 4, 12, 4)
            };

            _lblErrorMessage = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(185, 28, 28),
                Font = new Font("Segoe UI", 9.25F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(254, 242, 242)
            };

            _btnDismissError = new Guna2Button
            {
                Text = "✕ Đóng",
                Dock = DockStyle.Right,
                Width = 85,
                BorderRadius = 6,
                FillColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnDismissError.Click += (s, e) => _pnlErrorBanner.Visible = false;

            _pnlErrorBanner.Controls.Add(_lblErrorMessage);
            _pnlErrorBanner.Controls.Add(_btnDismissError);

            _bottomContainer.Controls.Add(_pnlErrorBanner);
        }

        // ====================================================================
        // 4. MAIN CONTAINER & 6 VIEWS
        // ====================================================================
        private void BuildMainContainerViews()
        {
            _pnlMainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(243, 246, 251),
                Padding = new Padding(12)
            };

            _viewComputers = new Panel { Dock = DockStyle.Fill };
            _viewCustomers = new Panel { Dock = DockStyle.Fill, Visible = false };
            _viewBooking = new Panel { Dock = DockStyle.Fill, Visible = false };
            _viewOrders = new Panel { Dock = DockStyle.Fill, Visible = false };
            _viewInvoices = new Panel { Dock = DockStyle.Fill, Visible = false };
            _viewReports = new Panel { Dock = DockStyle.Fill, Visible = false };

            SetupViewComputers();
            SetupViewCustomers();
            SetupViewBooking();
            SetupViewOrders();
            SetupViewInvoices();
            SetupViewReports();

            _pnlMainContainer.Controls.AddRange(new Control[] {
                _viewComputers, _viewCustomers, _viewBooking,
                _viewOrders, _viewInvoices, _viewReports
            });
        }

        // --------------------------------------------------------------------
        // VIEW 1: SƠ ĐỒ MÁY TRẠM
        // --------------------------------------------------------------------
        private void SetupViewComputers()
        {
            // Panel bên phải: Cố định 340px
            _pnlCompDetail = new Guna2Panel
            {
                Dock = DockStyle.Right,
                Width = 340,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                BorderRadius = 10,
                Padding = new Padding(14),
                AutoScroll = true
            };

            _lblCompDetailTitle = new Label
            {
                Text = "CHI TIẾT MÁY TRẠM",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Top = 12,
                Left = 14,
                Height = 28,
                Width = 300,
                BackColor = Color.White
            };

            var pnlInfoCard = new Guna2Panel
            {
                Top = 44,
                Left = 14,
                Width = 300,
                Height = 150,
                FillColor = Color.FromArgb(248, 250, 252),
                BorderRadius = 8,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                Padding = new Padding(10)
            };

            _lblCompDetailInfo = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Chọn một máy trạm trên sơ đồ để xem cấu hình hoặc mở phiên chơi.",
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 9.25F),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlInfoCard.Controls.Add(_lblCompDetailInfo);

            var grpActions = new Guna2Panel
            {
                Top = 204,
                Left = 14,
                Width = 300,
                Height = 245,
                FillColor = Color.FromArgb(248, 250, 252),
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                BorderRadius = 8,
                Padding = new Padding(10)
            };

            var lblActionTitle = new Label
            {
                Text = "THAO TÁC PHIÊN CHƠI",
                Font = new Font("Segoe UI", 9.25F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Top = 8,
                Left = 10,
                AutoSize = true,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            var lblSelectCust = new Label
            {
                Text = "Khách hàng mở máy:",
                Top = 32,
                Left = 10,
                AutoSize = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 9F)
            };

            _cboCustForSession = new Guna2ComboBox
            {
                Top = 52,
                Left = 10,
                Width = 280,
                Height = 36,
                BorderRadius = 6,
                BorderColor = Color.FromArgb(203, 213, 225),
                FillColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            _btnOpenSession = new Guna2GradientButton
            {
                Text = "▶ MỞ PHIÊN CHƠI",
                Top = 96,
                Left = 10,
                Width = 280,
                Height = 38,
                BorderRadius = 8,
                FillColor = Color.FromArgb(16, 185, 129),
                FillColor2 = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnOpenSession.Click += async (s, e) => await OpenSessionAsync();

            _btnCloseSessionBalance = new Guna2GradientButton
            {
                Text = "⏹ ĐÓNG PHIÊN (TRỪ VÍ)",
                Top = 142,
                Left = 10,
                Width = 280,
                Height = 36,
                BorderRadius = 8,
                FillColor = Color.FromArgb(239, 68, 68),
                FillColor2 = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnCloseSessionBalance.Click += async (s, e) => await CloseSessionAsync("balance");

            _btnCloseSessionCash = new Guna2GradientButton
            {
                Text = "💵 ĐÓNG PHIÊN (TIỀN MẶT/CK)",
                Top = 186,
                Left = 10,
                Width = 280,
                Height = 36,
                BorderRadius = 8,
                FillColor = Color.FromArgb(245, 158, 11),
                FillColor2 = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnCloseSessionCash.Click += async (s, e) => await CloseSessionAsync("cash");

            grpActions.Controls.AddRange(new Control[] {
                lblActionTitle, lblSelectCust, _cboCustForSession,
                _btnOpenSession, _btnCloseSessionBalance, _btnCloseSessionCash
            });

            _pnlCompDetail.Controls.AddRange(new Control[] {
                _lblCompDetailTitle, pnlInfoCard, grpActions
            });

            // Panel bên trái: Legend (trên) + FlowLayoutPanel Grid (dưới)
            var pnlLeftGrid = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 12, 0)
            };

            var pnlLegend = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                FillColor = Color.White,
                BorderRadius = 8,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                Padding = new Padding(12, 8, 12, 8)
            };

            var lblLegend = new Label
            {
                Text = "TRẠNG THÁI:   🟢 Sẵn sàng (Trống)     🔴 Đang có khách     🟡 Đang bảo trì     ⚫ Máy hỏng",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White
            };
            pnlLegend.Controls.Add(lblLegend);

            _pnlComputerGrid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(243, 246, 251),
                Padding = new Padding(0, 8, 0, 0)
            };

            pnlLeftGrid.Controls.Add(_pnlComputerGrid);
            pnlLeftGrid.Controls.Add(pnlLegend);

            _viewComputers.Controls.Add(pnlLeftGrid);
            _viewComputers.Controls.Add(_pnlCompDetail);
        }

        // --------------------------------------------------------------------
        // VIEW 2: KHÁCH HÀNG & NẠP TIỀN
        // --------------------------------------------------------------------
        private void SetupViewCustomers()
        {
            var topPanel = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                FillColor = Color.White,
                BorderRadius = 8,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                Padding = new Padding(12, 8, 12, 8)
            };

            _txtSearchCust = new Guna2TextBox
            {
                PlaceholderText = "🔍 Tìm kiếm theo tên hoặc SĐT...",
                Top = 10,
                Left = 12,
                Width = 260,
                Height = 38,
                BorderRadius = 8,
                BorderColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 9.5F)
            };

            _btnSearchCust = new Guna2Button
            {
                Text = "Tìm kiếm",
                Top = 10,
                Left = 280,
                Width = 100,
                Height = 38,
                BorderRadius = 8,
                FillColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSearchCust.Click += async (s, e) => await LoadCustomersAsync(_txtSearchCust.Text);

            var lblTopUpTitle = new Label
            {
                Text = "Nạp ví:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Top = 18,
                Left = 405,
                AutoSize = true,
                BackColor = Color.White
            };

            _txtTopUpAmount = new Guna2TextBox
            {
                Text = "50000",
                Top = 10,
                Left = 460,
                Width = 120,
                Height = 38,
                BorderRadius = 8,
                BorderColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 9.5F)
            };

            _btnTopUp = new Guna2GradientButton
            {
                Text = "💳 Nạp tiền",
                Top = 10,
                Left = 588,
                Width = 110,
                Height = 38,
                BorderRadius = 8,
                FillColor = Color.FromArgb(16, 185, 129),
                FillColor2 = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnTopUp.Click += async (s, e) => await TopUpCustomerAsync();

            _btnTestInvalidPhone = new Guna2Button
            {
                Text = "⚠️ Thử SĐT sai (Regex)",
                Top = 10,
                Left = 712,
                Width = 175,
                Height = 38,
                BorderRadius = 8,
                FillColor = Color.FromArgb(254, 226, 226),
                BorderColor = Color.FromArgb(248, 113, 113),
                BorderThickness = 1,
                ForeColor = Color.FromArgb(185, 28, 28),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnTestInvalidPhone.Click += async (s, e) => await TestInvalidPhoneAsync();

            topPanel.Controls.AddRange(new Control[] {
                _txtSearchCust, _btnSearchCust, lblTopUpTitle,
                _txtTopUpAmount, _btnTopUp, _btnTestInvalidPhone
            });

            var pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0)
            };

            _dgvCustomers = CreateModernDataGridView();
            pnlGridContainer.Controls.Add(_dgvCustomers);

            _viewCustomers.Controls.Add(pnlGridContainer);
            _viewCustomers.Controls.Add(topPanel);
        }

        // --------------------------------------------------------------------
        // VIEW 3: ĐẶT MÁY TRƯỚC (BỐ CỤC 2 CỘT RÕ RÀNG, ĐẦY ĐỦ NHÃN)
        // --------------------------------------------------------------------
        private void SetupViewBooking()
        {
            // Cột trái: Form đặt máy
            var pnlBookingForm = new Panel
            {
                Dock = DockStyle.Left,
                Width = 410,
                Padding = new Padding(0, 0, 12, 0)
            };

            var cardForm = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = Color.White,
                BorderRadius = 10,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                Padding = new Padding(16),
                AutoScroll = true
            };

            var lblTitle = new Label
            {
                Text = "📅 TẠO LỊCH ĐẶT MÁY MỚI",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Top = 14,
                Left = 14,
                AutoSize = true,
                BackColor = Color.White
            };

            var lblCust = new Label
            {
                Text = "Khách hàng hội viên:",
                Top = 46,
                Left = 14,
                AutoSize = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _cboBookingCust = new Guna2ComboBox
            {
                Top = 68,
                Left = 14,
                Width = 365,
                Height = 36,
                BorderRadius = 6,
                BorderColor = Color.FromArgb(203, 213, 225),
                FillColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblComp = new Label
            {
                Text = "Máy trạm cần đặt:",
                Top = 112,
                Left = 14,
                AutoSize = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _cboBookingComp = new Guna2ComboBox
            {
                Top = 134,
                Left = 14,
                Width = 365,
                Height = 36,
                BorderRadius = 6,
                BorderColor = Color.FromArgb(203, 213, 225),
                FillColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblFrom = new Label
            {
                Text = "Bắt đầu từ thời điểm:",
                Top = 178,
                Left = 14,
                AutoSize = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _dtpFrom = new DateTimePicker
            {
                Top = 200,
                Left = 14,
                Width = 365,
                Height = 32,
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm"
            };
            _dtpFrom.Value = DateTime.Today.AddHours(14);

            var lblTo = new Label
            {
                Text = "Kết thúc vào thời điểm:",
                Top = 242,
                Left = 14,
                AutoSize = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _dtpTo = new DateTimePicker
            {
                Top = 264,
                Left = 14,
                Width = 365,
                Height = 32,
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm"
            };
            _dtpTo.Value = DateTime.Today.AddHours(18);

            _btnCreateBooking = new Guna2GradientButton
            {
                Text = "📅 Đặt lịch máy",
                Top = 312,
                Left = 14,
                Width = 175,
                Height = 40,
                BorderRadius = 8,
                FillColor = Color.FromArgb(16, 185, 129),
                FillColor2 = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnCreateBooking.Click += async (s, e) => await CreateBookingAsync();

            _btnTestOverbooking = new Guna2Button
            {
                Text = "⚠️ Thử trùng giờ (GiST)",
                Top = 312,
                Left = 196,
                Width = 183,
                Height = 40,
                BorderRadius = 8,
                FillColor = Color.FromArgb(254, 226, 226),
                BorderColor = Color.FromArgb(248, 113, 113),
                BorderThickness = 1,
                ForeColor = Color.FromArgb(185, 28, 28),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnTestOverbooking.Click += async (s, e) => await TestOverbookingAsync();

            var lblHint = new Label
            {
                Text = "💡 Giải thích cho Thầy:\nRàng buộc EXCLUDE USING gist (tstzrange &&) ngăn chặn tuyệt đối tình trạng Overbooking ở tầng CSDL.",
                Top = 366,
                Left = 14,
                Width = 365,
                Height = 55,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.25F, FontStyle.Italic),
                BackColor = Color.White
            };

            cardForm.Controls.AddRange(new Control[] {
                lblTitle, lblCust, _cboBookingCust, lblComp, _cboBookingComp,
                lblFrom, _dtpFrom, lblTo, _dtpTo, _btnCreateBooking, _btnTestOverbooking, lblHint
            });
            pnlBookingForm.Controls.Add(cardForm);

            // Cột phải: Bảng danh sách lịch đặt trước
            var pnlBookingList = new Panel
            {
                Dock = DockStyle.Fill
            };

            var pnlListHeader = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                FillColor = Color.White,
                BorderRadius = 8,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                Padding = new Padding(12, 6, 12, 6)
            };

            var lblListTitle = new Label
            {
                Text = "📋 DANH SÁCH LỊCH ĐẶT TRƯỚC (BẢNG core.booking)",
                Font = new Font("Segoe UI", 9.25F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White
            };
            pnlListHeader.Controls.Add(lblListTitle);

            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 0)
            };

            _dgvBookings = CreateModernDataGridView();
            pnlGrid.Controls.Add(_dgvBookings);

            pnlBookingList.Controls.Add(pnlGrid);
            pnlBookingList.Controls.Add(pnlListHeader);

            _viewBooking.Controls.Add(pnlBookingList);
            _viewBooking.Controls.Add(pnlBookingForm);
        }

        // --------------------------------------------------------------------
        // VIEW 4: GỌI MÓN F&B (BỐ CỤC 2 CỘT RÕ RÀNG, ĐẦY ĐỦ NHÃN)
        // --------------------------------------------------------------------
        private void SetupViewOrders()
        {
            // Cột trái: Form gọi món
            var pnlOrderForm = new Panel
            {
                Dock = DockStyle.Left,
                Width = 410,
                Padding = new Padding(0, 0, 12, 0)
            };

            var cardForm = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                FillColor = Color.White,
                BorderRadius = 10,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                Padding = new Padding(16),
                AutoScroll = true
            };

            var lblTitle = new Label
            {
                Text = "🍔 GỌI MÓN VÀO PHIÊN MÁY",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Top = 14,
                Left = 14,
                AutoSize = true,
                BackColor = Color.White
            };

            var lblSess = new Label
            {
                Text = "Phiên máy đang hoạt động:",
                Top = 46,
                Left = 14,
                AutoSize = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _cboOrderSession = new Guna2ComboBox
            {
                Top = 68,
                Left = 14,
                Width = 365,
                Height = 36,
                BorderRadius = 6,
                BorderColor = Color.FromArgb(203, 213, 225),
                FillColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblProd = new Label
            {
                Text = "Chọn món ăn / nước uống:",
                Top = 112,
                Left = 14,
                AutoSize = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _cboOrderProduct = new Guna2ComboBox
            {
                Top = 134,
                Left = 14,
                Width = 365,
                Height = 36,
                BorderRadius = 6,
                BorderColor = Color.FromArgb(203, 213, 225),
                FillColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblQty = new Label
            {
                Text = "Số lượng đặt:",
                Top = 178,
                Left = 14,
                AutoSize = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            _numOrderQty = new NumericUpDown
            {
                Top = 200,
                Left = 14,
                Width = 140,
                Height = 32,
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            _btnPlaceOrder = new Guna2GradientButton
            {
                Text = "🍔 Đặt món (Trừ tồn)",
                Top = 250,
                Left = 14,
                Width = 175,
                Height = 40,
                BorderRadius = 8,
                FillColor = Color.FromArgb(16, 185, 129),
                FillColor2 = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnPlaceOrder.Click += async (s, e) => await PlaceOrderAsync();

            _btnTestOversell = new Guna2Button
            {
                Text = "⚠️ Thử mua vượt kho",
                Top = 250,
                Left = 196,
                Width = 183,
                Height = 40,
                BorderRadius = 8,
                FillColor = Color.FromArgb(254, 226, 226),
                BorderColor = Color.FromArgb(248, 113, 113),
                BorderThickness = 1,
                ForeColor = Color.FromArgb(185, 28, 28),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnTestOversell.Click += async (s, e) => await TestOversellAsync();

            var lblHint = new Label
            {
                Text = "💡 Giải thích cho Thầy:\nTrigger trg_order_detail_deduct_stock tự động trừ tồn kho. Nếu bán lố số lượng, CSDL chặn ngay bằng check_violation.",
                Top = 304,
                Left = 14,
                Width = 365,
                Height = 55,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.25F, FontStyle.Italic),
                BackColor = Color.White
            };

            cardForm.Controls.AddRange(new Control[] {
                lblTitle, lblSess, _cboOrderSession, lblProd, _cboOrderProduct,
                lblQty, _numOrderQty, _btnPlaceOrder, _btnTestOversell, lblHint
            });
            pnlOrderForm.Controls.Add(cardForm);

            // Cột phải: Bảng thực đơn & tồn kho
            var pnlProductList = new Panel
            {
                Dock = DockStyle.Fill
            };

            var pnlListHeader = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                FillColor = Color.White,
                BorderRadius = 8,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                Padding = new Padding(12, 6, 12, 6)
            };

            var lblListTitle = new Label
            {
                Text = "📦 DANH MỤC MÓN & TỒN KHO THỰC TẾ (BẢNG inventory.product)",
                Font = new Font("Segoe UI", 9.25F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White
            };
            pnlListHeader.Controls.Add(lblListTitle);

            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 0)
            };

            _dgvProducts = CreateModernDataGridView();
            pnlGrid.Controls.Add(_dgvProducts);

            pnlProductList.Controls.Add(pnlGrid);
            pnlProductList.Controls.Add(pnlListHeader);

            _viewOrders.Controls.Add(pnlProductList);
            _viewOrders.Controls.Add(pnlOrderForm);
        }

        // --------------------------------------------------------------------
        // VIEW 5: HÓA ĐƠN & SỔ QUỸ (ROW-LEVEL SECURITY)
        // --------------------------------------------------------------------
        private void SetupViewInvoices()
        {
            var topBanner = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                FillColor = Color.White,
                BorderRadius = 8,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                Padding = new Padding(12, 6, 12, 6)
            };

            var lblTitle = new Label
            {
                Text = "🧾 DANH SÁCH HÓA ĐƠN (CƠ CHẾ ROW-LEVEL SECURITY: THU NGÂN CHỈ XEM HÓA ĐƠN DO MÌNH LẬP)",
                Font = new Font("Segoe UI", 9.25F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White
            };
            topBanner.Controls.Add(lblTitle);

            var pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0)
            };

            _dgvInvoices = CreateModernDataGridView();
            pnlGridContainer.Controls.Add(_dgvInvoices);

            _viewInvoices.Controls.Add(pnlGridContainer);
            _viewInvoices.Controls.Add(topBanner);
        }

        // --------------------------------------------------------------------
        // VIEW 6: BÁO CÁO & THỐNG KÊ (HIỂN THỊ RÕ RÀNG 100%)
        // --------------------------------------------------------------------
        private void SetupViewReports()
        {
            var top = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                FillColor = Color.White,
                BorderRadius = 8,
                BorderColor = Color.FromArgb(226, 232, 240),
                BorderThickness = 1,
                Padding = new Padding(16, 8, 16, 8)
            };

            var lblPrompt = new Label
            {
                Text = "CHỌN BÁO CÁO:",
                Font = new Font("Segoe UI", 9.25F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Top = 16,
                Left = 16,
                AutoSize = true,
                BackColor = Color.White
            };

            _radDailyRev = new RadioButton
            {
                Text = "📈 Doanh thu ngày (vw_daily_revenue)",
                Top = 14,
                Left = 160,
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9.25F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                BackColor = Color.White
            };

            _radCompUsage = new RadioButton
            {
                Text = "🖥️ Hiệu suất máy (vw_computer_usage)",
                Top = 14,
                Left = 450,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.25F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.White
            };

            _radTopCust = new RadioButton
            {
                Text = "👑 Top khách hàng (mv_top_customers)",
                Top = 14,
                Left = 750,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.25F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.White
            };

            _radDailyRev.CheckedChanged += async (s, e) => { if (_radDailyRev.Checked) await LoadReportsAsync(1); };
            _radCompUsage.CheckedChanged += async (s, e) => { if (_radCompUsage.Checked) await LoadReportsAsync(2); };
            _radTopCust.CheckedChanged += async (s, e) => { if (_radTopCust.Checked) await LoadReportsAsync(3); };

            top.Controls.AddRange(new Control[] { lblPrompt, _radDailyRev, _radCompUsage, _radTopCust });

            var pnlGridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0)
            };

            _dgvReports = CreateModernDataGridView();
            pnlGridContainer.Controls.Add(_dgvReports);

            _viewReports.Controls.Add(pnlGridContainer);
            _viewReports.Controls.Add(top);
        }

        // ====================================================================
        // HELPER: TẠO DATAGRIDVIEW CĂN CHỈNH ĐẸP MẮT, HEADER LUÔN NỔI BẬT
        // ====================================================================
        private static DataGridView CreateModernDataGridView()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false
            };

            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 38;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(37, 99, 235);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9.25F);
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 58, 138);
            dgv.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.RowTemplate.Height = 34;

            return dgv;
        }

        // ====================================================================
        // DỮ LIỆU & SỰ KIỆN LOGIC
        // ====================================================================

        private async Task InitializeDataAsync()
        {
            await LoadComputerGridAsync();
            await LoadCustomersAsync();
            await LoadProductsAsync();
            await LoadInvoicesAsync();
            await LoadBookingsAsync();
            await LoadReportsAsync(1);
            _ = Task.Run(() => _repo.StartListeningAsync(_cts.Token));
        }

        private void ShowError(Exception ex)
        {
            var err = DbRepository.ParseException(ex);
            _lblErrorMessage.Text = $"[LỖI CSDL - SQLSTATE: {err.SqlState}] {err.Message} | Ràng buộc: {err.ConstraintName}";
            _pnlErrorBanner.Visible = true;
        }

        private void HandleSqlExecuted(SqlLogEntry entry)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[{entry.Timestamp:HH:mm:ss}] ");

                if (entry.IsSuccess)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"({entry.DurationMs,3}ms) [THÀNH CÔNG] ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"({entry.DurationMs,3}ms) [LỖI CSDL]   ");
                }

                Console.ForegroundColor = entry.IsSuccess ? ConsoleColor.White : ConsoleColor.Yellow;
                Console.Write(entry.CommandText);

                if (!string.IsNullOrEmpty(entry.Parameters))
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Write($" | Tham số: {entry.Parameters}");
                }

                if (!entry.IsSuccess && !string.IsNullOrEmpty(entry.ErrorMessage))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($" -> {entry.ErrorMessage}");
                }

                Console.WriteLine();
                Console.ResetColor();
            }
            catch
            {
                // Bỏ qua lỗi stream console
            }
        }

        private void HandleNotification(string channel, string payload)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"\n🔔 [{DateTime.Now:HH:mm:ss}] [CẢNH BÁO BẢO MẬT AI] Kênh: {channel} | {payload}\n");
                Console.ResetColor();
            }
            catch { }

            if (InvokeRequired)
            {
                Invoke(() => HandleNotification(channel, payload));
                return;
            }

            MessageBox.Show($"[CẢNH BÁO BẢO MẬT AI TỪ CSDL]\nKênh: {channel}\nNội dung: {payload}",
                            "Tín hiệu LISTEN/NOTIFY thời gian thực", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private async Task SwitchRoleAsync()
        {
            var idx = _cboRole.SelectedIndex;
            string? user = null;
            string? pwd = null;

            if (idx == 0) { user = "postgres"; pwd = "vietanh1105@"; }
            else if (idx == 1) { user = "nv_thao"; pwd = "Demo@2026NetCafe"; }
            else if (idx == 2) { user = "nv_manager"; pwd = "Demo@2026NetCafe"; }
            else if (idx == 3) { user = "nv_hung"; pwd = "Demo@2026NetCafe"; }

            try
            {
                _repo = new DbRepository(AppConfig.GetConnectionString(user, pwd));
                _repo.OnSqlExecuted += HandleSqlExecuted;
                _pnlErrorBanner.Visible = false;
                await LoadComputerGridAsync();
                await LoadInvoicesAsync();

                _lblDbStatus.Text = $"🟢 Role: {user}\nCSDL: netcafe_db";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task LoadComputerGridAsync(bool silent = false)
        {
            try
            {
                var list = await _repo.GetComputersAsync();
                _pnlComputerGrid.SuspendLayout();
                _pnlComputerGrid.Controls.Clear();

                foreach (var c in list)
                {
                    var card = CreateModernComputerCard(c);
                    _pnlComputerGrid.Controls.Add(card);
                }

                _pnlComputerGrid.ResumeLayout();

                _cboBookingComp.Items.Clear();
                _cboOrderSession.Items.Clear();
                foreach (var c in list)
                {
                    _cboBookingComp.Items.Add(new { Id = c.ComputerId, Text = $"{c.ComputerCode} - {c.ComputerName}" });
                    if (c.CurrentSessionId != null)
                    {
                        _cboOrderSession.Items.Add(new { Id = c.CurrentSessionId.Value, Text = $"Phiên #{c.CurrentSessionId} ({c.ComputerCode} - {c.CurrentCustomerName})" });
                    }
                }
                _cboBookingComp.DisplayMember = "Text";
                _cboOrderSession.DisplayMember = "Text";
                if (_cboBookingComp.Items.Count > 0) _cboBookingComp.SelectedIndex = 0;
                if (_cboOrderSession.Items.Count > 0) _cboOrderSession.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                if (!silent) ShowError(ex);
            }
        }

        private Control CreateModernComputerCard(ComputerGridItem c)
        {
            var card = new Guna2CustomGradientPanel
            {
                Width = 168,
                Height = 126,
                Margin = new Padding(8),
                BorderRadius = 12,
                BorderThickness = 1,
                Cursor = Cursors.Hand
            };

            Color statusBadgeBg;
            Color statusBadgeText;
            Color cardBorder;
            Color cardFill1;
            Color cardFill2;
            string statusText;

            switch (c.ComputerStatus)
            {
                case "available":
                    cardFill1 = Color.FromArgb(236, 253, 245);
                    cardFill2 = Color.White;
                    cardBorder = Color.FromArgb(52, 211, 153);
                    statusBadgeBg = Color.FromArgb(16, 185, 129);
                    statusBadgeText = Color.White;
                    statusText = "TRỐNG";
                    break;
                case "in_use":
                    cardFill1 = Color.FromArgb(254, 242, 242);
                    cardFill2 = Color.White;
                    cardBorder = Color.FromArgb(248, 113, 113);
                    statusBadgeBg = Color.FromArgb(239, 68, 68);
                    statusBadgeText = Color.White;
                    statusText = "ĐANG CHƠI";
                    break;
                case "maintenance":
                    cardFill1 = Color.FromArgb(254, 249, 195);
                    cardFill2 = Color.White;
                    cardBorder = Color.FromArgb(250, 204, 21);
                    statusBadgeBg = Color.FromArgb(245, 158, 11);
                    statusBadgeText = Color.White;
                    statusText = "BẢO TRÌ";
                    break;
                default:
                    cardFill1 = Color.FromArgb(241, 245, 249);
                    cardFill2 = Color.White;
                    cardBorder = Color.FromArgb(203, 213, 225);
                    statusBadgeBg = Color.FromArgb(100, 116, 139);
                    statusBadgeText = Color.White;
                    statusText = "HỎNG";
                    break;
            }

            card.FillColor = cardFill1;
            card.FillColor2 = cardFill2;
            card.BorderColor = cardBorder;

            var lblCode = new Label
            {
                Text = c.ComputerCode,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Top = 8,
                Left = 10,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var badge = new Label
            {
                Text = statusText,
                Top = 8,
                Left = 85,
                Width = 72,
                Height = 22,
                BackColor = statusBadgeBg,
                ForeColor = statusBadgeText,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblName = new Label
            {
                Text = $"{c.ComputerName}\n({c.TypeName})",
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Top = 35,
                Left = 10,
                Width = 148,
                Height = 34,
                BackColor = Color.Transparent
            };

            var lblUser = new Label
            {
                Text = c.CurrentCustomerName != null ? $"👤 {c.CurrentCustomerName}" : $"💰 {c.HourlyRate:N0} đ/h",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = c.CurrentCustomerName != null ? Color.FromArgb(185, 28, 28) : Color.FromArgb(5, 150, 105),
                Top = 75,
                Left = 10,
                Width = 148,
                BackColor = Color.Transparent
            };

            var lblTime = new Label
            {
                Text = c.PlayedMinutes > 0 ? $"⏱ Đã chơi: {c.PlayedMinutes:N0}p" : $"🌐 IP: {c.IpAddress}",
                Font = new Font("Segoe UI", 7.75F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                Top = 98,
                Left = 10,
                Width = 148,
                BackColor = Color.Transparent
            };

            card.Controls.AddRange(new Control[] { lblCode, badge, lblName, lblUser, lblTime });

            card.Click += (s, e) => SelectComputer(c);
            foreach (Control child in card.Controls)
            {
                child.Click += (s, e) => SelectComputer(c);
            }

            return card;
        }

        private void SelectComputer(ComputerGridItem c)
        {
            _selectedComp = c;
            _lblCompDetailTitle.Text = $"MÁY: {c.ComputerCode} - {c.ComputerName}";
            _lblCompDetailInfo.Text = $"• Khu vực: {c.RoomName}\n" +
                                     $"• Loại máy: {c.TypeName} ({c.HourlyRate:N0} đ/giờ)\n" +
                                     $"• Trạng thái: {c.ComputerStatus.ToUpper()}\n" +
                                     $"• IP LAN: {c.IpAddress}\n" +
                                     (c.CurrentSessionId != null ?
                                        $"• Khách đang chơi: {c.CurrentCustomerName} (@{c.CurrentUsername})\n" +
                                        $"• Bắt đầu: {c.SessionStartTime:HH:mm} (Đã chơi: {c.PlayedMinutes:N0} phút)" :
                                        "• Máy đang trống, sẵn sàng mở phiên mới.");

            _btnOpenSession.Enabled = c.ComputerStatus == "available";
            _btnCloseSessionBalance.Enabled = c.ComputerStatus == "in_use";
            _btnCloseSessionCash.Enabled = c.ComputerStatus == "in_use";
        }

        private async Task OpenSessionAsync()
        {
            if (_selectedComp == null || _selectedComp.ComputerStatus != "available")
            {
                MessageBox.Show("Vui lòng chọn một máy trống!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cboCustForSession.SelectedItem is not CustomerItem cust)
            {
                MessageBox.Show("Vui lòng chọn khách hàng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _pnlErrorBanner.Visible = false;
                var sessId = await _repo.OpenSessionAsync(cust.CustomerId, _selectedComp.ComputerId, 1);
                MessageBox.Show($"Mở phiên chơi #{sessId} thành công cho khách {cust.FullName} tại máy {_selectedComp.ComputerCode}!",
                                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadComputerGridAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task CloseSessionAsync(string method)
        {
            if (_selectedComp == null || _selectedComp.CurrentSessionId == null)
            {
                MessageBox.Show("Máy này không có phiên chơi đang mở!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _pnlErrorBanner.Visible = false;
                var invId = await _repo.CloseSessionAndPayAsync(_selectedComp.CurrentSessionId.Value, 1, method);
                MessageBox.Show($"Đã kết thúc phiên và xuất Hóa đơn #{invId} (Phương thức: {method})!",
                                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadComputerGridAsync();
                await LoadInvoicesAsync();
                await LoadCustomersAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task LoadCustomersAsync(string? search = null)
        {
            try
            {
                var list = await _repo.GetCustomersAsync(search);
                _dgvCustomers.DataSource = list;

                _cboCustForSession.Items.Clear();
                _cboBookingCust.Items.Clear();
                foreach (var c in list)
                {
                    _cboCustForSession.Items.Add(c);
                    _cboBookingCust.Items.Add(new { Id = c.CustomerId, Text = $"{c.FullName} ({c.Phone})" });
                }
                _cboCustForSession.DisplayMember = "FullName";
                _cboBookingCust.DisplayMember = "Text";
                if (_cboCustForSession.Items.Count > 0) _cboCustForSession.SelectedIndex = 0;
                if (_cboBookingCust.Items.Count > 0) _cboBookingCust.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task TopUpCustomerAsync()
        {
            if (_dgvCustomers.CurrentRow?.DataBoundItem is not CustomerItem cust)
            {
                MessageBox.Show("Vui lòng chọn khách hàng trong bảng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(_txtTopUpAmount.Text, out var amount) || amount <= 0)
            {
                MessageBox.Show("Số tiền nạp không hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _pnlErrorBanner.Visible = false;
                var newBal = await _repo.TopUpAsync(cust.CustomerId, 1, amount);
                MessageBox.Show($"Nạp thành công {amount:N0} đ cho khách {cust.FullName}! Số dư mới: {newBal:N0} đ.",
                                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadCustomersAsync(_txtSearchCust.Text);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task TestInvalidPhoneAsync()
        {
            try
            {
                _pnlErrorBanner.Visible = false;
                await using var conn = new Npgsql.NpgsqlConnection(AppConfig.GetConnectionString());
                await conn.OpenAsync();
                await using var cmd = new Npgsql.NpgsqlCommand(
                    "INSERT INTO core.customer (full_name, phone, username, password_hash) VALUES ('Lỗi Regex Phone', '12345', 'err_user', 'hash');", conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task LoadBookingsAsync()
        {
            try
            {
                string sql = @"
                    SELECT b.booking_id AS ""Mã đặt"",
                           c.full_name AS ""Khách hàng"",
                           cp.computer_code AS ""Máy trạm"",
                           to_char(b.booked_from, 'YYYY-MM-DD HH24:MI') AS ""Bắt đầu"",
                           to_char(b.booked_to, 'YYYY-MM-DD HH24:MI') AS ""Kết thúc"",
                           b.status AS ""Trạng thái""
                    FROM core.booking b
                    JOIN core.customer c ON b.customer_id = c.customer_id
                    JOIN core.computer cp ON b.computer_id = cp.computer_id
                    ORDER BY b.booking_id DESC;";

                await using var conn = new Npgsql.NpgsqlConnection(AppConfig.GetConnectionString());
                await conn.OpenAsync();
                await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
                using var da = new Npgsql.NpgsqlDataAdapter(cmd);
                var dt = new System.Data.DataTable();
                da.Fill(dt);
                _dgvBookings.DataSource = dt;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task CreateBookingAsync()
        {
            try
            {
                _pnlErrorBanner.Visible = false;
                dynamic comp = _cboBookingComp.SelectedItem!;
                dynamic cust = _cboBookingCust.SelectedItem!;
                int compId = comp.Id;
                int custId = cust.Id;

                var id = await _repo.CreateBookingAsync(custId, compId, _dtpFrom.Value, _dtpTo.Value);
                MessageBox.Show($"Đặt lịch máy thành công (Mã đặt #{id})!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadBookingsAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task TestOverbookingAsync()
        {
            try
            {
                _pnlErrorBanner.Visible = false;
                dynamic comp = _cboBookingComp.SelectedItem!;
                dynamic cust = _cboBookingCust.SelectedItem!;
                int compId = comp.Id;
                int custId = cust.Id;

                await _repo.CreateBookingAsync(custId, compId, DateTime.Today.AddHours(20), DateTime.Today.AddHours(23));
                await _repo.CreateBookingAsync(custId, compId, DateTime.Today.AddHours(21), DateTime.Today.AddHours(24));
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var list = await _repo.GetProductsAsync();
                _dgvProducts.DataSource = list;

                _cboOrderProduct.Items.Clear();
                foreach (var p in list)
                {
                    _cboOrderProduct.Items.Add(new { Id = p.ProductId, Text = $"{p.ProductName} ({p.UnitPrice:N0} đ - Tồn: {p.StockQty})" });
                }
                _cboOrderProduct.DisplayMember = "Text";
                if (_cboOrderProduct.Items.Count > 0) _cboOrderProduct.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task PlaceOrderAsync()
        {
            if (_cboOrderSession.SelectedItem == null)
            {
                MessageBox.Show("Không có phiên máy nào đang hoạt động để gọi món!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _pnlErrorBanner.Visible = false;
                dynamic sess = _cboOrderSession.SelectedItem;
                dynamic prod = _cboOrderProduct.SelectedItem!;
                long sessId = sess.Id;
                int prodId = prod.Id;
                int qty = (int)_numOrderQty.Value;

                var orderId = await _repo.PlaceOrderAsync(sessId, 1, prodId, qty);
                MessageBox.Show($"Gọi món thành công (Đơn #{orderId})! Tồn kho đã tự động trừ.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task TestOversellAsync()
        {
            if (_cboOrderSession.SelectedItem == null)
            {
                MessageBox.Show("Cần có ít nhất 1 phiên máy đang mở để thử gọi món!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _pnlErrorBanner.Visible = false;
                dynamic sess = _cboOrderSession.SelectedItem;
                dynamic prod = _cboOrderProduct.SelectedItem!;
                long sessId = sess.Id;
                int prodId = prod.Id;

                await _repo.PlaceOrderAsync(sessId, 1, prodId, 99999);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task LoadInvoicesAsync()
        {
            try
            {
                var list = await _repo.GetRecentInvoicesAsync();
                _dgvInvoices.DataSource = list;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task LoadReportsAsync(int type)
        {
            try
            {
                string sql = type switch
                {
                    1 => "SELECT revenue_date AS \"Ngày\", total_invoices AS \"Số HĐ\", total_play_amount AS \"Tiền giờ\", total_service_amount AS \"Tiền F&B\", total_discount_amount AS \"Giảm giá\", net_revenue AS \"Thực thu\" FROM reporting.vw_daily_revenue;",
                    2 => "SELECT computer_code AS \"Mã máy\", computer_name AS \"Tên máy\", room_name AS \"Khu vực\", total_sessions AS \"Tổng phiên\", total_hours_played AS \"Số giờ chơi\", total_play_revenue AS \"Doanh thu máy\" FROM reporting.vw_computer_usage;",
                    3 => "SELECT customer_id AS \"Mã khách\", full_name AS \"Họ tên\", phone AS \"SĐT\", membership_tier AS \"Hạng\", loyalty_points AS \"Điểm\", total_sessions AS \"Số phiên\", total_spent AS \"Tổng chi tiêu\" FROM reporting.mv_top_customers;",
                    _ => "SELECT 1"
                };

                await using var conn = new Npgsql.NpgsqlConnection(AppConfig.GetConnectionString());
                await conn.OpenAsync();
                await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
                using var da = new Npgsql.NpgsqlDataAdapter(cmd);
                var dt = new System.Data.DataTable();
                da.Fill(dt);
                _dgvReports.DataSource = dt;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _cts.Cancel();
                _refreshTimer.Stop();
            }
            catch { }
            base.OnFormClosing(e);
            Environment.Exit(0);
        }
    }
}
