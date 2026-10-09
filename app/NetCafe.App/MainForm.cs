using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetCafe.Data;

namespace NetCafe.App
{
    public class MainForm : Form
    {
        private DbRepository _repo;
        private CancellationTokenSource _cts = new();

        // Header controls
        private Label _lblTitle = null!;
        private Label _lblRole = null!;
        private ComboBox _cboRole = null!;
        private Button _btnRefresh = null!;

        // TabControl
        private TabControl _tabControl = null!;
        private TabPage _tabComputers = null!;
        private TabPage _tabCustomers = null!;
        private TabPage _tabBooking = null!;
        private TabPage _tabOrders = null!;
        private TabPage _tabInvoices = null!;
        private TabPage _tabReports = null!;

        // Tab Computers
        private FlowLayoutPanel _pnlComputerGrid = null!;
        private Panel _pnlCompDetail = null!;
        private Label _lblCompDetailTitle = null!;
        private Label _lblCompDetailInfo = null!;
        private ComboBox _cboCustForSession = null!;
        private Button _btnOpenSession = null!;
        private Button _btnCloseSessionBalance = null!;
        private Button _btnCloseSessionCash = null!;
        private ComputerGridItem? _selectedComp;

        // Tab Customers
        private TextBox _txtSearchCust = null!;
        private Button _btnSearchCust = null!;
        private DataGridView _dgvCustomers = null!;
        private TextBox _txtTopUpAmount = null!;
        private Button _btnTopUp = null!;
        private Button _btnTestInvalidPhone = null!;

        // Tab Booking
        private ComboBox _cboBookingComp = null!;
        private ComboBox _cboBookingCust = null!;
        private DateTimePicker _dtpFrom = null!;
        private DateTimePicker _dtpTo = null!;
        private Button _btnCreateBooking = null!;
        private Button _btnTestOverbooking = null!;

        // Tab Orders
        private ComboBox _cboOrderSession = null!;
        private ComboBox _cboOrderProduct = null!;
        private NumericUpDown _numOrderQty = null!;
        private Button _btnPlaceOrder = null!;
        private Button _btnTestOversell = null!;

        // Tab Invoices
        private DataGridView _dgvInvoices = null!;

        // Tab Reports
        private RadioButton _radDailyRev = null!;
        private RadioButton _radCompUsage = null!;
        private RadioButton _radTopCust = null!;
        private DataGridView _dgvReports = null!;

        // Bottom Controls
        private Panel _pnlError = null!;
        private Label _lblError = null!;
        private Button _btnDismissError = null!;
        private ListBox _lstSqlLog = null!;
        private System.Windows.Forms.Timer _refreshTimer = null!;

        public MainForm()
        {
            AppConfig.LoadEnv();
            _repo = new DbRepository(AppConfig.GetConnectionString());
            _repo.OnSqlExecuted += HandleSqlExecuted;
            _repo.OnNotificationReceived += HandleNotification;

            InitializeComponents();
            _ = InitializeDataAsync();
        }

        private void InitializeComponents()
        {
            Text = "NetCafe Management System - PostgreSQL 17 Thin Client";
            Size = new Size(1280, 800);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            Icon = SystemIcons.Application;

            // 1. Header Panel
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(24, 43, 73),
                ForeColor = Color.White,
                Padding = new Padding(12)
            };

            _lblTitle = new Label
            {
                Text = "QUẢN LÝ PHÒNG GAME/NET - POSTGRESQL 17",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 16)
            };

            _lblRole = new Label
            {
                Text = "Vai trò DB:",
                ForeColor = Color.LightGray,
                AutoSize = true,
                Location = new Point(620, 18)
            };

