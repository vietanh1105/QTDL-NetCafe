// Designer layout cho ComputersView; dữ liệu card do ComputersView.cs binding.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public partial class ComputersView
{
    private FlowLayoutPanel _cardsPanel = null!;

    private void InitializeComponent()
    {
        _cardsPanel = new FlowLayoutPanel();
        _cardsPanel.Dock = DockStyle.Fill;
        _cardsPanel.AutoScroll = true;
        _cardsPanel.WrapContents = true;
        _cardsPanel.FlowDirection = FlowDirection.LeftToRight;
        _cardsPanel.BackColor = Color.FromArgb(243, 246, 251);
        _cardsPanel.Padding = new Padding(8);
        Controls.Add(_cardsPanel);
        Grid.Visible = false;
        Name = "ComputersView";
        TabIndex = 0;
    }
}
