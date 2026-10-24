// Trách nhiệm: Hộp thoại thao tác nhanh hội viên từ sơ đồ máy trạm (Nạp tiền, Mở máy hoặc điều hướng).

using System.Drawing;
using System.Windows.Forms;
using NetCafe.App.Views;
using NetCafe.Data;

namespace NetCafe.App.Controls;

public enum MemberActionChoice
{
    None,
    TopUp,
    OpenSession,
    NavigateToCustomers
}

internal sealed class MemberQuickActionDialog : Form
{
    private readonly ComboBox _customerCombo = new();
    public CustomerItem? SelectedCustomer { get; private set; }
    public MemberActionChoice ActionChoice { get; private set; } = MemberActionChoice.None;

    public MemberQuickActionDialog(IReadOnlyList<CustomerItem> customers, string computerCode)
    {
        Text = $"Thao tác hội viên - Máy {computerCode}";
        StartPosition = FormStartPosition.CenterParent;
        Width = 460;
        Height = 260;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = CyberTheme.Background;
        ForeColor = CyberTheme.Text;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var prompt = new Label
        {
            Text = "Chọn tài khoản hội viên để thao tác:",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = CyberTheme.Text
        };

        _customerCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _customerCombo.Dock = DockStyle.Top;
        _customerCombo.Font = new Font("Segoe UI", 10F);
        foreach (var c in customers)
        {
            _customerCombo.Items.Add(new CustomerChoiceOption(c));
        }
        if (_customerCombo.Items.Count > 0)
        {
            _customerCombo.SelectedIndex = 0;
        }

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var cancelButton = new Button
        {
            Text = "Đóng",
            DialogResult = DialogResult.Cancel,
            Width = 75,
            Height = 36
        };
        CyberTheme.StyleButton(cancelButton, CyberTheme.SurfaceRaised);

        var manageButton = new Button
        {
            Text = "📋 Quản lý KH",
            Width = 115,
            Height = 36
        };
        CyberTheme.StyleButton(manageButton, CyberTheme.SurfaceRaised);
        manageButton.Click += (_, _) =>
        {
            ActionChoice = MemberActionChoice.NavigateToCustomers;
            DialogResult = DialogResult.OK;
        };

        var openButton = new Button
        {
            Text = "▶ Mở máy",
            Width = 100,
            Height = 36
        };
        CyberTheme.StyleButton(openButton, CyberTheme.Primary);
        openButton.Click += (_, _) =>
        {
            if (_customerCombo.SelectedItem is CustomerChoiceOption opt)
            {
                SelectedCustomer = opt.Customer;
                ActionChoice = MemberActionChoice.OpenSession;
                DialogResult = DialogResult.OK;
            }
        };

        var topUpButton = new Button
        {
            Text = "💳 Nạp ví",
            Width = 95,
            Height = 36
        };
        CyberTheme.StyleButton(topUpButton, CyberTheme.Success);
        topUpButton.Click += (_, _) =>
        {
            if (_customerCombo.SelectedItem is CustomerChoiceOption opt)
            {
                SelectedCustomer = opt.Customer;
                ActionChoice = MemberActionChoice.TopUp;
                DialogResult = DialogResult.OK;
            }
        };

        btnPanel.Controls.Add(cancelButton);
        btnPanel.Controls.Add(manageButton);
        btnPanel.Controls.Add(openButton);
        btnPanel.Controls.Add(topUpButton);

        panel.Controls.Add(prompt, 0, 0);
        panel.Controls.Add(_customerCombo, 0, 1);
        panel.Controls.Add(btnPanel, 0, 2);
        Controls.Add(panel);
        CancelButton = cancelButton;
    }

    private sealed class CustomerChoiceOption
    {
        public CustomerItem Customer { get; }
        public CustomerChoiceOption(CustomerItem customer) => Customer = customer;
        public override string ToString() => $"{Customer.FullName} (@{Customer.Username}) - Số dư: {Customer.Balance:N0} đ";
    }
}
