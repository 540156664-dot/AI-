using Common;
using Common.CADAbstraction;
using ConveyorGenerator;
using RackGenerator;
using EquipmentGenerator;
using BOMExporter;

namespace RackPlugin.Tests;

public class RackDataModelTests
{
    [Fact]
    public void CellWidth_Calculation()
    {
        var p = new RackDataModel
        {
            L = 2700,
            NCol = 10,
            GapX = 75
        };

        double expected = (2700.0 - 75 * 9) / 10; // = 202.5
        Assert.Equal(expected, p.CellWidth, 2);
    }

    [Fact]
    public void BackToBackDepth_Calculation()
    {
        var p = new RackDataModel { D = 1000, GapY = 200 };
        Assert.Equal(2200, p.BackToBackDepth, 2);
    }

    [Fact]
    public void RowPitch_Calculation()
    {
        var p = new RackDataModel { D = 1000, WAisle = 3500 };
        Assert.Equal(4500, p.RowPitch, 2);
    }

    [Fact]
    public void Default_Values_Are_Reasonable()
    {
        var p = new RackDataModel();
        Assert.True(p.L > 0);
        Assert.True(p.D > 0);
        Assert.True(p.H > 0);
        Assert.True(p.NLayer > 0);
        Assert.True(p.CellWidth > 0);
    }
}

public class PathSolverTests
{
    [Fact]
    public void TwoPoints_OneStraightSegment()
    {
        var solver = new PathSolver();
        var nodes = new List<Point3d>
        {
            new(0, 0, 0),
            new(1000, 0, 0)
        };

        var segments = solver.Solve(nodes);
        Assert.Single(segments);
        Assert.Equal(PathSegmentType.Straight, segments[0].Type);
        Assert.Equal(1000, segments[0].Length, 2);
    }

    [Fact]
    public void ThreePoints_With90DegTurn_DetectsTurn()
    {
        var solver = new PathSolver();
        var nodes = new List<Point3d>
        {
            new(0, 0, 0),
            new(1000, 0, 0),
            new(1000, 1000, 0)
        };

        var segments = solver.Solve(nodes);
        // 应该包含转弯段
        Assert.Contains(segments, s => s.Type == PathSegmentType.Turn);
    }

    [Fact]
    public void StraightLine_NoTurn()
    {
        var solver = new PathSolver();
        var nodes = new List<Point3d>
        {
            new(0, 0, 0),
            new(1000, 0, 0),
            new(2000, 0, 0)
        };

        var segments = solver.Solve(nodes);
        Assert.DoesNotContain(segments, s => s.Type == PathSegmentType.Turn);
    }

    [Fact]
    public void AngleBetween_Vectors_90Deg()
    {
        var v1 = new Point3d(1, 0, 0);
        var v2 = new Point3d(0, 1, 0);
        var angle = PathSolver.AngleBetween(v1, v2);
        Assert.Equal(90, angle, 1);
    }

    [Fact]
    public void AngleBetween_Vectors_0Deg()
    {
        var v1 = new Point3d(1, 0, 0);
        var v2 = new Point3d(2, 0, 0);
        var angle = PathSolver.AngleBetween(v1, v2);
        Assert.Equal(0, angle, 1);
    }

    [Fact]
    public void AngleBetween_Vectors_180Deg()
    {
        var v1 = new Point3d(1, 0, 0);
        var v2 = new Point3d(-1, 0, 0);
        var angle = PathSolver.AngleBetween(v1, v2);
        Assert.Equal(180, angle, 1);
    }
}

public class AutoNumberingTests
{
    [Fact]
    public void FirstNumber_NoExisting()
    {
        var numbering = new AutoNumbering();
        var existing = new HashSet<string>();

        var num = numbering.GetNextNumber("堆垛机", existing);
        Assert.Equal("SRM01", num);
    }

    [Fact]
    public void SkipsExistingNumbers()
    {
        var numbering = new AutoNumbering();
        var existing = new HashSet<string> { "SRM01", "SRM02" };

        var num = numbering.GetNextNumber("堆垛机", existing);
        Assert.Equal("SRM03", num);
    }

    [Fact]
    public void FormatNumber_ZeroPadding()
    {
        Assert.Equal("L001", AutoNumbering.FormatNumber("L", 1, 3));
        Assert.Equal("L099", AutoNumbering.FormatNumber("L", 99, 3));
        Assert.Equal("RK05", AutoNumbering.FormatNumber("RK", 5, 2));
    }

    [Fact]
    public void Duplicate_Detection()
    {
        var numbering = new AutoNumbering();
        var existing = new HashSet<string> { "SRM01" };

        Assert.True(numbering.IsDuplicate("SRM01", existing));
        Assert.False(numbering.IsDuplicate("SRM02", existing));
    }

    [Fact]
    public void ConveyorLine_UsesThreeDigits()
    {
        var numbering = new AutoNumbering();
        var existing = new HashSet<string>();
        Assert.Equal("L001", numbering.GetNextNumber("滚筒输送线", existing));
    }
}

