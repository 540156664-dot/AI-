using System.Text;
using System.Text.Json;
using Common;
using Common.CADAbstraction;

namespace BOMExporter;

/// <summary>
/// 设备清单统计与导出
/// </summary>
public class BOMExportService
{
    private readonly ICadContext _cad;

    public BOMExportService(ICadContext cad)
    {
        _cad = cad;
    }

    /// <summary>
    /// 扫描全图所有设备块，按类型统计
    /// </summary>
    public List<BOMEntry> ScanAllDevices()
    {
        var entries = new Dictionary<string, BOMEntry>();
        var entities = _cad.GetAllEntities();

        foreach (var entity in entities)
        {
            var typeJson = _cad.GetXData(entity, "DeviceType");
            var numberJson = _cad.GetXData(entity, "DeviceNumber");
            var paramJson = _cad.GetXData(entity, "ParameterJson");

            if (string.IsNullOrEmpty(typeJson)) continue;

            var deviceType = typeJson;
            var deviceNumber = numberJson ?? "-";

            if (!entries.ContainsKey(deviceType))
            {
                entries[deviceType] = new BOMEntry
                {
                    DeviceType = deviceType,
                    Count = 0,
                    Numbers = new List<string>()
                };
            }

            entries[deviceType].Count++;
            entries[deviceType].Numbers!.Add(deviceNumber);

            // 尝试读取型号规格
            if (!string.IsNullOrEmpty(paramJson))
            {
                try
                {
                    var spec = ParseSpec(deviceType, paramJson);
                    if (!string.IsNullOrEmpty(spec))
                        entries[deviceType].Specification = spec;
                }
                catch { }
            }
        }

        return entries.Values.OrderBy(e => e.DeviceType).ToList();
    }

    /// <summary>
    /// 导出为CSV
    /// </summary>
    public string ExportToCSV(List<BOMEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("序号,设备类型,型号/规格,数量,单位,备注");
        int seq = 1;
        foreach (var e in entries)
        {
            sb.AppendLine($"{seq},\"{e.DeviceType}\",\"{e.Specification}\",{e.Count},{e.Unit},\"{e.Remark}\"");
            seq++;
        }
        return sb.ToString();
    }

    /// <summary>
    /// 导出为简单的表格数据（供Excel生成使用）
    /// </summary>
    public List<Dictionary<string, string>> ExportAsTable(List<BOMEntry> entries)
    {
        var table = new List<Dictionary<string, string>>();
        int seq = 1;
        foreach (var e in entries)
        {
            table.Add(new Dictionary<string, string>
            {
                ["序号"] = seq.ToString(),
                ["设备类型"] = e.DeviceType,
                ["型号/规格"] = e.Specification,
                ["数量"] = e.Count.ToString(),
                ["单位"] = e.Unit,
                ["备注"] = e.Remark
            });
            seq++;
        }
        return table;
    }

    private static string ParseSpec(string deviceType, string paramJson)
    {
        using var doc = JsonDocument.Parse(paramJson);
        var root = doc.RootElement;

        return deviceType switch
        {
            "货架排" => $"L{GetInt(root, "L")}×D{GetInt(root, "D")}×H{GetInt(root, "H")}-{GetInt(root, "NLayer")}层",
            "滚筒输送线" => $"W{GetInt(root, "CW")}×H{GetInt(root, "CH")}",
            "堆垛机" => $"H{GetInt(root, "STK_H")}-载重{GetInt(root, "STK_Load")}kg",
            "主控制柜" => $"{GetInt(root, "Width")}×{GetInt(root, "Depth")}×{GetInt(root, "Height")}mm",
            "安全围栏段" => $"H{GetInt(root, "Height")}mm",
            "扫码器" => GetString(root, "MountPosition") == "top" ? "顶装" : "侧装",
            _ => ""
        };
    }

    private static int GetInt(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var prop) && prop.TryGetInt32(out var val))
            return val;
        return 0;
    }

    private static string GetString(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var prop))
            return prop.GetString() ?? "";
        return "";
    }
}

/// <summary>
/// BOM清单条目
/// </summary>
public class BOMEntry
{
    public string DeviceType { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Unit { get; set; } = "台";
    public string Remark { get; set; } = string.Empty;
    public List<string>? Numbers { get; set; }
}
