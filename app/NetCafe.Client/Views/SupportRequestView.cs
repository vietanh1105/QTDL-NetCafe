// Trách nhiệm: tạo service request qua core.sp_create_service_request.

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
            if (RequestSubmitted is not null)
            {
                await RequestSubmitted(_typeBox.Text, _messageBox.Text);
            }
        };
        _backButton.Click += (_, _) => BackRequested?.Invoke();
    }

    public void ShowMessage(string message)
    {
        _messageLabel.Text = message;
    }
}