public class BOMExportTests
{
    [Fact]
    public void EmptyDrawing_ReturnsEmptyList()
    {
        var cad = new MockCadContext();
        var service = new BOMExportService(cad);
        var result = service.ScanAllDevices();
        Assert.Empty(result);
    }

    [Fact]
    public void KnownDevice_CountsCorrectly()
    {
        var cad = new MockCadContext();
        var line = cad.AddLine(0, 0, 0, 100, 0, 0);
        cad.SetXData(line, "DeviceType", "堆垛机");
        cad.SetXData(line, "DeviceNumber", "SRM01");

        var service = new BOMExportService(cad);
        var result = service.ScanAllDevices();

        Assert.Single(result);
        Assert.Equal("堆垛机", result[0].DeviceType);
        Assert.Equal(1, result[0].Count);
    }

    [Fact]
    public void MultipleDevices_AggregatesCorrectly()
    {
        var cad = new MockCadContext();

        for (int i = 0; i < 5; i++)
        {
            var line = cad.AddLine(0, i * 100, 0, 100, i * 100, 0);
            cad.SetXData(line, "DeviceType", "货架排");
            cad.SetXData(line, "DeviceNumber", $"RK{i + 1:D2}");
        }

        var service = new BOMExportService(cad);
        var result = service.ScanAllDevices();

        Assert.Single(result);
        Assert.Equal(5, result[0].Count);
    }

    [Fact]
    public void CSVExport_GeneratesCorrectFormat()
    {
        var cad = new MockCadContext();
        var service = new BOMExportService(cad);
        var entries = new List<BOMEntry>
        {
            new() { DeviceType = "堆垛机", Specification = "H8000-500kg", Count = 2, Unit = "台" },
            new() { DeviceType = "滚筒输送线", Specification = "W1000×H750", Count = 85, Unit = "米" }
        };

        var csv = service.ExportToCSV(entries);
        Assert.Contains("堆垛机", csv);
        Assert.Contains("H8000-500kg", csv);
        Assert.Contains("2", csv);
        Assert.Contains("85", csv);
    }
}

public class PresetManagerTests
{
    [Fact]
    public void SaveAndLoad_PreservesData()
    {
        var mgr = new PresetManager(Path.Combine(Path.GetTempPath(), "RackTest_Presets"));
        var data = new RackDataModel { L = 3000, D = 1200, H = 8000, NLayer = 5 };

        mgr.SavePreset("货架", "test-preset", data);
        var loaded = mgr.LoadPreset<RackDataModel>("货架", "test-preset");

        Assert.NotNull(loaded);
        Assert.Equal(3000, loaded!.L);
        Assert.Equal(1200, loaded.D);
        Assert.Equal(8000, loaded.H);
        Assert.Equal(5, loaded.NLayer);

        mgr.DeletePreset("货架", "test-preset");
    }

    [Fact]
    public void ListPresets_ReturnsNames()
    {
        var mgr = new PresetManager(Path.Combine(Path.GetTempPath(), "RackTest_Presets2"));
        mgr.SavePreset("货架", "standard", new RackDataModel());
        mgr.SavePreset("货架", "large", new RackDataModel { L = 3300 });

        var list = mgr.ListPresets("货架");
        Assert.Contains("standard", list);
        Assert.Contains("large", list);

        mgr.DeletePreset("货架", "standard");
        mgr.DeletePreset("货架", "large");
    }
}

public class Point3dTests
{
    [Fact]
    public void Addition_Works()
    {
        var a = new Point3d(10, 20, 5);
        var b = new Point3d(5, 10, 2);
        var c = a + b;
        Assert.Equal(15, c.X);
        Assert.Equal(30, c.Y);
        Assert.Equal(7, c.Z);
    }

    [Fact]
    public void Distance_Calculation()
    {
        var a = new Point3d(0, 0, 0);
        var b = new Point3d(300, 400, 0);
        Assert.Equal(500, a.DistanceTo(b), 2);
    }
}

public class EquipmentDrawerTests
{
    [Fact]
    public void DrawStacker_CreatesEntities()
    {
        var cad = new MockCadContext();
        var drawer = new EquipmentDrawer(cad);
        var model = new StackerDataModel();

        var entities = drawer.DrawStacker(model, new Point3d(1000, 1000, 0));
        Assert.NotEmpty(entities);
        Assert.True(entities.Count >= 3); // body + 2 rails at minimum
    }

    [Fact]
    public void DrawCabinet_CreatesEntities()
    {
        var cad = new MockCadContext();
        var drawer = new EquipmentDrawer(cad);
        var model = new CabinetDataModel();

        var entities = drawer.DrawCabinet(model, new Point3d(0, 0, 0));
        Assert.NotEmpty(entities);
    }

    [Fact]
    public void DrawScanner_CreatesEntities()
    {
        var cad = new MockCadContext();
        var drawer = new EquipmentDrawer(cad);
        var model = new ScannerDataModel();

        var entities = drawer.DrawScanner(model, new Point3d(500, 500, 0));
        Assert.NotEmpty(entities);
    }
}
