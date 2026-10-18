// Trách nhiệm: layout Designer của card máy; dữ liệu được binding từ ComputerGridItem.
// Liên kết: ComputerCardControl.cs và ComputersView.cs.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Controls;

public partial class ComputerCardControl
{
    private Label _codeLabel = null!;
    private Label _nameLabel = null!;
    private Label _roomLabel = null!;
    private Label _typeLabel = null!;
    private Label _statusLabel = null!;
    private Label _customerLabel = null!;
    private Label _costLabel = null!;
    private Button _detailsButton = null!;

    private void InitializeComponent()
    {
        _codeLabel = new Label();
        _nameLabel = new Label();
        _roomLabel = new Label();
        _typeLabel = new Label();
        _statusLabel = new Label();
        _customerLabel = new Label();
        _costLabel = new Label();
        _detailsButton = new Button();
        SuspendLayout();
        Width = 260;
        Height = 190;
        Margin = new Padding(8);
        Padding = new Padding(12);
        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;

        _codeLabel.AutoSize = true;
        _codeLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _codeLabel.ForeColor = Color.FromArgb(30, 41, 59);
        _codeLabel.Location = new Point(12, 10);

        _nameLabel.AutoSize = true;
        _nameLabel.ForeColor = Color.FromArgb(71, 85, 105);
        _nameLabel.Location = new Point(12, 36);

        _roomLabel.AutoSize = true;
        _roomLabel.ForeColor = Color.FromArgb(100, 116, 139);
        _roomLabel.Location = new Point(12, 58);

        _typeLabel.AutoSize = true;
        _typeLabel.ForeColor = Color.FromArgb(100, 116, 139);
        _typeLabel.Location = new Point(12, 79);

        _statusLabel.AutoSize = true;
        _statusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _statusLabel.Location = new Point(12, 103);

        _customerLabel.AutoSize = true;
        _customerLabel.ForeColor = Color.FromArgb(71, 85, 105);
        _customerLabel.Location = new Point(12, 126);

        _costLabel.AutoSize = true;
        _costLabel.ForeColor = Color.FromArgb(71, 85, 105);
        _costLabel.Location = new Point(12, 148);

        _detailsButton.Text = "Chi tiết";
        _detailsButton.Size = new Size(70, 27);
        _detailsButton.Location = new Point(178, 8);
        _detailsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _detailsButton.FlatStyle = FlatStyle.Flat;
        _detailsButton.FlatAppearance.BorderSize = 0;
        _detailsButton.BackColor = Color.FromArgb(219, 234, 254);
        _detailsButton.ForeColor = Color.FromArgb(30, 64, 175);

        Controls.AddRange(new Control[] { _codeLabel, _nameLabel, _roomLabel, _typeLabel, _statusLabel, _customerLabel, _costLabel, _detailsButton });
        ResumeLayout(false);
        PerformLayout();
    }
}
