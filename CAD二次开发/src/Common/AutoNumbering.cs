namespace Common;

/// <summary>
/// 自动编号引擎 — 按设备类型自动递增编号，支持重复检测
/// </summary>
public class AutoNumbering
{
    /// <summary>
    /// 编号规则配置
    /// </summary>
    public static readonly Dictionary<string, NumberingRule> Rules = new()
    {
        ["货架排"]     = new("RK", 2),
        ["滚筒输送线"] = new("L", 3),
        ["堆垛机"]     = new("SRM", 2),
        ["主控制柜"]   = new("CAB", 2),
        ["安全围栏段"] = new("SF", 3),
        ["扫码器"]     = new("SC", 2),
    };

    /// <summary>
    /// 获取指定设备类型的下一个可用编号
    /// </summary>
    public string GetNextNumber(string deviceType, HashSet<string> existingNumbers)
    {
        if (!Rules.TryGetValue(deviceType, out var rule))
            return $"{deviceType}-??";

        int seq = 1;
        while (true)
        {
            var candidate = FormatNumber(rule.Prefix, seq, rule.Digits);
            if (!existingNumbers.Contains(candidate))
                return candidate;
            seq++;
        }
    }

    /// <summary>
    /// 校验编号是否与已有编号重复
    /// </summary>
    public bool IsDuplicate(string number, HashSet<string> existingNumbers)
        => existingNumbers.Contains(number);

    /// <summary>
    /// 格式化编号（前缀+补零序号）
    /// </summary>
    public static string FormatNumber(string prefix, int sequence, int digits)
        => $"{prefix}{sequence.ToString().PadLeft(digits, '0')}";

    /// <summary>
    /// 从整个图纸已有编号中获取下一个编号
    /// </summary>
    public string GetNextNumber(string deviceType, Func<IEnumerable<string>> getAllExistingNumbers)
    {
        var existing = new HashSet<string>(getAllExistingNumbers());
        return GetNextNumber(deviceType, existing);
    }
}

/// <summary>
/// 编号规则
/// </summary>
public class NumberingRule
{
    public string Prefix { get; }
    public int Digits { get; }
    public NumberingRule(string prefix, int digits)
    {
        Prefix = prefix;
        Digits = digits;
    }
}
