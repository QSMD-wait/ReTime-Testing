using System;
using System.Diagnostics;
using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 日志记录器
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <param name="module">模块名称（完整命名空间）</param>
        /// <param name="message">日志信息</param>
        public static void Log(LogLevel level, string module, string message)
        {
            var now = DateTime.Now;
            var date = now.ToString("yyyy-MM-dd");
            var time = now.ToString("HH:mm:ss");

            var logMessage = $"[{date} {time}][{level}][{module}] {message}";

            switch (level)
            {
                case LogLevel.INF:
                    Debug.WriteLine(logMessage);
                    break;
                case LogLevel.WRN:
                    Debug.WriteLine(logMessage);
                    break;
                case LogLevel.ERR:
                    Debug.WriteLine(logMessage);
                    Debug.Fail(logMessage);
                    break;
            }
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void Info(string module, string message)
        {
            Log(LogLevel.INF, module, message);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public static void Warn(string module, string message)
        {
            Log(LogLevel.WRN, module, message);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void Error(string module, string message)
        {
            Log(LogLevel.ERR, module, message);
        }

        /// <summary>
        /// 记录错误日志（带异常）
        /// </summary>
        public static void Error(string module, string message, Exception exception)
        {
            var fullMessage = $"{message}\n异常类型: {exception.GetType().Name}\n异常信息: {exception.Message}\n堆栈跟踪: {exception.StackTrace}";
            Log(LogLevel.ERR, module, fullMessage);
        }
    }
}