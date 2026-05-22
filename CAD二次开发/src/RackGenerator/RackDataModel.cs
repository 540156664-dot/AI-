namespace RackGenerator;

/// <summary>
/// 货架参数数据模型（一期：横梁式货架）
/// </summary>
public class RackDataModel
{
    // ========== 几何参数 ==========
    /// <summary>货架总长 (mm)</summary>
    public double L { get; set; } = 2700;

    /// <summary>货架深度 (mm)</summary>
    public double D { get; set; } = 1000;

    /// <summary>货架总高 (mm)</summary>
    public double H { get; set; } = 6000;

    /// <summary>层数（含地面层）</summary>
    public int NLayer { get; set; } = 4;

    /// <summary>层高 (mm)</summary>
    public double HLayer { get; set; } = 1500;

    // ========== 承载参数 ==========
    /// <summary>每层承重 (kg)</summary>
    public double WLayer { get; set; } = 1000;

    // ========== 布局参数 ==========
    /// <summary>排数</summary>
    public int NRow { get; set; } = 4;

    /// <summary>列数（每排货位数）</summary>
    public int NCol { get; set; } = 10;

    /// <summary>巷道宽度 (mm)</summary>
    public double WAisle { get; set; } = 3500;

    /// <summary>托盘宽度 (mm)</summary>
    public double WPallet { get; set; } = 1200;

    /// <summary>托盘深度 (mm)</summary>
    public double DPallet { get; set; } = 1000;

    /// <summary>列间距 (mm)</summary>
    public double GapX { get; set; } = 75;

    /// <summary>背靠背排间距 (mm)</summary>
    public double GapY { get; set; } = 200;

    // ========== 结构参数 ==========
    /// <summary>立柱宽度 (mm)</summary>
    public double WColumn { get; set; } = 90;

    /// <summary>横梁高度 (mm)</summary>
    public double DBeam { get; set; } = 120;

    // ========== 视图选项 ==========
    public bool DrawTopView { get; set; } = true;
    public bool DrawFrontView { get; set; } = false;
    public bool DrawSideView { get; set; } = false;

    // ========== 计算属性 ==========
    /// <summary>单个货位宽度</summary>
    public double CellWidth => (L - GapX * (NCol - 1)) / NCol;

    /// <summary>两排背靠背货架的总深度（含间隙）</summary>
    public double BackToBackDepth => D * 2 + GapY;

    /// <summary>一排+巷道+下一排的周期间距</summary>
    public double RowPitch => D + WAisle;
}
