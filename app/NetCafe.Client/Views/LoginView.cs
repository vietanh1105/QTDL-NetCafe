// Trách nhiệm: nhập customer username/password và gửi computer_code cố định của station.
// Liên kết: MainForm.cs gọi DbRepository.CustomerLoginAsync.

namespace NetCafe.Client.Views;

public partial class LoginView : UserControl
{
    public event Func<string, string, Task>? LoginRequested;

    public LoginView()
    {
        InitializeComponent();
        _loginButton.Click += async (_, _) =>
        {
            if (LoginRequested is not null)
            {
                await LoginRequested(_usernameBox.Text.Trim(), _passwordBox.Text);
            }
        };
    }

    public void ShowError(string message)
    {
        _errorLabel.Text = message;
    }

    public void ClearPassword()
    {
        _passwordBox.Clear();
    }
}
