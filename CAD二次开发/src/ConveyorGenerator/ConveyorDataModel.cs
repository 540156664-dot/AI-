using Common.CADAbstraction;

namespace ConveyorGenerator;

/// <summary>
/// 输送机参数数据模型（一期：滚筒输送机）
/// </summary>
public class ConveyorDataModel
{
    /// <summary>输送线有效宽度 (mm)</summary>
    public double CW { get; set; } = 1000;

    /// <summary>输送面离地高度 (mm)</summary>
    public double CH { get; set; } = 750;

    /// <summary>输送速度 (m/min)</summary>
    public double V { get; set; } = 20;

    /// <summary>最大载重 (kg)</summary>
    public double WMax { get; set; } = 1000;

    /// <summary>转弯内侧半径 (mm)</summary>
    public double RInner { get; set; } = 600;

    /// <summary>方向箭头间距 (mm)</summary>
    public double ArrowSpacing { get; set; } = 3000;

    /// <summary>路径节点列表</summary>
    public List<Point3d> PathNodes { get; set; } = new();
}

/// <summary>
/// 输送机路径段
/// </summary>
public class PathSegment
{
    public PathSegmentType Type { get; set; }
    public Point3d Start { get; set; }
    public Point3d End { get; set; }
    public double Length { get; set; }
    public double TurnAngleDeg { get; set; }  // 转弯段的角度
    public Point3d? TurnCenter { get; set; }   // 转弯段的圆心
}

public enum PathSegmentType
{
    Straight,
    Turn
}
