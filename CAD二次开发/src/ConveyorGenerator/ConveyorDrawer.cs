using Common.CADAbstraction;

namespace ConveyorGenerator;

/// <summary>
/// 输送机绘图器 — 沿路径生成滚筒输送机图元
/// </summary>
public class ConveyorDrawer
{
    private readonly ICadContext _cad;
    private readonly ConveyorDataModel _model;

    public ConveyorDrawer(ICadContext cad, ConveyorDataModel model)
    {
        _cad = cad;
        _model = model;
    }

    /// <summary>
    /// 根据已解析的路径段列表绘制整条输送线
    /// </summary>
    public List<object> Draw(List<PathSegment> segments)
    {
        var entities = new List<object>();

        foreach (var seg in segments)
        {
            switch (seg.Type)
            {
                case PathSegmentType.Straight:
                    entities.AddRange(DrawStraight(seg));
                    break;
                case PathSegmentType.Turn:
                    entities.AddRange(DrawTurn(seg));
                    break;
            }
        }

        return entities;
    }

    /// <summary>
    /// 绘制直线段：双线轮廓 + 中心虚线 + 方向箭头
    /// </summary>
    private List<object> DrawStraight(PathSegment seg)
    {
        var entities = new List<object>();
        double hw = _model.CW / 2.0;
        var dir = seg.End - seg.Start;
        double length = dir.DistanceTo(new Point3d(0, 0, 0));

        if (length < 1) return entities;

        // 单位方向向量
        var ux = (seg.End.X - seg.Start.X) / length;
        var uy = (seg.End.Y - seg.Start.Y) / length;
        // 法向量（向左）
        var nx = -uy;
        var ny = ux;

        // 左轮廓线
        var leftLine = _cad.AddLine(
            seg.Start.X + nx * hw, seg.Start.Y + ny * hw, 0,
            seg.End.X + nx * hw, seg.End.Y + ny * hw, 0);
        _cad.SetLayer(leftLine, "LG-输送机-轮廓");
        entities.Add(leftLine);

        // 右轮廓线
        var rightLine = _cad.AddLine(
            seg.Start.X - nx * hw, seg.Start.Y - ny * hw, 0,
            seg.End.X - nx * hw, seg.End.Y - ny * hw, 0);
        _cad.SetLayer(rightLine, "LG-输送机-轮廓");
        entities.Add(rightLine);

        // 中心线（虚线点段）
        var centerLine = _cad.AddLine(
            seg.Start.X, seg.Start.Y, 0,
            seg.End.X, seg.End.Y, 0);
        _cad.SetLayer(centerLine, "LG-输送机-中心线");
        entities.Add(centerLine);

        // 方向箭头（沿中心线等距放置）
        int arrowCount = (int)(length / _model.ArrowSpacing);
        for (int i = 0; i < arrowCount; i++)
        {
            double t = (i + 0.5) / arrowCount;
            double ax = seg.Start.X + (seg.End.X - seg.Start.X) * t;
            double ay = seg.Start.Y + (seg.End.Y - seg.Start.Y) * t;
            entities.Add(DrawArrow(ax, ay, ux, uy, 200));
        }

        return entities;
    }

    /// <summary>
    /// 绘制转弯段：内弧+外弧
    /// </summary>
    private List<object> DrawTurn(PathSegment seg)
    {
        var entities = new List<object>();

        // 简化：在转弯点绘制一个圆弧标记
        double r = _model.RInner + _model.CW / 2.0;
        var arc = _cad.AddArc(seg.Start, r, 0, seg.TurnAngleDeg);
        _cad.SetLayer(arc, "LG-输送机-轮廓");
        entities.Add(arc);

        return entities;
    }

    /// <summary>
    /// 绘制方向箭头（三角形简化版）
    /// </summary>
    private object DrawArrow(double x, double y, double ux, double uy, double size)
    {
        var p1 = new Point3d(x + ux * size * 0.5, y + uy * size * 0.5, 0);
        var p2 = new Point3d(x - ux * size * 0.5 + uy * size * 0.3, y - uy * size * 0.5 + ux * size * 0.3, 0);
        var p3 = new Point3d(x - ux * size * 0.5 - uy * size * 0.3, y - uy * size * 0.5 - ux * size * 0.3, 0);
        return _cad.AddPolyline(new[] { p1, p2, p3, p1 });
    }
}
