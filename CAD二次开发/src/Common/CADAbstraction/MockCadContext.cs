namespace Common.CADAbstraction;

/// <summary>
/// 模拟CAD上下文 — 用于开发调试，实际部署时替换为GstarCAD API实现
/// </summary>
public class MockCadContext : ICadContext
{
    private int _entityId = 0;
    private readonly List<MockEntity> _entities = new();
    private readonly Dictionary<string, string> _layers = new();
    private readonly Dictionary<int, Dictionary<string, string>> _xdata = new();

    public object GetCurrentDatabase() => this;

    public object AddLine(double x1, double y1, double z1, double x2, double y2, double z2)
    {
        var e = new MockEntity { Id = ++_entityId, Type = "Line" };
        _entities.Add(e);
        return e;
    }

    public object AddPolyline(IEnumerable<Point3d> points)
    {
        var e = new MockEntity { Id = ++_entityId, Type = "Polyline" };
        _entities.Add(e);
        return e;
    }

    public object AddArc(Point3d center, double radius, double startAngleDeg, double endAngleDeg)
    {
        var e = new MockEntity { Id = ++_entityId, Type = "Arc" };
        _entities.Add(e);
        return e;
    }

    public object AddCircle(Point3d center, double radius)
    {
        var e = new MockEntity { Id = ++_entityId, Type = "Circle" };
        _entities.Add(e);
        return e;
    }

    public object AddRectangle(Point3d minCorner, Point3d maxCorner)
    {
        var points = new[]
        {
            minCorner,
            new Point3d(maxCorner.X, minCorner.Y, 0),
            maxCorner,
            new Point3d(minCorner.X, maxCorner.Y, 0),
            minCorner
        };
        return AddPolyline(points);
    }

    public object AddText(string text, Point3d position, double height)
    {
        var e = new MockEntity { Id = ++_entityId, Type = "Text" };
        _entities.Add(e);
        return e;
    }

    public object AddAlignedDimension(Point3d start, Point3d end, Point3d textPosition)
    {
        var e = new MockEntity { Id = ++_entityId, Type = "Dimension" };
        _entities.Add(e);
        return e;
    }

    public object CreateBlockDefinition(string blockName, IEnumerable<object> entities)
        => new MockEntity { Id = ++_entityId, Type = "BlockDef" };

    public object InsertBlockReference(string blockName, Point3d position)
        => new MockEntity { Id = ++_entityId, Type = "BlockRef" };

    public object GetOrCreateLayer(string layerName)
    {
        if (!_layers.ContainsKey(layerName))
            _layers[layerName] = layerName;
        return layerName;
    }

    public void SetLayer(object entity, string layerName)
    {
        if (entity is MockEntity me)
        {
            me.Layer = layerName;
            GetOrCreateLayer(layerName);
        }
    }

    public string GetLayer(object entity)
    {
        if (entity is MockEntity me) return me.Layer ?? "";
        return "";
    }

    public void SetColor(object entity, short colorIndex) { }

    public void SetLinetype(object entity, string linetypeName) { }

    public void AddToCurrentSpace(object entity) { }

    public object StartTransaction() => new object();

    public void CommitTransaction(object transaction) { }

    public void AbortTransaction(object transaction) { }

    public Point3d? GetPoint(string prompt) => null;

    public IEnumerable<Point3d> GetPoints(string prompt) => Array.Empty<Point3d>();

    public object? GetSelection(string prompt) => null;

    public void SetXData(object entity, string key, string value)
    {
        if (entity is MockEntity me)
        {
            if (!_xdata.ContainsKey(me.Id))
                _xdata[me.Id] = new Dictionary<string, string>();
            _xdata[me.Id][key] = value;
        }
    }

    public string? GetXData(object entity, string key)
    {
        if (entity is MockEntity me &&
            _xdata.TryGetValue(me.Id, out var data) &&
            data.TryGetValue(key, out var val))
            return val;
        return null;
    }

    public IEnumerable<object> GetAllEntities() => _entities;
}

/// <summary>
/// 模拟图元
/// </summary>
public class MockEntity
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Layer { get; set; }
}
