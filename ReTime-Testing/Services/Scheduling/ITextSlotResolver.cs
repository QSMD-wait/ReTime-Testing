using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 文字插槽解析器接口
/// 根据数据源类型和调度上下文解析出显示文本
/// </summary>
public interface ITextSlotResolver
{
    /// <summary>
    /// 解析指定数据源的文本
    /// </summary>
    /// <param name="source">数据源类型</param>
    /// <param name="sourceSettings">组件专属配置</param>
    /// <param name="commonSettings">通用自定义项</param>
    /// <returns>解析后的文本（含前缀后缀），无法解析时返回空字符串</returns>
    string Resolve(TextSourceType source, TextSlotSourceSettings? sourceSettings = null, TextSlotCommonSettings? commonSettings = null);
}
