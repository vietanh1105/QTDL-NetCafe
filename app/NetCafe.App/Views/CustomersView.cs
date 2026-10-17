// Trách nhiệm: danh sách hội viên và điểm nối cho CRUD/top-up Admin.
// Liên kết: core.customer, billing.sp_top_up và DbRepository.GetCustomersAsync.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public partial class CustomersView : AdminDataViewBase
{
    private readonly TextBox _searchBox = new();
    private readonly TextBox _topUpBox = new();
    private readonly Button _topUpButton = new();

    public CustomersView()
    {
        InitializeComponent();
        SetTitle("Hội viên và ví", "Tìm kiếm không dấu; top-up dùng function database và ghi payment.");
        _searchBox.Width = 190;
        _searchBox.PlaceholderText = "Tên / SĐT";
        _topUpBox.Width = 100;
        _topUpBox.Text = "50000";
        _topUpButton.Text = "Nạp ví";
        _topUpButton.Width = 80;
        _topUpButton.BackColor = Color.FromArgb(16, 185, 129);
        _topUpButton.ForeColor = Color.White;
        _topUpButton.FlatStyle = FlatStyle.Flat;
        _topUpButton.FlatAppearance.BorderSize = 0;
        Toolbar.Controls.Add(_topUpButton);
        Toolbar.Controls.Add(_topUpBox);
        Toolbar.Controls.Add(_searchBox);
        _searchBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) await RefreshAsync();
        };
        _topUpButton.Click += async (_, _) => await TopUpAsync();
    }

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var rows = await RequiredContext.Repository.GetCustomersAsync(_searchBox.Text);
        SetRows(rows, $"{rows.Count} hội viên");
    }

    private async Task TopUpAsync()
    {
        if (RequiredContext.CurrentEmployeeId <= 0)
        {
            ReportError(new InvalidOperationException("Role hiện tại chưa ánh xạ employee_id; không thể ghi nhận nạp ví."));
            return;
        }

        if (Grid.CurrentRow?.DataBoundItem is not NetCafe.Data.CustomerItem customer ||
            !decimal.TryParse(_topUpBox.Text, out var amount) || amount <= 0)
        {
            return;
        }
        try
        {
            await RequiredContext.Repository.TopUpAsync(customer.CustomerId, RequiredContext.CurrentEmployeeId, amount);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }
}