            _cboRole = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Location = new Point(710, 15)
            };
            _cboRole.Items.AddRange(new object[] {
                "postgres (Superuser)",
                "nv_thao (Thu ngân)",
                "nv_manager (Quản lý)",
                "nv_hung (Kỹ thuật)"
            });
            _cboRole.SelectedIndex = 0;
            _cboRole.SelectedIndexChanged += async (s, e) => await SwitchRoleAsync();

            _btnRefresh = new Button
            {
                Text = "🔄 Làm mới (F5)",
                Width = 140,
                Height = 32,
                Location = new Point(930, 14),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnRefresh.Click += async (s, e) => await LoadComputerGridAsync();

            header.Controls.AddRange(new Control[] { _lblTitle, _lblRole, _cboRole, _btnRefresh });

            // 2. Bottom Panel (Error Banner + SQL Log Strip)
            var bottomContainer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 170
            };

            _pnlError = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(254, 226, 226),
                Visible = false,
                Padding = new Padding(8)
            };

            _lblError = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(153, 27, 27),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnDismissError = new Button
            {
                Text = "✕ Đóng",
                Dock = DockStyle.Right,
                Width = 80,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnDismissError.Click += (s, e) => _pnlError.Visible = false;
            _pnlError.Controls.Add(_lblError);
            _pnlError.Controls.Add(_btnDismissError);

            var sqlLogPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            var lblSqlTitle = new Label
            {
                Text = "NHẬT KÝ LỆNH CSDL (SQL LOG - THỜI GIAN THỰC):",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Padding = new Padding(6, 4, 0, 0)
            };

            _lstSqlLog = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.None
            };

            sqlLogPanel.Controls.Add(_lstSqlLog);
            sqlLogPanel.Controls.Add(lblSqlTitle);

            bottomContainer.Controls.Add(sqlLogPanel);
            bottomContainer.Controls.Add(_pnlError);

            // 3. Main TabControl
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(12, 6)
            };

            _tabComputers = new TabPage("🖥️ Sơ đồ máy");
            _tabCustomers = new TabPage("👥 Khách hàng & Nạp tiền");
            _tabBooking = new TabPage("📅 Đặt máy trước");
            _tabOrders = new TabPage("🍔 Gọi món F&B");
            _tabInvoices = new TabPage("🧾 Hóa đơn & Sổ quỹ");
            _tabReports = new TabPage("📊 Báo cáo & Thống kê");

            SetupTabComputers();
            SetupTabCustomers();
            SetupTabBooking();
            SetupTabOrders();
            SetupTabInvoices();
            SetupTabReports();

            _tabControl.TabPages.AddRange(new[] {
                _tabComputers, _tabCustomers, _tabBooking, _tabOrders, _tabInvoices, _tabReports
            });

            Controls.Add(_tabControl);
            Controls.Add(bottomContainer);
            Controls.Add(header);

            // Auto-refresh timer every 3 seconds
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            _refreshTimer.Tick += async (s, e) =>
            {
                if (_tabControl.SelectedTab == _tabComputers)
                {
                    await LoadComputerGridAsync(silent: true);
                }
            };
            _refreshTimer.Start();
        }

        private void SetupTabComputers()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 860,
                FixedPanel = FixedPanel.Panel2
            };

            _pnlComputerGrid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(12)
            };

            _pnlCompDetail = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16)
            };

            _lblCompDetailTitle = new Label
            {
                Text = "CHI TIẾT MÁY TRẠM",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 35
            };

            _lblCompDetailInfo = new Label
            {
                Text = "Chọn một máy trạm trên sơ đồ để xem thông tin hoặc mở phiên.",
                Dock = DockStyle.Top,
                Height = 160,
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            var grpActions = new GroupBox
            {
                Text = "Thao tác phiên chơi",
                Dock = DockStyle.Top,
                Height = 250,
                Padding = new Padding(10)
            };

            var lblSelectCust = new Label { Text = "Khách hàng mở máy:", Top = 25, Left = 12, AutoSize = true };
            _cboCustForSession = new ComboBox
            {
                Top = 50,
                Left = 12,
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            _btnOpenSession = new Button
            {
                Text = "▶ MỞ PHIÊN CHƠI",
                Top = 90,
                Left = 12,
                Width = 320,
                Height = 40,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            _btnOpenSession.Click += async (s, e) => await OpenSessionAsync();

            _btnCloseSessionBalance = new Button
            {
                Text = "⏹ ĐÓNG PHIÊN (TRỪ SỐ DƯ)",
                Top = 140,
                Left = 12,
                Width = 320,
                Height = 38,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            _btnCloseSessionBalance.Click += async (s, e) => await CloseSessionAsync("balance");

            _btnCloseSessionCash = new Button
            {
                Text = "💵 ĐÓNG PHIÊN (TIỀN MẶT)",
                Top = 185,
                Left = 12,
                Width = 320,
                Height = 36,
                BackColor = Color.FromArgb(245, 158, 11),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            _btnCloseSessionCash.Click += async (s, e) => await CloseSessionAsync("cash");

            grpActions.Controls.AddRange(new Control[] {
                lblSelectCust, _cboCustForSession, _btnOpenSession, _btnCloseSessionBalance, _btnCloseSessionCash
            });

            _pnlCompDetail.Controls.Add(grpActions);
            _pnlCompDetail.Controls.Add(_lblCompDetailInfo);
            _pnlCompDetail.Controls.Add(_lblCompDetailTitle);

            split.Panel1.Controls.Add(_pnlComputerGrid);
            split.Panel2.Controls.Add(_pnlCompDetail);
            _tabComputers.Controls.Add(split);
        }

        private void SetupTabCustomers()
        {
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10) };
            var lblSearch = new Label { Text = "Tìm khách hàng (họ tên/SĐT/user):", Top = 14, Left = 10, AutoSize = true };
            _txtSearchCust = new TextBox { Top = 10, Left = 260, Width = 220 };
            _btnSearchCust = new Button { Text = "🔍 Tìm", Top = 9, Left = 490, Width = 80, Height = 28 };
            _btnSearchCust.Click += async (s, e) => await LoadCustomersAsync(_txtSearchCust.Text);

            var lblTopUp = new Label { Text = "Số tiền nạp:", Top = 14, Left = 600, AutoSize = true };
            _txtTopUpAmount = new TextBox { Top = 10, Left = 690, Width = 110, Text = "50000" };
            _btnTopUp = new Button
            {
                Text = "💰 Nạp tiền",
                Top = 9,
                Left = 810,
                Width = 100,
                Height = 28,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnTopUp.Click += async (s, e) => await TopUpCustomerAsync();

            _btnTestInvalidPhone = new Button
            {
                Text = "⚠️ Thử SĐT sai (Test CHECK Regex)",
                Top = 9,
                Left = 930,
                Width = 260,
                Height = 28,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnTestInvalidPhone.Click += async (s, e) => await TestInvalidPhoneAsync();

            topPanel.Controls.AddRange(new Control[] {
                lblSearch, _txtSearchCust, _btnSearchCust, lblTopUp, _txtTopUpAmount, _btnTopUp, _btnTestInvalidPhone
            });

            _dgvCustomers = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                BackgroundColor = Color.White
            };

            _tabCustomers.Controls.Add(_dgvCustomers);
            _tabCustomers.Controls.Add(topPanel);
        }

        private void SetupTabBooking()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24) };
            var grp = new GroupBox { Text = "Thông tin đặt máy", Dock = DockStyle.Top, Height = 320, Padding = new Padding(20) };

            var lblCust = new Label { Text = "Khách hàng:", Top = 35, Left = 20, AutoSize = true };
            _cboBookingCust = new ComboBox { Top = 30, Left = 160, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblComp = new Label { Text = "Máy tính:", Top = 80, Left = 20, AutoSize = true };
            _cboBookingComp = new ComboBox { Top = 75, Left = 160, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblFrom = new Label { Text = "Bắt đầu:", Top = 125, Left = 20, AutoSize = true };
            _dtpFrom = new DateTimePicker { Top = 120, Left = 160, Width = 300, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm" };
            _dtpFrom.Value = DateTime.Today.AddHours(14);

            var lblTo = new Label { Text = "Kết thúc:", Top = 170, Left = 20, AutoSize = true };
            _dtpTo = new DateTimePicker { Top = 165, Left = 160, Width = 300, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm" };
            _dtpTo.Value = DateTime.Today.AddHours(18);

            _btnCreateBooking = new Button
            {
                Text = "📅 Xác nhận đặt máy",
                Top = 220,
                Left = 160,
                Width = 200,
                Height = 38,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            _btnCreateBooking.Click += async (s, e) => await CreateBookingAsync();

            _btnTestOverbooking = new Button
            {
                Text = "⚠️ Thử đặt trùng giờ (Test EXCLUDE GiST)",
                Top = 220,
                Left = 380,
                Width = 320,
                Height = 38,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            _btnTestOverbooking.Click += async (s, e) => await TestOverbookingAsync();

            grp.Controls.AddRange(new Control[] {
                lblCust, _cboBookingCust, lblComp, _cboBookingComp, lblFrom, _dtpFrom, lblTo, _dtpTo,
                _btnCreateBooking, _btnTestOverbooking
            });
            pnl.Controls.Add(grp);
            _tabBooking.Controls.Add(pnl);
        }

        private void SetupTabOrders()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24) };
            var grp = new GroupBox { Text = "Gọi món cho phiên chơi đang hoạt động", Dock = DockStyle.Top, Height = 280, Padding = new Padding(20) };

            var lblSess = new Label { Text = "Phiên máy:", Top = 35, Left = 20, AutoSize = true };
            _cboOrderSession = new ComboBox { Top = 30, Left = 160, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblProd = new Label { Text = "Sản phẩm F&B:", Top = 80, Left = 20, AutoSize = true };
            _cboOrderProduct = new ComboBox { Top = 75, Left = 160, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblQty = new Label { Text = "Số lượng:", Top = 125, Left = 20, AutoSize = true };
            _numOrderQty = new NumericUpDown { Top = 120, Left = 160, Width = 100, Minimum = 1, Maximum = 1000, Value = 1 };

            _btnPlaceOrder = new Button
            {
                Text = "🍔 Gọi món (Trừ tồn kho)",
                Top = 180,
                Left = 160,
                Width = 220,
                Height = 38,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            _btnPlaceOrder.Click += async (s, e) => await PlaceOrderAsync();

            _btnTestOversell = new Button
            {
                Text = "⚠️ Thử mua vượt kho (Test CHECK stock_qty)",
                Top = 180,
                Left = 400,
                Width = 340,
                Height = 38,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            _btnTestOversell.Click += async (s, e) => await TestOversellAsync();

            grp.Controls.AddRange(new Control[] {
                lblSess, _cboOrderSession, lblProd, _cboOrderProduct, lblQty, _numOrderQty,
                _btnPlaceOrder, _btnTestOversell
            });
            pnl.Controls.Add(grp);
            _tabOrders.Controls.Add(pnl);
        }

        private void SetupTabInvoices()
        {
            _dgvInvoices = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White
            };
            _tabInvoices.Controls.Add(_dgvInvoices);
        }

        private void SetupTabReports()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(10) };
            _radDailyRev = new RadioButton { Text = "Doanh thu theo ngày (vw_daily_revenue)", Top = 10, Left = 15, AutoSize = true, Checked = true };
            _radCompUsage = new RadioButton { Text = "Hiệu suất máy (vw_computer_usage)", Top = 10, Left = 330, AutoSize = true };
            _radTopCust = new RadioButton { Text = "Top khách hàng (mv_top_customers)", Top = 10, Left = 620, AutoSize = true };

            _radDailyRev.CheckedChanged += async (s, e) => { if (_radDailyRev.Checked) await LoadReportsAsync(1); };
            _radCompUsage.CheckedChanged += async (s, e) => { if (_radCompUsage.Checked) await LoadReportsAsync(2); };
            _radTopCust.CheckedChanged += async (s, e) => { if (_radTopCust.Checked) await LoadReportsAsync(3); };

            top.Controls.AddRange(new Control[] { _radDailyRev, _radCompUsage, _radTopCust });

            _dgvReports = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                BackgroundColor = Color.White
            };

            _tabReports.Controls.Add(_dgvReports);
            _tabReports.Controls.Add(top);
        }

        // ==========================================
        // DỮ LIỆU & SỰ KIỆN
        // ==========================================

        private async Task InitializeDataAsync()
        {
            await LoadComputerGridAsync();
            await LoadCustomersAsync();
            await LoadProductsAsync();
            await LoadInvoicesAsync();
            await LoadReportsAsync(1);
            _ = Task.Run(() => _repo.StartListeningAsync(_cts.Token));
        }

        private void ShowError(Exception ex)
        {
            var err = DbRepository.ParseException(ex);
            _lblError.Text = $"[MÃ LỖI: {err.SqlState}] {err.Message} | Ràng buộc: {err.ConstraintName}";
            _pnlError.Visible = true;
        }

        private void HandleSqlExecuted(SqlLogEntry entry)
        {
            if (InvokeRequired)
            {
                Invoke(() => HandleSqlExecuted(entry));
                return;
            }

            _lstSqlLog.Items.Insert(0, entry.ToString());
            if (_lstSqlLog.Items.Count > 200) _lstSqlLog.Items.RemoveAt(200);
        }

        private void HandleNotification(string channel, string payload)
        {
            if (InvokeRequired)
            {
                Invoke(() => HandleNotification(channel, payload));
                return;
            }

            MessageBox.Show($"[CẢNH BÁO BẢO MẬT AI] Kênh: {channel}\nNội dung: {payload}",
                            "Tín hiệu LISTEN/NOTIFY từ CSDL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                _pnlError.Visible = false;
                await LoadComputerGridAsync();
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
                    var card = CreateComputerCard(c);
                    _pnlComputerGrid.Controls.Add(card);
                }

                _pnlComputerGrid.ResumeLayout();

                // Update combo for booking & session
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

        private Control CreateComputerCard(ComputerGridItem c)
        {
            var pnl = new Panel
            {
                Width = 150,
                Height = 120,
                Margin = new Padding(8),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };

            Color statusColor;
            string statusText;
            switch (c.ComputerStatus)
            {
                case "available":
                    statusColor = Color.FromArgb(209, 250, 229);
                    statusText = "TRỐNG";
                    break;
                case "in_use":
                    statusColor = Color.FromArgb(254, 226, 226);
                    statusText = "ĐANG CHƠI";
                    break;
                case "maintenance":
                    statusColor = Color.FromArgb(254, 243, 199);
                    statusText = "BẢO TRÌ";
                    break;
                default:
                    statusColor = Color.FromArgb(241, 245, 249);
                    statusText = "HỎNG";
                    break;
            }

            pnl.BackColor = statusColor;

            var lblCode = new Label
            {
                Text = c.ComputerCode,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Top = 8,
                Left = 8,
                AutoSize = true
            };

            var lblName = new Label
            {
                Text = $"{c.ComputerName} ({c.TypeName})",
                Font = new Font("Segoe UI", 8F),
                Top = 32,
                Left = 8,
                Width = 135
            };

            var lblStatus = new Label
            {
                Text = statusText,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = c.ComputerStatus == "in_use" ? Color.Red : (c.ComputerStatus == "available" ? Color.DarkGreen : Color.DarkOrange),
                Top = 55,
                Left = 8,
                AutoSize = true
            };

            var lblUser = new Label
            {
                Text = c.CurrentCustomerName ?? $"{c.HourlyRate:N0} đ/h",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                Top = 80,
                Left = 8,
                Width = 135
            };

            pnl.Controls.AddRange(new Control[] { lblCode, lblName, lblStatus, lblUser });
            pnl.Click += (s, e) => SelectComputer(c);
            foreach (Control child in pnl.Controls)
            {
                child.Click += (s, e) => SelectComputer(c);
            }

            return pnl;
        }

        private void SelectComputer(ComputerGridItem c)
        {
            _selectedComp = c;
            _lblCompDetailTitle.Text = $"MÁY: {c.ComputerCode} - {c.ComputerName}";
            _lblCompDetailInfo.Text = $"Khu vực: {c.RoomName}\n" +
                                     $"Loại máy: {c.TypeName} ({c.HourlyRate:N0} đ/giờ)\n" +
                                     $"Trạng thái: {c.ComputerStatus.ToUpper()}\n" +
                                     $"IP LAN: {c.IpAddress}\n" +
                                     (c.CurrentSessionId != null ?
                                        $"Khách đang chơi: {c.CurrentCustomerName} (@{c.CurrentUsername})\n" +
                                        $"Bắt đầu: {c.SessionStartTime:HH:mm} (Đã chơi: {c.PlayedMinutes:N0} phút)" :
                                        "Máy đang trống, sẵn sàng mở phiên mới.");

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
                _pnlError.Visible = false;
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
                _pnlError.Visible = false;
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
                _pnlError.Visible = false;
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
                _pnlError.Visible = false;
                // Cố tình gọi truy vấn INSERT SĐT sai định dạng '12345'
                await using var conn = new Npgsql.NpgsqlConnection(AppConfig.GetConnectionString());
                await conn.OpenAsync();
                await using var cmd = new Npgsql.NpgsqlCommand(
                    "INSERT INTO core.customer (full_name, phone, username, password_hash) VALUES ('Lỗi Regex', '12345', 'err_user', 'hash');", conn);
                await cmd.ExecuteNonQueryAsync();
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
                _pnlError.Visible = false;
                dynamic comp = _cboBookingComp.SelectedItem!;
                dynamic cust = _cboBookingCust.SelectedItem!;
                int compId = comp.Id;
                int custId = cust.Id;

                var id = await _repo.CreateBookingAsync(custId, compId, _dtpFrom.Value, _dtpTo.Value);
                MessageBox.Show($"Đặt lịch máy thành công (Mã đặt #{id})!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                _pnlError.Visible = false;
                dynamic comp = _cboBookingComp.SelectedItem!;
                dynamic cust = _cboBookingCust.SelectedItem!;
                int compId = comp.Id;
                int custId = cust.Id;

                // Cố tình đặt 2 lần cùng một khung giờ để kích hoạt lỗi EXCLUDE USING gist
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
                _pnlError.Visible = false;
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
                _pnlError.Visible = false;
                dynamic sess = _cboOrderSession.SelectedItem;
                dynamic prod = _cboOrderProduct.SelectedItem!;
                long sessId = sess.Id;
                int prodId = prod.Id;

                // Cố tình mua 99999 sản phẩm vượt quá tồn kho
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
                    1 => "SELECT revenue_date, total_invoices, total_play_amount, total_service_amount, total_discount_amount, net_revenue FROM reporting.vw_daily_revenue;",
                    2 => "SELECT computer_code, computer_name, room_name, total_sessions, total_hours_played, total_play_revenue FROM reporting.vw_computer_usage;",
                    3 => "SELECT customer_id, full_name, phone, membership_tier, loyalty_points, total_sessions, total_spent FROM reporting.mv_top_customers;",
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
            _cts.Cancel();
            _refreshTimer.Stop();
            base.OnFormClosing(e);
        }
    }
}
