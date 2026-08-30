using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace ReTime_Testing.ViewModels.Testing
{
    /// <summary>
    /// 错误模拟测试 ViewModel
    /// 职责：模拟各种异常抛出场景，测试全局错误处理与崩溃窗口
    /// </summary>
    public partial class ErrorSimulatorViewModel : ObservableObject
    {
        private readonly CrashReportService _crashService = new();

        public string TabTitle => "错误模拟";

        /// <summary>
        /// 直接打开崩溃窗口（不抛异常，传入自定义文本）
        /// </summary>
        [RelayCommand]
        private void OpenCrashWindowDirectly()
        {
            var crashInfo = _crashService.BuildCrashReport(
                new InvalidOperationException("这是一个模拟的测试异常"));
            _crashService.SaveCrashLog(crashInfo);
            _crashService.ShowCrashWindow(crashInfo, isTerminating: false);
        }

        /// <summary>
        /// 抛出 NullReferenceException（UI 线程同步）
        /// </summary>
        [RelayCommand]
        private void ThrowNullReference()
        {
            string? obj = null;
            _ = obj.Length; // 触发 NullReferenceException
        }

        /// <summary>
        /// 抛出 InvalidOperationException
        /// </summary>
        [RelayCommand]
        private void ThrowInvalidOperation()
        {
            throw new InvalidOperationException("操作无效：模拟的业务逻辑异常");
        }

        /// <summary>
        /// 抛出带内部异常的 AggregateException
        /// </summary>
        [RelayCommand]
        private void ThrowAggregateException()
        {
            var inner1 = new ArgumentException("参数 'name' 不能为空");
            var inner2 = new TimeoutException("操作超时（30秒）");
            throw new AggregateException(inner1, inner2);
        }

        /// <summary>
        /// 后台 Task 抛出异常（测试 TaskScheduler.UnobservedTaskException）
        /// </summary>
        [RelayCommand]
        private void ThrowBackgroundTaskException()
        {
            _ = Task.Run(() =>
            {
                throw new ApplicationException("后台任务中未观察的异常");
            });
        }

        /// <summary>
        /// 打开崩溃窗口（终止性异常模式，不允许忽略）
        /// </summary>
        [RelayCommand]
        private void OpenCrashWindowTerminating()
        {
            var crashInfo = _crashService.BuildCrashReport(
                new OutOfMemoryException("模拟内存不足（终止性异常）"));
            _crashService.SaveCrashLog(crashInfo);
            _crashService.ShowCrashWindow(crashInfo, isTerminating: true);
        }

        /// <summary>
        /// 仅保存崩溃日志到文件（不打开窗口）
        /// </summary>
        [RelayCommand]
        private void SaveCrashLogOnly()
        {
            var crashInfo = _crashService.BuildCrashReport(
                new StackOverflowException("模拟栈溢出（仅保存日志）"));
            var path = _crashService.SaveCrashLog(crashInfo);
            MessageBox.Show($"崩溃日志已保存到：\n{path}", "保存成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
