// #####################################################################
// AutoCadContext.cs — ICadContext 的 AutoCAD 2020 .NET API 完整实现
//
// 在安装了 AutoCAD 2020 + ObjectARX SDK 的机器上：
//   1. 项目添加引用: accoremgd.dll / acdbmgd.dll / acmgd.dll
//      (位于 C:\Program Files\Autodesk\AutoCAD 2020\)
//   2. 把文件末尾的 #if FALSE 改为 #if TRUE
//   3. 编译即可
//
// 开发机（无AutoCAD）上此文件不参与编译，不影响其他模块构建和测试。
// #####################################################################

#if FALSE
// ======================== 以下代码仅在 AutoCAD 机器上编译 ========================

using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Common.CADAbstraction;

namespace AcRackAddin;

public class AutoCadContext : ICadContext
{
    private const string XDATA_APP = "ACRACKPLUGIN";

    private Document Doc =>
        Application.DocumentManager.MdiActiveDocument
        ?? throw new System.InvalidOperationException("没有打开的CAD文档");

    private Database Db => Doc.Database;

    private Transaction GetOrStartTransaction()
    {
        if (Doc.TransactionManager.TopTransaction is Transaction top)
            return top;
        return Doc.TransactionManager.StartTransaction();
    }

    private ObjectId AddToModelSpace(Transaction tr, Entity ent)
    {
        var bt = (BlockTable)tr.GetObject(Db.BlockTableId, OpenMode.ForRead);
        var btr = (BlockTableRecord)tr.GetObject(
            bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        var id = btr.AppendEntity(ent);
        tr.AddNewlyCreatedDBObject(ent, true);
        return id;
    }

    private void EnsureRegAppTable()
    {
        var tr = GetOrStartTransaction();
        var rat = (RegAppTable)tr.GetObject(Db.RegAppTableId, OpenMode.ForRead);
        if (!rat.Has(XDATA_APP))
        {
            rat.UpgradeOpen();
            var rec = new RegAppTableRecord { Name = XDATA_APP };
            rat.Add(rec);
            tr.AddNewlyCreatedDBObject(rec, true);
        }
    }

    // ======================== 事务 ========================
    public object StartTransaction() => Doc.TransactionManager.StartTransaction();
    public void CommitTransaction(object t) { if (t is Transaction tr) tr.Commit(); }
    public void AbortTransaction(object t) { if (t is Transaction tr) tr.Abort(); }
    public object GetCurrentDatabase() => Db;

    // ======================== 直线 ========================
    public object AddLine(double x1, double y1, double z1,
                          double x2, double y2, double z2)
    {
        var tr = GetOrStartTransaction();
        return AddToModelSpace(tr, new Line(
            new Point3d(x1, y1, z1), new Point3d(x2, y2, z2)));
    }

    // ======================== 多段线 ========================
    public object AddPolyline(IEnumerable<Common.CADAbstraction.Point3d> points)
    {
        var tr = GetOrStartTransaction();
        var pl = new Polyline();
        int i = 0;
        foreach (var p in points)
            pl.AddVertexAt(i++, new Point2d(p.X, p.Y), 0, 0, 0);
        pl.Closed = true;
        return AddToModelSpace(tr, pl);
    }

    // ======================== 圆弧 ========================
    public object AddArc(Common.CADAbstraction.Point3d center, double radius,
                         double startDeg, double endDeg)
    {
        var tr = GetOrStartTransaction();
        return AddToModelSpace(tr, new Arc(
            new Point3d(center.X, center.Y, center.Z),
            radius,
            startDeg * System.Math.PI / 180.0,
            endDeg * System.Math.PI / 180.0));
    }

    // ======================== 圆 ========================
    public object AddCircle(Common.CADAbstraction.Point3d center, double radius)
    {
        var tr = GetOrStartTransaction();
        return AddToModelSpace(tr, new Circle(
            new Point3d(center.X, center.Y, center.Z),
            Vector3d.ZAxis, radius));
    }

    // ======================== 矩形（闭合多段线） ========================
    public object AddRectangle(Common.CADAbstraction.Point3d min,
                               Common.CADAbstraction.Point3d max)
    {
        var tr = GetOrStartTransaction();
        var pl = new Polyline();
        pl.AddVertexAt(0, new Point2d(min.X, min.Y), 0, 0, 0);
        pl.AddVertexAt(1, new Point2d(max.X, min.Y), 0, 0, 0);
        pl.AddVertexAt(2, new Point2d(max.X, max.Y), 0, 0, 0);
        pl.AddVertexAt(3, new Point2d(min.X, max.Y), 0, 0, 0);
        pl.Closed = true;
        return AddToModelSpace(tr, pl);
    }

    // ======================== 文字 ========================
    public object AddText(string text, Common.CADAbstraction.Point3d pos,
                          double height)
    {
        var tr = GetOrStartTransaction();
        return AddToModelSpace(tr, new DBText
        {
            Position = new Point3d(pos.X, pos.Y, pos.Z),
            Height = height,
            TextString = text
        });
    }

    // ======================== 对齐标注 ========================
    public object AddAlignedDimension(Common.CADAbstraction.Point3d start,
                                       Common.CADAbstraction.Point3d end,
                                       Common.CADAbstraction.Point3d textPos)
    {
        var tr = GetOrStartTransaction();
        return AddToModelSpace(tr, new AlignedDimension(
            new Point3d(start.X, start.Y, start.Z),
            new Point3d(end.X, end.Y, end.Z),
            new Point3d(textPos.X, textPos.Y, textPos.Z),
            "", Db.Dimstyle));
    }

    // ======================== 图块 ========================
    public object CreateBlockDefinition(string name, IEnumerable<object> entities)
    {
        var tr = GetOrStartTransaction();
        var bt = (BlockTable)tr.GetObject(Db.BlockTableId, OpenMode.ForWrite);
        if (bt.Has(name)) return bt[name];

        var btr = new BlockTableRecord { Name = name };
        var id = bt.Add(btr);
        tr.AddNewlyCreatedDBObject(btr, true);

        foreach (var e in entities)
            if (e is Entity ent) btr.AppendEntity(ent);

        return id;
    }

    public object InsertBlockReference(string name,
                                        Common.CADAbstraction.Point3d pos)
    {
        var tr = GetOrStartTransaction();
        var bt = (BlockTable)tr.GetObject(Db.BlockTableId, OpenMode.ForRead);
        if (!bt.Has(name)) return ObjectId.Null;
        return AddToModelSpace(tr,
            new BlockReference(new Point3d(pos.X, pos.Y, pos.Z), bt[name]));
    }

    // ======================== 图层 ========================
    public object GetOrCreateLayer(string layerName)
    {
        var tr = GetOrStartTransaction();
        var lt = (LayerTable)tr.GetObject(Db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(layerName)) return lt[layerName];

        lt.UpgradeOpen();
        var ltr = new LayerTableRecord { Name = layerName };
        var id = lt.Add(ltr);
        tr.AddNewlyCreatedDBObject(ltr, true);
        return id;
    }

    public void SetLayer(object entity, string layerName)
    {
        if (entity is ObjectId id && !id.IsNull)
        {
            var tr = GetOrStartTransaction();
            if (tr.GetObject(id, OpenMode.ForWrite) is Entity ent)
            {
                ent.Layer = layerName;
                GetOrCreateLayer(layerName); // 确保图层存在
            }
        }
    }

    public string GetLayer(object entity)
    {
        if (entity is ObjectId id && !id.IsNull)
        {
            var tr = GetOrStartTransaction();
            if (tr.GetObject(id, OpenMode.ForRead) is Entity ent)
                return ent.Layer;
        }
        return "0";
    }

    public void SetColor(object entity, short colorIndex)
    {
        if (entity is ObjectId id && !id.IsNull)
        {
            var tr = GetOrStartTransaction();
            if (tr.GetObject(id, OpenMode.ForWrite) is Entity ent)
                ent.ColorIndex = colorIndex;
        }
    }

    public void SetLinetype(object entity, string ltName)
    {
        if (entity is ObjectId id && !id.IsNull)
        {
            var tr = GetOrStartTransaction();
            if (tr.GetObject(id, OpenMode.ForWrite) is Entity ent)
                ent.Linetype = ltName;
        }
    }

    public void AddToCurrentSpace(object entity) { }

    // ======================== 扩展数据 ========================
    public void SetXData(object entity, string key, string value)
    {
        if (entity is not ObjectId id || id.IsNull) return;
        var tr = GetOrStartTransaction();
        EnsureRegAppTable();
        if (tr.GetObject(id, OpenMode.ForWrite) is not DBObject obj) return;

        var dict = new Dictionary<string, string>();
        var oldRb = obj.XData;
        if (oldRb != null)
        {
            foreach (var tv in oldRb.AsArray())
            {
                if (tv.TypeCode == 1001) continue;
                if (tv.TypeCode == 1000 && tv.Value is string s && s.Contains('='))
                {
                    var parts = s.Split(new[] { '=' }, 2);
                    if (parts.Length == 2) dict[parts[0]] = parts[1];
                }
            }
        }
        dict[key] = value;

        var rb = new ResultBuffer(new TypedValue(1001, XDATA_APP));
        foreach (var kv in dict)
            rb.Add(new TypedValue(1000, kv.Key + "=" + kv.Value));
        obj.XData = rb;
    }

    public string? GetXData(object entity, string key)
    {
        if (entity is not ObjectId id || id.IsNull) return null;
        var tr = GetOrStartTransaction();
        if (tr.GetObject(id, OpenMode.ForRead) is not DBObject obj) return null;
        var rb = obj.XData;
        if (rb == null) return null;

        foreach (var tv in rb.AsArray())
        {
            if (tv.TypeCode == 1000 && tv.Value is string s && s.Contains('='))
            {
                var parts = s.Split(new[] { '=' }, 2);
                if (parts.Length == 2 && parts[0] == key)
                    return parts[1];
            }
        }
        return null;
    }

    // ======================== 用户交互 ========================
    public Common.CADAbstraction.Point3d? GetPoint(string prompt)
    {
        var ppr = Doc.Editor.GetPoint("\n" + prompt);
        if (ppr.Status == PromptStatus.OK)
            return new Common.CADAbstraction.Point3d(
                ppr.Value.X, ppr.Value.Y, ppr.Value.Z);
        return null;
    }

    public IEnumerable<Common.CADAbstraction.Point3d> GetPoints(string prompt)
    {
        while (true)
        {
            var ppr = Doc.Editor.GetPoint("\n" + prompt + " (回车结束)");
            if (ppr.Status != PromptStatus.OK) yield break;
            yield return new Common.CADAbstraction.Point3d(
                ppr.Value.X, ppr.Value.Y, ppr.Value.Z);
        }
    }

    public object? GetSelection(string prompt)
    {
        var opts = new PromptSelectionOptions { MessageForAdding = prompt };
        var psr = Doc.Editor.GetSelection(opts);
        return psr.Status == PromptStatus.OK ? psr.Value : null;
    }

    // ======================== 全图扫描 ========================
    public IEnumerable<object> GetAllEntities()
    {
        var tr = GetOrStartTransaction();
        var bt = (BlockTable)tr.GetObject(Db.BlockTableId, OpenMode.ForRead);
        var btr = (BlockTableRecord)tr.GetObject(
            bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
        foreach (ObjectId id in btr)
            yield return id;
    }
}

#endif // FALSE — 改为 TRUE 启用AutoCAD编译
