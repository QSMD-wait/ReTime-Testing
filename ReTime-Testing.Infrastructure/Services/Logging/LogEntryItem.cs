using System;
using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 内存日志条目（供日志查看器使用）
    /// </summary>
    public sealed record LogEntryItem(DateTimeOffset Timestamp, LogLevel Level, string Module, string Message);
}
