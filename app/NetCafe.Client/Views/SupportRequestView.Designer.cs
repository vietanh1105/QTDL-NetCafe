// Designer layout cho queue hỗ trợ station.

using System.Windows.Forms;

namespace NetCafe.Client.Views;

public partial class SupportRequestView
{
    private TextBox _typeBox = null!;
    private TextBox _messageBox = null!;
    private Button _submitButton = null!;
    private Button _backButton = null!;
    private Label _messageLabel = null!;

    private void InitializeComponent()
    {
        _typeBox = new TextBox { Width = 220, Text = "general" };
        _messageBox = new TextBox { Width = 320, Height = 90, Multiline = true };
        _submitButton = new Button { Text = "Gửi yêu cầu", Width = 120, Height = 36 };
        _backButton = new Button { Text = "Quay lại", Width = 120, Height = 36 };
        _messageLabel = new Label { AutoSize = true };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(32) };
        panel.Controls.Add(new Label { Text = "Loại yêu cầu", AutoSize = true });
        panel.Controls.Add(_typeBox);
        panel.Controls.Add(new Label { Text = "Nội dung", AutoSize = true });
        panel.Controls.Add(_messageBox);
        panel.Controls.Add(_submitButton);
        panel.Controls.Add(_backButton);
        panel.Controls.Add(_messageLabel);
        Controls.Add(panel);
        Dock = DockStyle.Fill;
    }
}
