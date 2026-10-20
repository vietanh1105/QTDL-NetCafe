// Trách nhiệm: vùng cuộn responsive cho sơ đồ máy và tự dồn card theo chiều rộng.
// Liên kết: ComputersView.cs giữ dictionary card theo ComputerId; ComputerCardControl.cs vẽ từng card.

using System.Drawing;
using System.Windows.Forms;

namespace NetCafe.App.Views;

public partial class ComputersView
{
    private Panel _cardsViewport = null!;
    private FlowLayoutPanel _cardsGrid = null!;

    private void InitializeComponent()
    {
        _cardsViewport = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(243, 246, 251),
            Padding = new Padding(0)
        };
        _cardsGrid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(16, 16, 24, 24),
            BackColor = Color.FromArgb(243, 246, 251)
        };
        _cardsGrid.Resize += (_, _) => RebuildGridPositions();
        _cardsViewport.Controls.Add(_cardsGrid);
        Controls.Add(_cardsViewport);
        // Dock sau thanh công cụ để hàng đầu không bị che viền.
        _cardsViewport.BringToFront();
        Grid.Visible = false;
        Name = "ComputersView";
        TabIndex = 0;
    }

}
