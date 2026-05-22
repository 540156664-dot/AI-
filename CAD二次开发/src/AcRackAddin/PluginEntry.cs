// #####################################################################
// PluginEntry.cs — AutoCAD 2020 插件入口（IExtensionApplication）
//
// 在安装了 AutoCAD 2020 的机器上：
//   1. 确保 AutoCadContext.cs 已启用（#if FALSE → #if TRUE）
//   2. 此文件同样需要启用（#if FALSE → #if TRUE）
//   3. 编译后用 NETLOAD 命令加载 AcRackAddin.dll
// #####################################################################

#if FALSE
// ======================== 以下代码仅在 AutoCAD 机器上编译 ========================

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows;
using Common;
using Common.CADAbstraction;
using RackUI;

[assembly: ExtensionApplication(typeof(AcRackAddin.PluginEntry))]

namespace AcRackAddin;

public class PluginEntry : IExtensionApplication
{
    internal static ICadContext CadContext { get; private set; } = null!;
    internal static AutoCadContext RealCad { get; private set; } = null!;
    private static MainPalette? _palette;
    private static PaletteSet? _ps;

    // ======================== 生命周期 ========================
    public void Initialize()
    {
        RealCad = new AutoCadContext();
        CadContext = RealCad;

        foreach (var layer in LayerDefinitions.Layers)
            CadContext.GetOrCreateLayer(layer.Name);

        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc != null)
        {
            doc.Editor.WriteMessage("\n==============================================");
            doc.Editor.WriteMessage("\n  物流方案自动生成插件 v1.0 已加载");
            doc.Editor.WriteMessage("\n  可用命令:");
            doc.Editor.WriteMessage("\n    RACKGEN  - 货架生成         CONVGEN  - 输送机生成");
            doc.Editor.WriteMessage("\n    STKGEN   - 堆垛机生成       CABGEN   - 控制柜生成");
            doc.Editor.WriteMessage("\n    FENCEGEN - 安全围栏生成     SCANGEN  - 扫码器生成");
            doc.Editor.WriteMessage("\n    BOMLIST  - 设备清单统计     RACKSET  - 系统设置");
            doc.Editor.WriteMessage("\n    TESTPLUGIN - 运行诊断测试");
            doc.Editor.WriteMessage("\n==============================================");
        }
    }

    public void Terminate()
    {
        if (_ps != null) { _ps.Dispose(); _ps = null; }
    }

    // ======================== 面板 ========================
    private static void EnsurePalette()
    {
        if (_ps == null)
        {
            _palette = new MainPalette();
            _ps = new PaletteSet("物流方案生成")
            {
                MinimumSize = new System.Drawing.Size(340, 500),
                Visible = true
            };
            _ps.Add("控制面板", _palette);
            _ps.Dock = DockSides.Right;
        }
        _ps.Visible = true;
    }

    private static void ShowTab(int index)
    {
        EnsurePalette();
        _palette?.SwitchToTab(index);
    }

    // ======================== 命令 ========================
    [CommandMethod("RACKGEN")]   public void CmdRackGen()   => ShowTab(0);
    [CommandMethod("CONVGEN")]   public void CmdConvGen()   => ShowTab(1);
    [CommandMethod("STKGEN")]    public void CmdStkGen()    => ShowTab(2);
    [CommandMethod("CABGEN")]    public void CmdCabGen()    => ShowTab(3);
    [CommandMethod("FENCEGEN")]  public void CmdFenceGen()  => ShowTab(4);
    [CommandMethod("SCANGEN")]   public void CmdScanGen()   => ShowTab(5);
    [CommandMethod("BOMLIST")]   public void CmdBomList()   => ShowTab(6);
    [CommandMethod("RACKSET")]   public void CmdRackSet()   => ShowTab(7);

    [CommandMethod("TESTPLUGIN")]
    public void CmdTest()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null) return;

        using var tr = doc.Database.TransactionManager.StartTransaction();
        var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId,
            OpenMode.ForRead);
        var btr = (BlockTableRecord)tr.GetObject(
            bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

        // 绘制测试矩形 — 横梁式货架俯视图 2700x1000
        var pl = new Polyline();
        pl.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
        pl.AddVertexAt(1, new Point2d(2700, 0), 0, 0, 0);
        pl.AddVertexAt(2, new Point2d(2700, 1000), 0, 0, 0);
        pl.AddVertexAt(3, new Point2d(0, 1000), 0, 0, 0);
        pl.Closed = true;
        btr.AppendEntity(pl);
        tr.AddNewlyCreatedDBObject(pl, true);
        tr.Commit();

        doc.Editor.WriteMessage("\n[TEST] 物流插件核心功能正常 — 2700×1000 测试货架已绘制。");
    }
}

#endif // FALSE — 改为 TRUE 启用AutoCAD编译
