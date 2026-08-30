using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models.UI;
using ReTime_Testing.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;

namespace ReTime_Testing.ViewModels.Testing
{
    /// <summary>
    /// 控件测试 ViewModel
    /// 职责：自研控件（Toast 等）的各种参数与行为测试
    /// </summary>
    public partial class ControlsViewModel : ObservableObject
    {

        public ControlsViewModel(ILogger<ControlsViewModel> logger)
        {
            _logger = logger;
        }

        private readonly ILogger<ControlsViewModel> _logger;
        public string TabTitle => "控件";

        [ObservableProperty]
        private int _selectedToastSeverityIndex;

        [ObservableProperty]
        private string _toastTitle = "测试标题";

        [ObservableProperty]
        private string _toastMessage = "这是一条测试 Toast 通知消息";

        [ObservableProperty]
        private double _toastDurationSeconds = 5;

        [ObservableProperty]
        private bool _toastAutoClose = true;

        [ObservableProperty]
        private bool _toastCanUserClose = true;

        public List<string> ToastSeverityNames { get; } = Enum.GetNames<ToastSeverity>().ToList();

        /// <summary>
        /// Toast 显示请求事件（由窗口代码后置处理）
        /// </summary>
        public event Action<ToastMessage>? ToastRequested;

        // ==================== Toast 测试命令 ====================

        private ToastSeverity GetSelectedSeverity()
        {
            return (ToastSeverity)SelectedToastSeverityIndex;
        }

        [RelayCommand]
        private void ShowCustomToast()
        {
            var message = new ToastMessage(ToastTitle, ToastMessage)
            {
                Severity = GetSelectedSeverity(),
                Duration = TimeSpan.FromSeconds(ToastDurationSeconds),
                AutoClose = ToastAutoClose,
                CanUserClose = ToastCanUserClose
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowInfoToast()
        {
            var message = new ToastMessage("信息通知", "这是一条信息级别的 Toast 通知")
            {
                Severity = ToastSeverity.Informational
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowSuccessToastTest()
        {
            var message = new ToastMessage("操作成功", "任务已成功完成！")
            {
                Severity = ToastSeverity.Success
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowWarningToastTest()
        {
            var message = new ToastMessage("警告", "检测到潜在问题，请注意检查")
            {
                Severity = ToastSeverity.Warning,
                Duration = TimeSpan.FromSeconds(7)
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowErrorToastTest()
        {
            var message = new ToastMessage("错误", "操作执行失败，请重试或联系管理员")
            {
                Severity = ToastSeverity.Error,
                Duration = TimeSpan.FromSeconds(10)
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowNonClosableToast()
        {
            var message = new ToastMessage("不可关闭", "此 Toast 不会自动关闭，只能通过代码关闭")
            {
                Severity = ToastSeverity.Warning,
                AutoClose = false,
                CanUserClose = false,
                Duration = TimeSpan.MaxValue
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowBurstToast()
        {
            var severities = new[] { ToastSeverity.Informational, ToastSeverity.Success, ToastSeverity.Warning, ToastSeverity.Error };
            for (int i = 0; i < 4; i++)
            {
                var message = new ToastMessage($"批量通知 #{i + 1}", $"这是第 {i + 1} 条批量 Toast")
                {
                    Severity = severities[i],
                    Duration = TimeSpan.FromSeconds(3 + i)
                };
                ToastRequested?.Invoke(message);
            }
        }

        [RelayCommand]
        private void ShowActionToast()
        {
            var message = new ToastMessage("更新可用", "新版本 v2.0 已发布，包含多项改进")
            {
                Severity = ToastSeverity.Informational,
                ActionContent = new Button
                {
                    Content = "查看详情",
                    Command = new RelayCommand(() =>
                    {
                        _logger.LogInformation("用户点击了 Toast 操作按钮：查看详情");
                    })
                }
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowErrorActionToast()
        {
            var message = new ToastMessage("保存失败", "文件被占用，无法写入配置")
            {
                Severity = ToastSeverity.Error,
                Duration = TimeSpan.FromSeconds(10),
                ActionContent = new Button
                {
                    Content = "重试",
                    Command = new RelayCommand(() =>
                    {
                        _logger.LogInformation("用户点击了重试按钮");
                    })
                }
            };
            ToastRequested?.Invoke(message);
        }
    }
}