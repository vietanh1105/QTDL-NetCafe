// Designer layout cho form yêu cầu hỗ trợ của Station.
// Liên kết: SupportRequestView.cs phát RequestSubmitted cho MainForm.cs.

using System.Windows.Forms;

namespace NetCafe.Client.Views;

public partial class SupportRequestView
{
    private ComboBox _typeBox = null!;
    private TextBox _messageBox = null!;
    private Button _submitButton = null!;
    private Button _backButton = null!;
    private Label _messageLabel = null!;

    private void InitializeComponent()
    {
        _typeBox = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = StationTheme.SurfaceRaised,
            ForeColor = StationTheme.Text,
            Font = new System.Drawing.Font("Segoe UI", 10F)
        };
        _typeBox.Items.AddRange(new object[] { "Kỹ thuật", "Đồ ăn / Nước", "Nạp tiền", "Hỗ trợ chung" });
        _typeBox.SelectedIndex = 0;
        _messageBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            BackColor = StationTheme.SurfaceRaised,
            ForeColor = StationTheme.Text,
            Font = new System.Drawing.Font("Segoe UI", 10F),
            PlaceholderText = "Mô tả ngắn gọn vấn đề cần hỗ trợ..."
        };
        _submitButton = new Button { Text = "Gửi yêu cầu", Dock = DockStyle.Fill, Height = 42 };
        _backButton = new Button { Text = "Quay lại phiên", Dock = DockStyle.Fill, Height = 42 };
        StationTheme.StyleButton(_submitButton, StationTheme.Primary);
        StationTheme.StyleButton(_backButton, StationTheme.SurfaceRaised);
        _messageLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            ForeColor = StationTheme.Cyan,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = StationTheme.Background,
            Padding = new Padding(14)
        };
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = StationTheme.Surface,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 8,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        card.Controls.Add(new Label { Text = "YÊU CẦU HỖ TRỢ", Dock = DockStyle.Fill, ForeColor = StationTheme.Text, Font = new System.Drawing.Font("Segoe UI", 13F, FontStyle.Bold), TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
        card.Controls.Add(new Label { Text = "Gửi yêu cầu trực tiếp đến quầy Admin", Dock = DockStyle.Fill, ForeColor = StationTheme.MutedText, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 1);
        card.Controls.Add(new Label { Text = "Loại yêu cầu", Dock = DockStyle.Fill, ForeColor = StationTheme.Cyan, TextAlign = System.Drawing.ContentAlignment.BottomLeft }, 0, 2);
        card.Controls.Add(_typeBox, 0, 3);
        card.Controls.Add(new Label { Text = "Nội dung", Dock = DockStyle.Fill, ForeColor = StationTheme.Cyan, TextAlign = System.Drawing.ContentAlignment.BottomLeft }, 0, 4);
        card.Controls.Add(_messageBox, 0, 5);
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 8, 0, 4) };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        actions.Controls.Add(_submitButton, 0, 0);
        actions.Controls.Add(_backButton, 1, 0);
        card.Controls.Add(actions, 0, 6);
        card.Controls.Add(_messageLabel, 0, 7);
        panel.Controls.Add(card);
        Controls.Add(panel);
        Dock = DockStyle.Fill;
    }
}
