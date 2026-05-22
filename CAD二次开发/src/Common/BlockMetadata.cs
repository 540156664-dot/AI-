namespace Common;

/// <summary>
/// 设备块元数据 — 存储在CAD图块的扩展数据中，用于图块识别和参数回读
/// </summary>
public class BlockMetadata
{
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceNumber { get; set; } = string.Empty;
    public string ParameterJson { get; set; } = "{}";
}
