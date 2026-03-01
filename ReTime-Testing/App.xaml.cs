using System;
using System.Configuration;
using System.Data;
using System.Media;
using System.Windows;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using ReTime_Testing.Views;

namespace ReTime_Testing
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private MutexManager? _mutexManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 初始化互斥锁管理器
            _mutexManager = MutexManager.Instance;

            // 订阅互斥锁事件
            _mutexManager.OnConflictDetected += OnMutexConflictDetected;
            _mutexManager.OnMutexAcquired += OnMutexAcquired;

            // 尝试获取互斥锁
            bool mutexAcquired = _mutexManager.TryAcquire();

            // 如果未获取到互斥锁，则启动冲突处理流程
            if (!mutexAcquired)
            {
                HandleMutexConflict();
                return;
            }

            // 互斥锁获取成功，正常启动应用
            StartupApplication();
        }

        /// <summary>
        /// 启动应用程序主窗口
        /// </summary>
        private void StartupApplication()
        {
            try
            {
                // 初始化全局服务
                var service = GlobalTimeTopDesktopService.Instance;

                // 打开主窗口
                var mainWindow = new MainWindow();
                mainWindow.Show();

                // 打开 TimeTopDesktop 窗口
                var timeTopDesktop = new TimeTopDesktop();
                timeTopDesktop.Show();

                // 打开 TimeTopSetting 窗口
                var timeTopSetting = new TimeTopSetting();
                timeTopSetting.Show();

                Logger.Info(GetType().FullName ?? "App", "应用程序启动成功");
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "App", "启动应用程序时发生异常", ex);
                Shutdown();
            }
        }

        /// <summary>
        /// 处理互斥锁冲突
        /// </summary>
        private void HandleMutexConflict()
        {
            var config = _mutexManager?.Config;

            if (config == null)
            {
                Logger.Error(GetType().FullName ?? "App", "互斥锁管理器配置为 null");
                Shutdown();
                return;
            }

            // 显示冲突警告弹窗（使用 Modern UI 风格）
            ShowModernConflictDialog(config);

            Logger.Warn(GetType().FullName ?? "App", "检测到多实例运行，显示冲突弹窗");

            // 根据配置决定是否自动关闭应用程序
            if (config.AutoShutdownOnConflict)
            {
                Shutdown();
            }
        }

        /// <summary>
        /// 显示 Modern UI 风格的冲突对话框
        /// </summary>
        private void ShowModernConflictDialog(MutexConfig config)
        {
            try
            {
                // 使用 iNKORE.Modern 的 MessageBox.Show API
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    config.ConflictWindowMessage,
                    config.ConflictWindowTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning,
                    MessageBoxResult.OK,
                    config.PlaySound ? SystemSounds.Beep : null
                );
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "App", "显示 Modern UI 对话框时发生异常，回退到标准 MessageBox", ex);

                // 回退到标准 MessageBox
                MessageBox.Show(
                    config.ConflictWindowMessage,
                    config.ConflictWindowTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        /// <summary>
        /// 互斥锁冲突事件处理
        /// </summary>
        private void OnMutexConflictDetected(object? sender, MutexConflictEventArgs e)
        {
            Logger.Warn(GetType().FullName ?? "App", $"互斥锁冲突事件触发，冲突时间: {e.ConflictTime}");
        }

        /// <summary>
        /// 互斥锁获取成功事件处理
        /// </summary>
        private void OnMutexAcquired(object? sender, EventArgs e)
        {
            Logger.Info(GetType().FullName ?? "App", "互斥锁获取成功事件触发");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 释放互斥锁
            _mutexManager?.Release();

            // 取消事件订阅
            if (_mutexManager != null)
            {
                _mutexManager.OnConflictDetected -= OnMutexConflictDetected;
                _mutexManager.OnMutexAcquired -= OnMutexAcquired;
            }

            Logger.Info(GetType().FullName ?? "App", "应用程序退出");

            base.OnExit(e);
        }
    }
}
