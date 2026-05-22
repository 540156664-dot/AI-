namespace Common;

/// <summary>
/// 图层定义 — 预定义图层名称和颜色
/// </summary>
public static class LayerDefinitions
{
    public static readonly (string Name, short Color, string Description)[] Layers = new[]
    {
        ("LG-货架-外框",    (short)1, "货架外轮廓"),
        ("LG-货架-货位",    (short)8, "货位分隔线"),
        ("LG-货架-标注",    (short)3, "货架尺寸标注"),
        ("LG-输送机-轮廓",  (short)5, "输送机外轮廓"),
        ("LG-输送机-中心线",(short)1, "输送机中心线"),
        ("LG-输送机-标注",  (short)3, "输送机尺寸标注"),
        ("LG-堆垛机",       (short)4, "堆垛机图块"),
        ("LG-控制柜",       (short)6, "控制柜图块"),
        ("LG-围栏",         (short)2, "安全围栏"),
        ("LG-扫码器",       (short)7, "扫码器图块"),
        ("LG-标注",         (short)3, "通用标注"),
        ("LG-统计",         (short)9, "统计表格"),
        ("LG-编号",         (short)2, "设备编号文字"),
    };
}
