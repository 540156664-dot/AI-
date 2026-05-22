using Common.CADAbstraction;

namespace EquipmentGenerator;

/// <summary>
/// 综合设备绘图器（堆垛机/控制柜/围栏/扫码器）
/// </summary>
public class EquipmentDrawer
{
    private readonly ICadContext _cad;

    public EquipmentDrawer(ICadContext cad)
    {
        _cad = cad;
    }

    // ============================================================
    // 堆垛机 — 巷道内矩形+轨道+载货台
    // ============================================================
    public List<object> DrawStacker(StackerDataModel p, Point3d origin)
    {
        var entities = new List<object>();

        // 堆垛机主体矩形
        double bodyW = 300; // 载货台宽度
        double bodyD = 500; // 前后深度
        var body = _cad.AddRectangle(
            new Point3d(origin.X - bodyW / 2, origin.Y - bodyD / 2, 0),
            new Point3d(origin.X + bodyW / 2, origin.Y + bodyD / 2, 0));
        _cad.SetLayer(body, "LG-堆垛机");
        entities.Add(body);

        // 轨道双线
        double railHalf = p.STK_Aisle_W / 2;
        var railLeft = _cad.AddLine(
            origin.X - railHalf, origin.Y - bodyD / 2 - 100, 0,
            origin.X - railHalf, origin.Y + bodyD / 2 + 100, 0);
        _cad.SetLayer(railLeft, "LG-堆垛机");
        entities.Add(railLeft);

        var railRight = _cad.AddLine(
            origin.X + railHalf, origin.Y - bodyD / 2 - 100, 0,
            origin.X + railHalf, origin.Y + bodyD / 2 + 100, 0);
        _cad.SetLayer(railRight, "LG-堆垛机");
        entities.Add(railRight);

        // 载货台标记（十字线）
        var crossH = _cad.AddLine(
            origin.X - bodyW / 2, origin.Y, 0,
            origin.X + bodyW / 2, origin.Y, 0);
        _cad.SetLayer(crossH, "LG-堆垛机");
        entities.Add(crossH);

        return entities;
    }

    // ============================================================
    // 控制柜 — 矩形+柜门标记
    // ============================================================
    public List<object> DrawCabinet(CabinetDataModel p, Point3d origin)
    {
        var entities = new List<object>();

        var rect = _cad.AddRectangle(
            origin,
            new Point3d(origin.X + p.Width, origin.Y + p.Depth, 0));
        _cad.SetLayer(rect, "LG-控制柜");
        entities.Add(rect);

        // 柜门标记（前面中间一条竖线）
        var doorLine = _cad.AddLine(
            origin.X, origin.Y + p.Depth / 2, 0,
            origin.X + p.Width, origin.Y + p.Depth / 2, 0);
        _cad.SetLayer(doorLine, "LG-控制柜");
        entities.Add(doorLine);

        return entities;
    }

    // ============================================================
    // 安全围栏 — 沿多边形路径生成围栏线
    // ============================================================
    public List<object> DrawFence(FenceDataModel p, List<Point3d> pathNodes)
    {
        var entities = new List<object>();

        for (int i = 0; i < pathNodes.Count; i++)
        {
            var start = pathNodes[i];
            var end = pathNodes[(i + 1) % pathNodes.Count];
            var fenceLine = _cad.AddLine(start.X, start.Y, 0, end.X, end.Y, 0);
            _cad.SetLayer(fenceLine, "LG-围栏");
            entities.Add(fenceLine);

            // 节点处立柱标记
            var post = _cad.AddCircle(start, 30);
            _cad.SetLayer(post, "LG-围栏");
            entities.Add(post);
        }

        return entities;
    }

    // ============================================================
    // 扫码器 — 小矩形标记+方向
    // ============================================================
    public List<object> DrawScanner(ScannerDataModel p, Point3d origin)
    {
        var entities = new List<object>();

        double sw = 150, sd = 80;
        var rect = _cad.AddRectangle(
            new Point3d(origin.X - sw / 2, origin.Y - sd / 2, 0),
            new Point3d(origin.X + sw / 2, origin.Y + sd / 2, 0));
        _cad.SetLayer(rect, "LG-扫码器");
        entities.Add(rect);

        // 扫描方向箭头（指向输送线方向）
        var arrow = _cad.AddLine(
            origin.X, origin.Y, 0,
            origin.X, origin.Y + sd / 2 + 80, 0);
        _cad.SetLayer(arrow, "LG-扫码器");
        entities.Add(arrow);

        return entities;
    }
}
