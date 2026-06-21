using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 自启动服务接口
    /// </summary>
    public interface IAutoStartService
    {
        /// <summary>
        /// 获取当前自启动是否启用
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 获取当前自启动方式
        /// </summary>
        string Method { get; }

        /// <summary>
        /// 从配置初始化状态
        /// </summary>
        void InitializeFromConfig(AutoStartConfig config);

        /// <summary>
        /// 启用自启动
        /// </summary>
        /// <param name="method">自启动方式: registry, startupFolder</param>
        void Enable(string method);

        /// <summary>
        /// 禁用自启动
        /// </summary>
        void Disable();
    }
}
