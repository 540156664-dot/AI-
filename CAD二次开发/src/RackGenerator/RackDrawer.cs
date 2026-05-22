using Common.CADAbstraction;

namespace RackGenerator;

/// <summary>
/// 货架绘图器 — 生成横梁式货架的俯视图/正视图/侧视图
/// </summary>
public class RackDrawer
{
    private readonly ICadContext _cad;

    public RackDrawer(ICadContext cad)
    {
        _cad = cad;
    }

    /// <summary>
    /// 绘制俯视图（平面布局）
    /// </summary>
    /// <param name="p">货架参数</param>
    /// <param name="origin">左下角插入点</param>
    /// <returns>生成的图元ID列表</returns>
    public List<object> DrawTopView(RackDataModel p, Point3d origin)
    {
        var entities = new List<object>();

        // 货架外框矩形
        var outerRect = _cad.AddRectangle(
            origin,
            new Point3d(origin.X + p.L, origin.Y + p.D, 0));
        _cad.SetLayer(outerRect, "LG-货架-外框");
        entities.Add(outerRect);

        // 四角立柱标记（小方块）
        double colH = p.WColumn;
        entities.Add(DrawColumnMark(origin.X, origin.Y, colH));
        entities.Add(DrawColumnMark(origin.X + p.L - colH, origin.Y, colH));
        entities.Add(DrawColumnMark(origin.X, origin.Y + p.D - colH, colH));
        entities.Add(DrawColumnMark(origin.X + p.L - colH, origin.Y + p.D - colH, colH));

        // 货位分隔线（虚线）
        for (int i = 1; i < p.NCol; i++)
        {
            double x = origin.X + p.CellWidth * i + p.GapX * (i - 0.5);
            var sepLine = _cad.AddLine(x, origin.Y, 0, x, origin.Y + p.D, 0);
            _cad.SetLayer(sepLine, "LG-货架-货位");
            entities.Add(sepLine);
        }

        return entities;
    }

    /// <summary>
    /// 绘制正视图（立面图）
    /// </summary>
    public List<object> DrawFrontView(RackDataModel p, Point3d origin)
    {
        var entities = new List<object>();

        // 外框
        var outerRect = _cad.AddRectangle(
            origin,
            new Point3d(origin.X + p.L, origin.Y + p.H, 0));
        _cad.SetLayer(outerRect, "LG-货架-外框");
        entities.Add(outerRect);

        // 左右立柱
        entities.Add(_cad.AddLine(origin.X, origin.Y, 0, origin.X, origin.Y + p.H, 0));
        entities.Add(_cad.AddLine(origin.X + p.L, origin.Y, 0, origin.X + p.L, origin.Y + p.H, 0));

        // 横梁（每层水平线）
        for (int i = 1; i < p.NLayer; i++)
        {
            double y = origin.Y + p.HLayer * i;
            var beam = _cad.AddLine(origin.X, y, 0, origin.X + p.L, y, 0);
            _cad.SetLayer(beam, "LG-货架-外框");
            entities.Add(beam);
        }

        return entities;
    }

    /// <summary>
    /// 绘制侧视图
    /// </summary>
    public List<object> DrawSideView(RackDataModel p, Point3d origin)
    {
        var entities = new List<object>();

        var outerRect = _cad.AddRectangle(
            origin,
            new Point3d(origin.X + p.D, origin.Y + p.H, 0));
        _cad.SetLayer(outerRect, "LG-货架-外框");
        entities.Add(outerRect);

        // 层板标记
        for (int i = 1; i < p.NLayer; i++)
        {
            double y = origin.Y + p.HLayer * i;
            var layerLine = _cad.AddLine(origin.X, y, 0, origin.X + p.D, y, 0);
            _cad.SetLayer(layerLine, "LG-货架-货位");
            entities.Add(layerLine);
        }

        return entities;
    }

    /// <summary>
    /// 绘制单个立柱标记（小方块）
    /// </summary>
    private object DrawColumnMark(double x, double y, double size)
    {
        var rect = _cad.AddRectangle(
            new Point3d(x, y, 0),
            new Point3d(x + size, y + size, 0));
        _cad.SetLayer(rect, "LG-货架-外框");
        return rect;
    }
}
