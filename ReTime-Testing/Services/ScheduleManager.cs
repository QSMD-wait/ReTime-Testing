using ReTime_Testing.Models;
using System.Windows.Threading;

namespace ReTime_Testing.Services;

/// <summary>
/// 调度管理器
/// 负责执行计划的调度和状态管理
/// </summary>
public class ScheduleManager
{
    private readonly ITimeService _timeService;
    private readonly ProgressStateManager _stateManager;

    private ExecutionPlan? _currentPlan;
    private DispatcherTimer? _timer;
    private DateTime _currentTime;
    private ScheduleBehavior _currentBehavior = ScheduleBehavior.Default;

    /// <summary>
    /// 当前执行计划
    /// </summary>
    public ExecutionPlan? CurrentPlan => _currentPlan;

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning => _timer != null;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="timeService">时间服务</param>
    /// <param name="stateManager">状态管理器</param>
    public ScheduleManager(
        ITimeService timeService,
        ProgressStateManager stateManager)
    {
        _timeService = timeService;
        _stateManager = stateManager;

        // 订阅时间跳跃事件
        _timeService.TimeJumped += OnTimeJumped;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="plan">执行计划</param>
    public void Initialize(ExecutionPlan plan)
    {
        _currentPlan = plan;
        _currentTime = _timeService.GetCurrentTime();

        // 启动 1秒轮询
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        // 立即应用当前状态
        ApplyCurrentState();

        // 初始化时应用当前时间段的行为配置
        if (_currentPlan?.CurrentSegment != null)
        {
            ApplyBehavior(_currentPlan.CurrentSegment);
        }

        Logger.Info("ScheduleManager", $"调度管理器已初始化: {plan}");
    }

    /// <summary>
    /// 重新生成执行计划
    /// </summary>
    /// <param name="newPlan">新的执行计划</param>
    public void RegenerateExecutionPlan(ExecutionPlan newPlan)
    {
        Logger.Info("ScheduleManager", $"重新生成执行计划: {newPlan}");

        _currentPlan = newPlan;
        ApplyCurrentState();
    }

    /// <summary>
    /// 停止调度
    /// </summary>
    public void Stop()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;

            Logger.Info("ScheduleManager", "调度管理器已停止");
        }
    }

    /// <summary>
    /// 定时器回调（1秒轮询）
    /// </summary>
    private void OnTimerTick(object? sender, EventArgs e)
    {
        var currentTime = _timeService.GetCurrentTime();
        _currentTime = currentTime;

        try
        {
            // 1. 检查状态切换
            CheckStateTransition(currentTime);

            // 2. 更新进度条
            UpdateProgressBar(currentTime);

            // 3. 处理事件队列
            ProcessEventQueue(currentTime);
        }
        catch (Exception ex)
        {
            Logger.Error("ScheduleManager", $"定时器回调失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 检查状态切换
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    private void CheckStateTransition(DateTime currentTime)
    {
        if (_currentPlan == null) return;

        // 查找所有应该执行但尚未执行的时间点
        // 条件：时间点时间 <= 当前时间，且未被标记为已执行
        var lastExecutedTime = _currentPlan.LastExecutedTimePoint?.Time ?? DateTime.MinValue;
        
        var pendingTimePoints = _currentPlan.TimePoints
            .Where(tp => tp.Time <= currentTime && tp.Time > lastExecutedTime)
            .OrderBy(tp => tp.Time)
            .ToList();

        if (pendingTimePoints.Any())
        {
            Logger.Info("ScheduleManager", $"发现 {pendingTimePoints.Count} 个待执行的时间点 (当前时间: {currentTime:HH:mm:ss}, 上次执行: {lastExecutedTime:HH:mm:ss})");

            foreach (var timePoint in pendingTimePoints)
            {
                Logger.Info("ScheduleManager", $"执行时间点: {timePoint.Id} ({timePoint.Time:HH:mm:ss}) - {timePoint.FromState} → {timePoint.ToState}");

                // 执行状态切换
                ExecuteTransition(timePoint);
                _currentPlan.LastExecutedTimePoint = timePoint;
            }
        }
    }

    /// <summary>
    /// 执行状态切换
    /// </summary>
    /// <param name="timePoint">时间点</param>
    private void ExecuteTransition(TimePoint timePoint)
    {
        Logger.Info("ScheduleManager", $"状态切换: {timePoint.FromState} → {timePoint.ToState} ({timePoint.Name})");

        // 1. 先更新当前时间段（使用时间点的时间，而不是当前轮询时间）
        // 这样 CurrentSegment 会在状态设置前正确更新
        UpdateCurrentSegment(timePoint.Time);

        // 2. 确定要应用的样式
        StyleOverrides? styleToApply = timePoint.StyleOverrides;

        // 如果切换到 Progress 状态且时间点没有自定义样式，使用时间段的样式
        if (timePoint.ToState == ProgressStateType.Progress && 
            (styleToApply == null || !styleToApply.HasAnyOverride) &&
            _currentPlan?.CurrentSegment?.StyleOverrides != null)
        {
            styleToApply = _currentPlan.CurrentSegment.StyleOverrides;
            Logger.Info("ScheduleManager", $"使用时间段样式: {_currentPlan.CurrentSegment.Name}");
        }

        // 3. 设置状态和进度（使用批量更新确保只触发一次回调）
        _stateManager.BatchUpdate(manager =>
        {
            // 设置状态
            manager.SetState(timePoint.ToState, styleToApply);

            // 设置进度
            if (timePoint.ToState == ProgressStateType.Progress)
            {
                // 蓝图设计：恢复时进度跳变到当前时间对应的进度
                var progress = CalculateProgressForTime(_currentTime);
                manager.UpdateProgress(progress);
            }
            else if (timePoint.ToState == ProgressStateType.Success)
            {
                manager.UpdateProgress(100);
            }
            else if (timePoint.ToState == ProgressStateType.Loading)
            {
                // Loading 状态不需要进度值（不确定动画）
                manager.UpdateProgress(0);
            }
            // Paused 状态：保持当前进度不变，不调用 UpdateProgress
        });
    }

    /// <summary>
    /// 计算指定时间的进度值
    /// 进度基于时间段自身计算
    /// 正常模式：0% → 100%（已过时间）
    /// 倒计时模式：100% → 0%（剩余时间）
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <returns>进度值 0-100</returns>
    private double CalculateProgressForTime(DateTime currentTime)
    {
        if (_currentPlan?.CurrentSegment == null) return 0;

        var segment = _currentPlan.CurrentSegment;

        // 进度基于时间段自身计算
        var totalDuration = segment.EndTime - segment.StartTime;
        var elapsed = currentTime - segment.StartTime;
        var progress = (elapsed.TotalSeconds / totalDuration.TotalSeconds) * 100;

        // 倒计时模式：进度翻转
        if (_currentBehavior.ReverseProgress)
        {
            progress = 100 - progress;
        }

        return Math.Clamp(progress, 0, 100);
    }

    /// <summary>
    /// 更新进度条
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    private void UpdateProgressBar(DateTime currentTime)
    {
        if (_currentPlan?.CurrentSegment == null) return;

        var segment = _currentPlan.CurrentSegment;

        // 只有在活跃时间段内 且 当前状态为 Progress 时才更新进度
        // 如果被时间点切换到其他状态（Paused、Success等），进度更新被抑制
        if (segment.IsActive && _stateManager.CurrentConfig.StateType == ProgressStateType.Progress)
        {
            var progress = CalculateProgressForTime(currentTime);
            _stateManager.UpdateProgress(progress);
        }
    }

    /// <summary>
    /// 处理事件队列
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    private void ProcessEventQueue(DateTime currentTime)
    {
        // 预留：处理其他时间相关事件
    }

    /// <summary>
    /// 时间跳跃事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">时间跳跃事件参数</param>
    private void OnTimeJumped(object? sender, TimeJumpedEventArgs e)
    {
        Logger.Info("ScheduleManager", $"时间跳跃: {e.OldTime:HH:mm:ss} → {e.NewTime:HH:mm:ss} (偏移: {e.Offset.TotalSeconds}秒)");

        if (e.NewTime > e.OldTime)
        {
            // 向前跳跃：执行错过的状态切换
            ExecuteMissedTransitions(e.OldTime, e.NewTime);
        }
        else
        {
            // 向后跳跃：回退状态
            RecalculateCurrentState(e.NewTime);
        }
    }

    /// <summary>
            /// 执行错过的状态切换
            /// </summary>
            /// <param name="oldTime">旧时间</param>
            /// <param name="newTime">新时间</param>
            private void ExecuteMissedTransitions(DateTime oldTime, DateTime newTime)
            {
                if (_currentPlan == null) return;
    
                // 找出 (oldTime, newTime] 范围内的时间点
                // 使用 >= 确保 oldTime 等于某个时间点时也能执行该状态切换
                var missedPoints = _currentPlan.TimePoints
                    .Where(tp => tp.Time >= oldTime && tp.Time <= newTime)
                    .OrderBy(tp => tp.Time)
                    .ToList();
    
                if (missedPoints.Any())
                {
                    Logger.Info("ScheduleManager", $"执行 {missedPoints.Count} 个错过的状态切换");

                    // 按顺序执行
                    foreach (var point in missedPoints)
                    {
                        ExecuteTransition(point);
                        _currentPlan.LastExecutedTimePoint = point;
                    }
                }
    
                // 更新当前时间段
                UpdateCurrentSegment(newTime);
            }
    /// <summary>
    /// 重新计算当前状态
    /// </summary>
    /// <param name="newTime">新时间</param>
    private void RecalculateCurrentState(DateTime newTime)
    {
        Logger.Info("ScheduleManager", "重新计算当前状态");

        // 更新当前时间段
        UpdateCurrentSegment(newTime);

        // 重新应用当前状态
        ApplyCurrentState();
    }

    /// <summary>
    /// 更新当前时间段
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    private void UpdateCurrentSegment(DateTime currentTime)
    {
        if (_currentPlan == null) return;

        var oldSegment = _currentPlan.CurrentSegment;
        
        // 调用 ExecutionPlan 的 UpdateCurrentState 方法
        _currentPlan.UpdateCurrentState(currentTime);
        
        var newSegment = _currentPlan.CurrentSegment;
        
        if (oldSegment?.State != newSegment?.State)
        {
            Logger.Info("ScheduleManager", $"时间段状态变化: {oldSegment?.State} → {newSegment?.State} (时间: {currentTime:HH:mm:ss})");
        }

        // 如果时间段发生变化，重新解析行为配置
        if (oldSegment != newSegment && newSegment != null)
        {
            ApplyBehavior(newSegment);
        }
        
        // ⚠️ 不要立即应用状态，避免覆盖 ExecuteTransition 设置的状态
        // 状态应该在 ExecuteTransition 中设置，然后在下一个轮询周期中应用
    }

    /// <summary>
    /// 解析并应用时间段的行为配置（三级优先级）
    /// 优先级：时间计划表 > 配置文件 > 全局默认
    /// </summary>
    /// <param name="segment">当前时间段</param>
    private void ApplyBehavior(TimeSegment segment)
    {
        // 三级链式合并：硬编码默认 → 配置文件级 → 时间计划级
        var configBehavior = LoadConfigBehavior();
        var segmentBehavior = segment.Behavior ?? new ScheduleBehaviorData();
        var resolved = segmentBehavior
            .MergeWith(configBehavior)
            .ToResolved();

        var oldBehavior = _currentBehavior;
        _currentBehavior = resolved;

        // 更新轮询间隔
        if (_timer != null && resolved.PollingIntervalMs != oldBehavior.PollingIntervalMs)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(resolved.PollingIntervalMs);
            Logger.Info("ScheduleManager", $"轮询间隔已更新: {oldBehavior.PollingIntervalMs}ms → {resolved.PollingIntervalMs}ms");
        }

        Logger.Info("ScheduleManager", $"行为配置已应用: {resolved}");
    }

    /// <summary>
    /// 从配置文件加载行为配置默认值
    /// </summary>
    /// <returns>配置文件级行为配置，加载失败返回 null</returns>
    private ScheduleBehaviorData? LoadConfigBehavior()
    {
        try
        {
            var configManager = Services.ConfigurationManager.Instance;
            if (configManager == null) return null;
            var timeTopSetting = configManager.LoadTimeTopSetting();
            return timeTopSetting?.Behavior;
        }
        catch (Exception ex)
        {
            Logger.Warn("ScheduleManager", $"读取行为配置失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 应用当前状态
    /// </summary>
    public void ApplyCurrentState()
    {
        if (_currentPlan?.CurrentSegment == null) return;

        var segment = _currentPlan.CurrentSegment;

        Logger.Info("ScheduleManager", $"应用当前状态: {segment.State} - {segment.Name} (时间: {_currentTime:HH:mm:ss})");

        // 使用批量更新确保只触发一次回调
        _stateManager.BatchUpdate(manager =>
        {
            // 设置状态
            manager.SetState(segment.State, segment.StyleOverrides);

            // 如果是活跃状态，计算当前进度
            if (segment.IsActive)
            {
                var currentTime = _timeService.GetCurrentTime();
                var progress = CalculateProgressForTime(currentTime);
                manager.UpdateProgress(progress);
            }
        });
    }

    /// <summary>
    /// 析构函数，取消事件订阅
    /// </summary>
    ~ScheduleManager()
    {
        if (_timeService != null)
        {
            _timeService.TimeJumped -= OnTimeJumped;
        }
    }
}