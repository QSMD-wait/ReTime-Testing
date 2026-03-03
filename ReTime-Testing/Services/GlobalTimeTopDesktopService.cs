using ReTime_Testing.Models;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// TimeTop 桌面进度条全局服务（单例）
    /// </summary>
    public class GlobalTimeTopDesktopService
    {
        private static readonly Lazy<GlobalTimeTopDesktopService> _instance = 
            new Lazy<GlobalTimeTopDesktopService>(() => new GlobalTimeTopDesktopService());
        
        /// <summary>
        /// 获取全局唯一实例
        /// </summary>
        public static GlobalTimeTopDesktopService Instance => _instance.Value;
        
        private readonly ProgressStateManager _stateManager;
        private DispatcherTimer? _timer;
        private int _startHour = 9;
        private int _startMinute = 0;
        private int _endHour = 17;
        private int _endMinute = 0;
        
        /// <summary>
        /// 定时器进度（用于 UI 显示）
        /// </summary>
        public double TimerProgress { get; private set; }
        
        /// <summary>
        /// 定时器状态（用于 UI 显示）
        /// </summary>
        public string TimerStatus { get; private set; } = "未开始";
        
        /// <summary>
        /// 调度是否正在运行
        /// </summary>
        public bool IsScheduleRunning => _timer != null;
        
        /// <summary>
        /// 调度进度（用于 UI 显示）
        /// </summary>
        public double ScheduleProgress { get; private set; }
        
        /// <summary>
        /// 调度状态（用于 UI 显示）
        /// </summary>
        public string ScheduleStatus { get; private set; } = "未开始";
        
        /// <summary>
        /// 调度状态变更事件
        /// </summary>
        public event Action<double, string>? OnScheduleStateChanged;
        
        private GlobalTimeTopDesktopService()
        {
            _stateManager = new ProgressStateManager();
        }
        
        // ==================== 状态设置 ====================
        
        /// <summary>
        /// 设置为加载状态（蓝色不确定动画）
        /// </summary>
        public void SetLoading()
        {
            Logger.Info("ReTime_Testing.Services.GlobalTimeTopDesktopService", "设置状态: Loading");
            _stateManager.SetState(ProgressStateManager.ProgressStates.Loading);
        }
        
        /// <summary>
        /// 设置为进度状态（蓝色确定进度）
        /// </summary>
        public void SetProgress(double value)
        {
            Logger.Info("ReTime_Testing.Services.GlobalTimeTopDesktopService", $"设置进度: {value:F1}%");
            _stateManager.SetState(ProgressStateManager.ProgressStates.Progress);
            _stateManager.SetValue(value);
        }
        
        /// <summary>
        /// 设置为成功状态（绿色完成）
        /// </summary>
        public void SetSuccess()
        {
            Logger.Info("ReTime_Testing.Services.GlobalTimeTopDesktopService", "设置状态: Success");
            _stateManager.SetState(ProgressStateManager.ProgressStates.Success);
        }
        
        /// <summary>
        /// 设置为错误状态（红色）
        /// </summary>
        public void SetError()
        {
            Logger.Info("ReTime_Testing.Services.GlobalTimeTopDesktopService", "设置状态: Error");
            _stateManager.SetState(ProgressStateManager.ProgressStates.Error);
        }
        
        /// <summary>
        /// 设置为暂停状态（橙色）
        /// </summary>
        public void SetPaused()
        {
            Logger.Info("ReTime_Testing.Services.GlobalTimeTopDesktopService", "设置状态: Paused");
            _stateManager.SetState(ProgressStateManager.ProgressStates.Paused);
        }
        
        /// <summary>
        /// 设置为隐藏状态
        /// </summary>
        public void SetHidden()
        {
            Logger.Info("ReTime_Testing.Services.GlobalTimeTopDesktopService", "设置状态: Hidden");
            _stateManager.SetState(ProgressStateManager.ProgressStates.Hidden);
        }
        
        /// <summary>
        /// 设置为禁用状态
        /// </summary>
        public void SetDisabled()
        {
            Logger.Info("ReTime_Testing.Services.GlobalTimeTopDesktopService", "设置状态: Disabled");
            _stateManager.SetState(ProgressStateManager.ProgressStates.Disabled);
        }
        
        // ==================== 属性控制 ====================
        
        /// <summary>
        /// 设置进度值
        /// </summary>
        public void SetValue(double value) => _stateManager.SetValue(value);
        
        /// <summary>
        /// 设置前景色
        /// </summary>
        public void SetForeground(Brush foreground) => _stateManager.SetForeground(foreground);
        
        /// <summary>
        /// 设置背景色
        /// </summary>
        public void SetBackground(Brush background) => _stateManager.SetBackground(background);
        
        /// <summary>
        /// 设置透明度
        /// </summary>
        public void SetOpacity(double opacity) => _stateManager.SetOpacity(opacity);
        
        /// <summary>
        /// 设置可见性
        /// </summary>
        public void SetVisibility(Visibility visibility) => _stateManager.SetVisibility(visibility);
        
        /// <summary>
        /// 设置启用状态
        /// </summary>
        public void SetEnabled(bool isEnabled) => _stateManager.SetEnabled(isEnabled);
        
        /// <summary>
        /// 设置进度范围
        /// </summary>
        public void SetRange(double minimum, double maximum) => _stateManager.SetRange(minimum, maximum);
        
        // ==================== 状态变更回调 ====================
        
        /// <summary>
        /// 状态变更回调（用于 ViewModel 更新 UI）
        /// </summary>
        public Action<ProgressStateConfig>? OnStateChanged
        {
            get => _stateManager.OnStateChanged;
            set => _stateManager.OnStateChanged = value;
        }
        
        // ==================== 批量更新 ====================
        
        /// <summary>
        /// 开始批量更新（期间不会触发回调）
        /// </summary>
        public GlobalTimeTopDesktopService BeginBatchUpdate()
        {
            _stateManager.BeginBatchUpdate();
            return this;
        }
        
        /// <summary>
        /// 结束批量更新（触发一次回调）
        /// </summary>
        public GlobalTimeTopDesktopService EndBatchUpdate()
        {
            _stateManager.EndBatchUpdate();
            return this;
        }
        
        /// <summary>
        /// 批量更新操作
        /// </summary>
        public void BatchUpdate(Action<GlobalTimeTopDesktopService> action)
        {
            BeginBatchUpdate();
            try
            {
                action(this);
            }
            finally
            {
                EndBatchUpdate();
            }
        }
        
        // ==================== 其他 ====================
        
        /// <summary>
        /// 重置为默认状态
        /// </summary>
        public void Reset() => _stateManager.Reset();
        
        /// <summary>
        /// 获取当前配置
        /// </summary>
        public ProgressStateConfig GetCurrentConfig() => _stateManager.CurrentConfig;
        
        // ==================== 调度控制 ====================
        
        /// <summary>
        /// 设置调度时间
        /// </summary>
        /// <param name="startHour">开始小时</param>
        /// <param name="startMinute">开始分钟</param>
        /// <param name="endHour">结束小时</param>
        /// <param name="endMinute">结束分钟</param>
        public void SetScheduleTime(int startHour, int startMinute, int endHour, int endMinute)
        {
            _startHour = startHour;
            _startMinute = startMinute;
            _endHour = endHour;
            _endMinute = endMinute;
            Logger.Info("ReTime_Testing.Services.GlobalTimeTopDesktopService", 
                $"调度时间已设置: {startHour:D2}:{startMinute:D2} - {endHour:D2}:{endMinute:D2}");
        }
        
        /// <summary>
        /// 启动调度
        /// </summary>
        /// <param name="startHour">开始小时</param>
        /// <param name="startMinute">开始分钟</param>
        /// <param name="endHour">结束小时</param>
        /// <param name="endMinute">结束分钟</param>
        /// <returns>是否成功启动</returns>
        public bool StartSchedule(int startHour, int startMinute, int endHour, int endMinute)
        {
            // 检查调度是否已运行
            if (_timer != null)
            {
                Logger.Warn("ReTime_Testing.Services.GlobalTimeTopDesktopService", "调度已在运行中");
                return false;
            }

            var startTime = new TimeSpan(startHour, startMinute, 0);
            var endTime = new TimeSpan(endHour, endMinute, 0);

            // 验证时间：如果开始时间等于结束时间，则无意义
            if (startTime == endTime)
            {
                Logger.Warn("ReTime_Testing.Services.GlobalTimeTopDesktopService", "开始时间不能等于结束时间");
                return false;
            }

            // 验证时间跨度（跨天时计算总时长）
            var duration = endTime > startTime ? endTime - startTime : endTime + TimeSpan.FromHours(24) - startTime;
            if (duration.TotalHours > 8)
            {
                Logger.Warn("ReTime_Testing.Services.GlobalTimeTopDesktopService", "时间跨度不能超过8小时");
                return false;
            }

            // 设置调度时间
            SetScheduleTime(startHour, startMinute, endHour, endMinute);

            // 使用 Application.Current.Dispatcher 创建定时器，确保与应用程序生命周期绑定
            _timer = new DispatcherTimer 
            { 
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnScheduleTick;
            _timer.Start();

            ScheduleProgress = 0;
            ScheduleStatus = "运行中...";
            NotifyScheduleStateChanged();

            Logger.Info("ReTime_Testing.Services.GlobalTimeTopDesktopService", "调度已启动");
            return true;
        }
        
        /// <summary>
        /// 停止调度
        /// </summary>
        public void StopSchedule()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnScheduleTick;
                _timer = null;
            }

            ScheduleProgress = 0;
            ScheduleStatus = "已停止";
            NotifyScheduleStateChanged();

            Logger.Info("ReTime_Testing.Services.GlobalTimeTopDesktopService", "调度已停止");
        }
        
        /// <summary>
        /// 调度定时器回调
        /// </summary>
        private void OnScheduleTick(object? sender, EventArgs e)
        {
            var now = DateTime.Now.TimeOfDay;
            var nowTime = now.TotalSeconds;
            var start = new TimeSpan(_startHour, _startMinute, 0).TotalSeconds;
            var end = new TimeSpan(_endHour, _endMinute, 0).TotalSeconds;

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
                SetLoading();
                ScheduleProgress = 0;
                ScheduleStatus = "等待开始...";
            }
            else if (nowTime >= end)
            {
                // 已到期：绿色 Loading 状态
                SetLoading();
                SetForeground(ProgressColors.SuccessGreen);
                ScheduleProgress = 100;
                ScheduleStatus = "已完成";
            }
            else
            {
                // 在时间段内：按进度前进
                var totalDuration = end - start;
                var elapsed = nowTime - start;
                var progress = (elapsed / totalDuration) * 100;

                SetProgress(progress);
                SetForeground(ProgressColors.DefaultBlue);
                ScheduleProgress = progress;
                ScheduleStatus = "进行中...";
            }

            NotifyScheduleStateChanged();
        }
        
        /// <summary>
        /// 通知调度状态变更
        /// </summary>
        private void NotifyScheduleStateChanged()
        {
            OnScheduleStateChanged?.Invoke(ScheduleProgress, ScheduleStatus);
        }
    }
}