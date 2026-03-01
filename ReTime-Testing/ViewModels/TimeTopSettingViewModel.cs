using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace ReTime_Testing.ViewModels
{
    public partial class TimeTopSettingViewModel : ObservableObject
    {
        private DispatcherTimer? _timer;
        private readonly GlobalTimeTopDesktopService _service;

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

        public List<int> Hours { get; } = new List<int>();
        public List<int> Minutes { get; } = new List<int>();

        [ObservableProperty]
        private double _timerProgress = 0;

        [ObservableProperty]
        private string _timerStatus = "未开始";

        [ObservableProperty]
        private bool _isStateControlsEnabled = true;

        public TimeTopSettingViewModel()
        {
            _service = GlobalTimeTopDesktopService.Instance;

            // 初始化小时和分钟列表
            for (int i = 0; i < 24; i++) Hours.Add(i);
            for (int i = 0; i < 60; i++) Minutes.Add(i);
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
            var startTime = new TimeSpan(StartHour, StartMinute, 0);
            var endTime = new TimeSpan(EndHour, EndMinute, 0);

            // 验证时间
            if (endTime <= startTime)
            {
                if ((endTime - startTime).TotalHours > 8)
                {
                    TimerStatus = "错误：时间跨度不能超过8小时";
                    return;
                }
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
            _timer?.Stop();
            _timer = null;
        }
    }
}