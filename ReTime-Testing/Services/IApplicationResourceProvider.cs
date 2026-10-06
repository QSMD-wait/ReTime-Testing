using System.Collections.Generic;
using System.Windows;

namespace ReTime_Testing.Services;

/// <summary>
/// 应用资源提供者：向主题/样式服务暴露应用级合并资源字典
/// </summary>
public interface IApplicationResourceProvider
{
    /// <summary>获取当前应用（App.Resources）已合并的资源字典列表</summary>
    IList<ResourceDictionary> GetMergedDictionaries();
}
