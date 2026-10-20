// Trách nhiệm: tạo yêu cầu hỗ trợ qua core.sp_create_service_request.
// Liên kết: SupportRequestView.Designer.cs cung cấp form và MainForm.cs gửi request.

namespace NetCafe.Client.Views;

public partial class SupportRequestView : UserControl
{
    public event Func<string, string, Task>? RequestSubmitted;
    public event Action? BackRequested;

    public SupportRequestView()
    {
        InitializeComponent();
        _submitButton.Click += async (_, _) =>
        {
            _submitButton.Enabled = false;
            try
            {
                if (RequestSubmitted is not null)
                {
                    await RequestSubmitted(_typeBox.Text, _messageBox.Text.Trim());
                }
            }
            finally
            {
                _submitButton.Enabled = true;
            }
        };
        _backButton.Click += (_, _) => BackRequested?.Invoke();
    }

    public void ShowMessage(string message)
    {
        _messageLabel.Text = message;
    }
}
