using Common.CADAbstraction;

namespace RackGenerator;

/// <summary>
/// 货架阵列布局器 — 多排多列自动排布
/// </summary>
public class RackArrayBuilder
{
    private readonly RackDrawer _drawer;
    private readonly ICadContext _cad;

    public RackArrayBuilder(ICadContext cad)
    {
        _cad = cad;
        _drawer = new RackDrawer(cad);
    }

    /// <summary>
    /// 生成整个货架阵列布局
    /// </summary>
    /// <param name="p">货架参数</param>
    /// <param name="insertPoint">整片货架区的左下角基准点</param>
    /// <returns>生成结果（图纸编号 + 图元列表）</returns>
    public RackArrayResult Build(RackDataModel p, Point3d insertPoint)
    {
        var result = new RackArrayResult();
        double currentY = insertPoint.Y;

        for (int row = 0; row < p.NRow; row++)
        {
            // 背靠背模式：每两排为一组，组内两排之间gap_y，组之间为巷道
            bool isBack = (row % 2 == 1);

            double rowOriginY;
            if (isBack)
            {
                // 背靠背的第二排：紧挨前一排（间距gap_y）
                rowOriginY = currentY - p.D - p.GapY;
            }
            else
            {
                rowOriginY = currentY;
            }

            var rowOrigin = new Point3d(insertPoint.X, rowOriginY, 0);
            var entities = _drawer.DrawTopView(p, rowOrigin);

            // 尺寸标注
            var dimOrigin = new Point3d(insertPoint.X + p.L + 500, rowOriginY, 0);
            _cad.AddAlignedDimension(
                new Point3d(insertPoint.X, rowOriginY, 0),
                new Point3d(insertPoint.X + p.L, rowOriginY, 0),
                new Point3d(insertPoint.X + p.L / 2, rowOriginY - 600, 0));

            result.Rows.Add(new RackRowResult
            {
                RowNumber = row + 1,
                RowType = isBack ? "背靠背" : "主排",
                Origin = rowOrigin,
                Entities = entities
            });

            // 更新Y坐标到下一排
            if (isBack)
            {
                currentY = rowOriginY - p.WAisle;
            }
            else
            {
                currentY = rowOriginY;
            }
        }

        return result;
    }
}

/// <summary>
/// 货架阵列生成结果
/// </summary>
public class RackArrayResult
{
    public List<RackRowResult> Rows { get; set; } = new();
}

/// <summary>
/// 单排货架生成结果
/// </summary>
public class RackRowResult
{
    public int RowNumber { get; set; }
    public string RowType { get; set; } = string.Empty;
    public Point3d Origin { get; set; }
    public List<object> Entities { get; set; } = new();
}
