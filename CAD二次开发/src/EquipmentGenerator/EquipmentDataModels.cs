namespace EquipmentGenerator;

// ============================================================
// 堆垛机参数
// ============================================================
public class StackerDataModel
{
    /// <summary>堆垛机总高 (mm)</summary>
    public double STK_H { get; set; } = 8000;

    /// <summary>额定载重 (kg)</summary>
    public double STK_Load { get; set; } = 500;

    /// <summary>货叉类型：single=单伸, double=双伸</summary>
    public string STK_Fork { get; set; } = "single";

    /// <summary>巷道净宽 (mm)</summary>
    public double STK_Aisle_W { get; set; } = 1500;

    /// <summary>水平速度 (m/min)</summary>
    public double STK_SpeedV { get; set; } = 160;

    /// <summary>升降速度 (m/min)</summary>
    public double STK_SpeedH { get; set; } = 40;
}

// ============================================================
// 控制柜参数
// ============================================================
public class CabinetDataModel
{
    public double Width { get; set; } = 800;
    public double Depth { get; set; } = 600;
    public double Height { get; set; } = 2000;
}

// ============================================================
// 安全围栏参数
// ============================================================
public class FenceDataModel
{
    public double Height { get; set; } = 2000;
    public double PostSpacing { get; set; } = 2500;
}

// ============================================================
// 扫码器参数
// ============================================================
public class ScannerDataModel
{
    /// <summary>安装位置：side=侧装, top=顶装</summary>
    public string MountPosition { get; set; } = "side";
}
