using System;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// Toast 通知服务临时实现
    /// 通过事件机制通知 View 层显示 Toast，后续将替换为全局 Toast 栈组件
    /// </summary>
    public class EditorToastService : IToastService
    {
        public event Action<string, ToastType, int>? ToastRequested;

        public void Show(string message, ToastType type = ToastType.Info, int durationMs = 3000)
        {
            ToastRequested?.Invoke(message, type, durationMs);
        }
    }
}