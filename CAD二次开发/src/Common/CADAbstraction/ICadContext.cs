namespace Common.CADAbstraction;

/// <summary>
/// CAD上下文接口 — 隔离浩辰CAD .NET API依赖。
/// 实际部署时用GstarCAD API实现此接口。
/// </summary>
public interface ICadContext
{
    /// <summary>获取当前文档数据库</summary>
    object GetCurrentDatabase();

    /// <summary>在当前空间中创建直线</summary>
    object AddLine(double x1, double y1, double z1, double x2, double y2, double z2);

    /// <summary>在当前空间中创建多段线</summary>
    object AddPolyline(IEnumerable<Point3d> points);

    /// <summary>在当前空间中创建圆弧</summary>
    object AddArc(Point3d center, double radius, double startAngleDeg, double endAngleDeg);

    /// <summary>在当前空间中创建圆</summary>
    object AddCircle(Point3d center, double radius);

    /// <summary>在当前空间中创建矩形（闭合多段线）</summary>
    object AddRectangle(Point3d minCorner, Point3d maxCorner);

    /// <summary>在当前空间中创建单行文字</summary>
    object AddText(string text, Point3d position, double height);

    /// <summary>在当前空间中创建对齐标注</summary>
    object AddAlignedDimension(Point3d start, Point3d end, Point3d textPosition);

    /// <summary>创建图块定义</summary>
    object CreateBlockDefinition(string blockName, IEnumerable<object> entities);

    /// <summary>插入图块引用</summary>
    object InsertBlockReference(string blockName, Point3d position);

    /// <summary>获取或创建图层</summary>
    object GetOrCreateLayer(string layerName);

    /// <summary>设置图元的图层</summary>
    void SetLayer(object entity, string layerName);

    /// <summary>获取图元所在图层</summary>
    string GetLayer(object entity);

    /// <summary>设置图元颜色索引</summary>
    void SetColor(object entity, short colorIndex);

    /// <summary>设置线型</summary>
    void SetLinetype(object entity, string linetypeName);

    /// <summary>将图元添加到当前空间</summary>
    void AddToCurrentSpace(object entity);

    /// <summary>启动事务</summary>
    object StartTransaction();

    /// <summary>提交事务</summary>
    void CommitTransaction(object transaction);

    /// <summary>回滚事务</summary>
    void AbortTransaction(object transaction);

    /// <summary>提示用户选择点</summary>
    Point3d? GetPoint(string prompt);

    /// <summary>提示用户选择多个点</summary>
    IEnumerable<Point3d> GetPoints(string prompt);

    /// <summary>提示用户选择图元</summary>
    object? GetSelection(string prompt);

    /// <summary>将扩展数据附加到图元</summary>
    void SetXData(object entity, string key, string value);

    /// <summary>从图元读取扩展数据</summary>
    string? GetXData(object entity, string key);

    /// <summary>扫描当前空间所有图元</summary>
    IEnumerable<object> GetAllEntities();
}
