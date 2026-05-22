using System.Windows.Controls;

namespace RackUI;

public partial class MainPalette : UserControl
{
    public MainPalette()
    {
        InitializeComponent();
    }

    public void SwitchToTab(int index)
    {
        if (index >= 0 && index < MainTabControl.Items.Count)
            MainTabControl.SelectedIndex = index;
    }

    public StackPanel GetPanel(string name) => name switch
    {
        "Rack" => RackPanel,
        "Conveyor" => ConveyorPanel,
        "Stacker" => StackerPanel,
        "Cabinet" => CabinetPanel,
        "Fence" => FencePanel,
        "Scanner" => ScannerPanel,
        "BOM" => BOMPanel,
        "Settings" => SettingsPanel,
        _ => RackPanel
    };
}
