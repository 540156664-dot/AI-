namespace Common.CADAbstraction;

/// <summary>
/// 三维点结构体（CAD坐标系，单位mm）
/// </summary>
public struct Point3d
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Point3d(double x, double y, double z = 0)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Point3d operator +(Point3d a, Point3d b)
        => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Point3d operator -(Point3d a, Point3d b)
        => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public double DistanceTo(Point3d other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}
