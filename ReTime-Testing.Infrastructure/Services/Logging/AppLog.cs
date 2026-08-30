using System;
using Microsoft.Extensions.Logging;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 应用日志入口
    /// 为非 DI 管理的类型（POCO、静态上下文）提供 ILogger&lt;T&gt; 获取途径，
    /// 来源（SourceContext）自动取泛型类名，无需手写模块名
    /// </summary>
    public static class AppLog
    {
        private static Serilog.Extensions.Logging.SerilogLoggerFactory? _factory;
        private static readonly object _lock = new();

        /// <summary>
        /// 获取指定类型的日志器（来源自动为类名）
        /// </summary>
        public static ILogger<T> For<T>()
        {
            _factory ??= new Serilog.Extensions.Logging.SerilogLoggerFactory(Serilog.Log.Logger);
            return _factory.CreateLogger<T>();
        }

        /// <summary>
        /// 重置内部工厂（日志器重建后由 LoggingSetup 调用，使后续 For&lt;T&gt; 绑定新日志器）
        /// </summary>
        internal static void ResetFactory()
        {
            lock (_lock)
            {
                _factory = null;
            }
        }
    }
}
