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
            BackColor = CyberTheme.Background,
            Padding = new Padding(0)
        };
        _cardsGrid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(16, 12, 16, 36),
            BackColor = CyberTheme.Background
        };
        _cardsGrid.Resize += (_, _) => RebuildGridPositions();
        _cardsViewport.Controls.Add(_cardsGrid);
        Controls.Add(_cardsViewport);
        // Dock sau thanh công cụ để hàng đầu không bị che viền.
        _cardsViewport.BringToFront();
        Grid.Visible = false;
        InfoLabel.Visible = false;
        InfoLabel.Height = 0;
        Name = "ComputersView";
        TabIndex = 0;
    }
}
