using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReTime_Testing.ViewModels
{
    public partial class TimeTopSettingViewModel : ObservableObject
    {
        private static readonly List<int> _hours = Enumerable.Range(0, 24).ToList();
        private static readonly List<int> _minutes = Enumerable.Range(0, 60).ToList();

        private DispatcherTimer? _timer;
        private readonly GlobalTimeTopDesktopService _service;
        private readonly MutexManager _mutexManager;

        [ObservableProperty]
        private double _progressValue = 50;

        [ObservableProperty]
        private int _startHour = 9;

        [ObservableProperty]
        private int _startMinute = 0;

        [ObservableProperty]
        private int _endHour = 17;

        [ObservableProperty]
        private int _endMinute = 0;

        public List<int> Hours => _hours;
        public List<int> Minutes => _minutes;

        [ObservableProperty]
        private double _timerProgress = 0;

        [ObservableProperty]
        private string _timerStatus = "未开始";

        [ObservableProperty]
        private bool _isStateControlsEnabled = true;

        [ObservableProperty]
        private bool _isMutexAcquired = false;

        [ObservableProperty]
        private string _mutexId = string.Empty;

        [ObservableProperty]
        private bool _isMutexEnabled = true;

        [ObservableProperty]
        private string _mutexStatus = "未知";

        [ObservableProperty]
        private ProgressBarPosition _currentPosition = ProgressBarPosition.Top;

        [ObservableProperty]
        private string _positionText = "顶部";

        public TimeTopSettingViewModel()
        {
            _service = GlobalTimeTopDesktopService.Instance;
            _mutexManager = MutexManager.Instance;

            // 初始化互斥锁状态
            UpdateMutexStatus();

            // 初始化进度条位置
            UpdatePositionStatus();
        }

        partial void OnProgressValueChanged(double value)
        {
            _service.SetValue(value);
        }

        [RelayCommand]
        private void SetLoading()
        {
            _service.SetLoading();
        }

        [RelayCommand]
        private void SetSuccess()
        {
            _service.SetSuccess();
        }

        [RelayCommand]
        private void SetError()
        {
            _service.SetError();
        }

        [RelayCommand]
        private void SetPaused()
        {
            _service.SetPaused();
        }

        [RelayCommand]
        private void SetProgress()
        {
            _service.SetProgress(ProgressValue);
        }

        [RelayCommand]
        private void StartTimer()
        {
            // 检查 Timer 是否已运行
            if (_timer != null)
            {
                TimerStatus = "错误：计时器已在运行中";
                return;
            }

            var startTime = new TimeSpan(StartHour, StartMinute, 0);
            var endTime = new TimeSpan(EndHour, EndMinute, 0);

            // 验证时间：如果开始时间等于结束时间，则无意义
            if (startTime == endTime)
            {
                TimerStatus = "错误：开始时间不能等于结束时间";
                return;
            }

            // 验证时间跨度（跨天时计算总时长）
            var duration = endTime > startTime ? endTime - startTime : endTime + TimeSpan.FromHours(24) - startTime;
            if (duration.TotalHours > 8)
            {
                TimerStatus = "错误：时间跨度不能超过8小时";
                return;
            }

            IsStateControlsEnabled = false;
            TimerProgress = 0;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            TimerStatus = "运行中...";
        }

        [RelayCommand]
        private void StopTimer()
        {
            _timer?.Stop();
            _timer = null;
            TimerProgress = 0;
            TimerStatus = "已停止";
            IsStateControlsEnabled = true;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            var now = DateTime.Now.TimeOfDay;
            var nowTime = now.TotalSeconds;
            var start = new TimeSpan(StartHour, StartMinute, 0).TotalSeconds;
            var end = new TimeSpan(EndHour, EndMinute, 0).TotalSeconds;

            // 跨天处理：如果结束时间小于开始时间，说明跨天
            if (end < start)
            {
                end += 24 * 60 * 60;  // 加上24小时
                if (nowTime < start)
                {
                    nowTime += 24 * 60 * 60;  // 当前时间也在跨天后
                }
            }

            if (nowTime < start)
            {
                // 未到开始时间：Loading 状态
                _service.SetLoading();
                TimerProgress = 0;
                TimerStatus = "等待开始...";
            }
            else if (nowTime >= end)
            {
                // 已到期：绿色 Loading 状态
                _service.SetLoading();
                _service.SetForeground(ProgressColors.SuccessGreen);
                TimerProgress = 100;
                TimerStatus = "已完成";
            }
            else
            {
                // 在时间段内：按进度前进
                var totalDuration = end - start;
                var elapsed = nowTime - start;
                var progress = (elapsed / totalDuration) * 100;

                _service.SetProgress(progress);
                _service.SetForeground(ProgressColors.DefaultBlue);
                TimerProgress = progress;
                TimerStatus = "进行中...";
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
                _timer = null;
            }
        }

        /// <summary>
        /// 更新互斥锁状态
        /// </summary>
        private void UpdateMutexStatus()
        {
            IsMutexAcquired = _mutexManager.IsAcquired;
            MutexId = _mutexManager.Config.MutexId;
            IsMutexEnabled = _mutexManager.Config.IsEnabled;
            MutexStatus = IsMutexAcquired ? "已获取" : "未获取";
        }

        /// <summary>
        /// 释放互斥锁
        /// </summary>
        [RelayCommand]
        private void ReleaseMutex()
        {
            try
            {
                _mutexManager.Release();
                UpdateMutexStatus();
                Logger.Info("TimeTopSettingViewModel", "互斥锁已释放");
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "释放互斥锁时发生异常", ex);
            }
        }

        /// <summary>
        /// 重新获取互斥锁
        /// </summary>
        [RelayCommand]
        private void ReacquireMutex()
        {
            try
            {
                bool acquired = _mutexManager.TryAcquire();
                UpdateMutexStatus();

                if (acquired)
                {
                    Logger.Info("TimeTopSettingViewModel", "互斥锁重新获取成功");
                }
                else
                {
                    Logger.Warn("TimeTopSettingViewModel", "互斥锁重新获取失败");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "重新获取互斥锁时发生异常", ex);
            }
        }

        /// <summary>
        /// 切换互斥锁启用状态
        /// </summary>
        [RelayCommand]
        private void ToggleMutexEnabled()
        {
            try
            {
                var config = _mutexManager.Config;
                config.IsEnabled = !config.IsEnabled;
                IsMutexEnabled = config.IsEnabled;

                Logger.Info("TimeTopSettingViewModel", $"互斥锁已{(config.IsEnabled ? "启用" : "禁用")}");
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "切换互斥锁启用状态时发生异常", ex);
            }
        }

        // ==================== GlobalTimeTopDesktopService API 调试命令 ====================

        /// <summary>
        /// 设置为隐藏状态
        /// </summary>
        [RelayCommand]
        private void SetHidden()
        {
            _service.SetHidden();
        }

        /// <summary>
        /// 设置为禁用状态
        /// </summary>
        [RelayCommand]
        private void SetDisabled()
        {
            _service.SetDisabled();
        }

        /// <summary>
        /// 设置可见性为 Visible
        /// </summary>
        [RelayCommand]
        private void SetVisibilityVisible()
        {
            _service.SetVisibility(Visibility.Visible);
        }

        /// <summary>
        /// 设置可见性为 Hidden
        /// </summary>
        [RelayCommand]
        private void SetVisibilityHidden()
        {
            _service.SetVisibility(Visibility.Hidden);
        }

        /// <summary>
        /// 设置可见性为 Collapsed
        /// </summary>
        [RelayCommand]
        private void SetVisibilityCollapsed()
        {
            _service.SetVisibility(Visibility.Collapsed);
        }

        /// <summary>
        /// 设置启用状态为 True
        /// </summary>
        [RelayCommand]
        private void SetEnabledTrue()
        {
            _service.SetEnabled(true);
        }

        /// <summary>
        /// 设置启用状态为 False
        /// </summary>
        [RelayCommand]
        private void SetEnabledFalse()
        {
            _service.SetEnabled(false);
        }

        /// <summary>
        /// 设置透明度为 1.0
        /// </summary>
        [RelayCommand]
        private void SetOpacityFull()
        {
            _service.SetOpacity(1.0);
        }

        /// <summary>
        /// 设置透明度为 0.5
        /// </summary>
        [RelayCommand]
        private void SetOpacityHalf()
        {
            _service.SetOpacity(0.5);
        }

        /// <summary>
        /// 设置透明度为 0.2
        /// </summary>
        [RelayCommand]
        private void SetOpacityLow()
        {
            _service.SetOpacity(0.2);
        }

        /// <summary>
        /// 设置前景色为蓝色
        /// </summary>
        [RelayCommand]
        private void SetForegroundBlue()
        {
            _service.SetForeground(ProgressColors.DefaultBlue);
        }

        /// <summary>
        /// 设置前景色为绿色
        /// </summary>
        [RelayCommand]
        private void SetForegroundGreen()
        {
            _service.SetForeground(ProgressColors.SuccessGreen);
        }

        /// <summary>
        /// 设置前景色为红色
        /// </summary>
        [RelayCommand]
        private void SetForegroundRed()
        {
            _service.SetForeground(ProgressColors.ErrorRed);
        }

        /// <summary>
        /// 设置前景色为橙色
        /// </summary>
        [RelayCommand]
        private void SetForegroundOrange()
        {
            _service.SetForeground(ProgressColors.PauseOrange);
        }

        /// <summary>
        /// 设置前景色为灰色
        /// </summary>
        [RelayCommand]
        private void SetForegroundGray()
        {
            _service.SetForeground(ProgressColors.Gray);
        }

        /// <summary>
        /// 设置背景色为透明
        /// </summary>
        [RelayCommand]
        private void SetBackgroundTransparent()
        {
            _service.SetBackground(Brushes.Transparent);
        }

        /// <summary>
        /// 设置背景色为浅灰色
        /// </summary>
        [RelayCommand]
        private void SetBackgroundLightGray()
        {
            _service.SetBackground(Brushes.LightGray);
        }

        /// <summary>
        /// 设置背景色为白色
        /// </summary>
        [RelayCommand]
        private void SetBackgroundWhite()
        {
            _service.SetBackground(Brushes.White);
        }

        /// <summary>
        /// 设置范围为 0-100
        /// </summary>
        [RelayCommand]
        private void SetRange0100()
        {
            _service.SetRange(0, 100);
        }

        /// <summary>
        /// 设置范围为 0-1
        /// </summary>
        [RelayCommand]
        private void SetRange01()
        {
            _service.SetRange(0, 1);
        }

        /// <summary>
        /// 重置为默认状态
        /// </summary>
        [RelayCommand]
        private void ResetState()
        {
            _service.Reset();
        }

        /// <summary>
        /// 批量更新测试
        /// </summary>
        [RelayCommand]
        private void BatchUpdateTest()
        {
            _service.BatchUpdate(svc =>
            {
                svc.SetProgress(75);
                svc.SetForeground(ProgressColors.SuccessGreen);
                svc.SetOpacity(0.8);
                svc.SetVisibility(Visibility.Visible);
            });
        }

        // ==================== 进度条位置控制 ====================

        /// <summary>
        /// 更新位置状态
        /// </summary>
        private void UpdatePositionStatus()
        {
            var manager = DesktopWindowManager.Instance;
            CurrentPosition = manager.CurrentPosition;
            PositionText = GetPositionText(CurrentPosition);
        }

        /// <summary>
        /// 获取位置文本
        /// </summary>
        private string GetPositionText(ProgressBarPosition position)
        {
            return position switch
            {
                ProgressBarPosition.Top => "顶部",
                ProgressBarPosition.Bottom => "底部",
                ProgressBarPosition.Left => "左侧",
                ProgressBarPosition.Right => "右侧",
                _ => "未知"
            };
        }

        /// <summary>
        /// 切换到顶部位置
        /// </summary>
        [RelayCommand]
        private void SetPositionTop()
        {
            try
            {
                DesktopWindowManager.Instance.SetPosition(ProgressBarPosition.Top);
                UpdatePositionStatus();
                Logger.Info("TimeTopSettingViewModel", "进度条位置已切换到顶部");
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "切换进度条位置到顶部时发生异常", ex);
            }
        }

        /// <summary>
        /// 切换到底部位置
        /// </summary>
        [RelayCommand]
        private void SetPositionBottom()
        {
            try
            {
                DesktopWindowManager.Instance.SetPosition(ProgressBarPosition.Bottom);
                UpdatePositionStatus();
                Logger.Info("TimeTopSettingViewModel", "进度条位置已切换到底部");
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "切换进度条位置到底部时发生异常", ex);
            }
        }

        /// <summary>
        /// 切换到左侧位置
        /// </summary>
        [RelayCommand]
        private void SetPositionLeft()
        {
            try
            {
                DesktopWindowManager.Instance.SetPosition(ProgressBarPosition.Left);
                UpdatePositionStatus();
                Logger.Info("TimeTopSettingViewModel", "进度条位置已切换到左侧");
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "切换进度条位置到左侧时发生异常", ex);
            }
        }

        /// <summary>
        /// 切换到右侧位置
        /// </summary>
        [RelayCommand]
        private void SetPositionRight()
        {
            try
            {
                DesktopWindowManager.Instance.SetPosition(ProgressBarPosition.Right);
                UpdatePositionStatus();
                Logger.Info("TimeTopSettingViewModel", "进度条位置已切换到右侧");
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "切换进度条位置到右侧时发生异常", ex);
            }
        }
    }
}