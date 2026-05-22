using Common.CADAbstraction;

namespace ConveyorGenerator;

/// <summary>
/// 路径解析器 — 将节点列表解析为直线段/转弯段序列
/// </summary>
public class PathSolver
{
    /// <summary>
    /// 解析路径节点为路径段列表
    /// 三个点 A→B→C，如果A-B向量与B-C向量夹角超过阈值，则在B处插入转弯段
    /// </summary>
    public List<PathSegment> Solve(List<Point3d> nodes, double turnThresholdDeg = 2.0)
    {
        var segments = new List<PathSegment>();
        if (nodes.Count < 2) return segments;

        for (int i = 0; i < nodes.Count - 1; i++)
        {
            var start = nodes[i];
            var end = nodes[i + 1];
            var dist = start.DistanceTo(end);

            // 判断下一个点（如果有）是否需要转弯
            double angle = 0;
            if (i < nodes.Count - 2)
            {
                var v1 = new Point3d(end.X - start.X, end.Y - start.Y, 0);
                var nextEnd = nodes[i + 2];
                var v2 = new Point3d(nextEnd.X - end.X, nextEnd.Y - end.Y, 0);
                angle = AngleBetween(v1, v2);
            }

            if (angle > turnThresholdDeg)
            {
                // 插入一个短的转弯段
                segments.Add(new PathSegment
                {
                    Type = PathSegmentType.Straight,
                    Start = start,
                    End = end,
                    Length = dist - 0.01 // 留一点给转弯
                });
                segments.Add(new PathSegment
                {
                    Type = PathSegmentType.Turn,
                    Start = end,
                    End = end,
                    Length = 0,
                    TurnAngleDeg = angle
                });
            }
            else
            {
                segments.Add(new PathSegment
                {
                    Type = PathSegmentType.Straight,
                    Start = start,
                    End = end,
                    Length = dist
                });
            }
        }

        return segments;
    }

    /// <summary>
    /// 计算两个向量之间的夹角（度）
    /// </summary>
    public static double AngleBetween(Point3d v1, Point3d v2)
    {
        var dot = v1.X * v2.X + v1.Y * v2.Y;
        var mag1 = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);
        var mag2 = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y);
        if (mag1 < 1e-6 || mag2 < 1e-6) return 0;

        var cosAngle = (dot / (mag1 * mag2));
            cosAngle = cosAngle > 1 ? 1 : (cosAngle < -1 ? -1 : cosAngle);
        return Math.Acos(cosAngle) * 180.0 / Math.PI;
    }

    /// <summary>
    /// 获取路径总长度
    /// </summary>
    public double TotalLength(List<PathSegment> segments)
        => segments.Where(s => s.Type == PathSegmentType.Straight).Sum(s => s.Length);
}
