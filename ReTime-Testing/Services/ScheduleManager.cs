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

        // 查找当前时间点（5秒窗口内，提高可靠性）
        var timePoint = _currentPlan.TimePoints
            .FirstOrDefault(tp => tp.Time <= currentTime &&
                                 tp.Time.AddSeconds(5) > currentTime);

        if (timePoint != null && _currentPlan.NextTimePoint != timePoint)
        {
            // 执行状态切换
            ExecuteTransition(timePoint);
            _currentPlan.NextTimePoint = timePoint;
        }
    }

    /// <summary>
            /// 执行状态切换
            /// </summary>
            /// <param name="timePoint">时间点</param>
            private void ExecuteTransition(TimePoint timePoint)
            {
                Logger.Info("ScheduleManager", $"状态切换: {timePoint.FromState} → {timePoint.ToState} ({timePoint.Name})");
    
                // 设置状态
                _stateManager.SetState(timePoint.ToState, timePoint.StyleOverrides);
    
                // 设置初始进度
                if (timePoint.ToState == ProgressStateType.Progress)
                {
                    _stateManager.UpdateProgress(0);
                }
                else if (timePoint.ToState == ProgressStateType.Success)
                {
                    _stateManager.UpdateProgress(100);
                }
    
                // 更新当前时间段
                UpdateCurrentSegment(_currentTime);
            }
    /// <summary>
    /// 更新进度条
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    private void UpdateProgressBar(DateTime currentTime)
    {
        if (_currentPlan?.CurrentSegment == null) return;

        var segment = _currentPlan.CurrentSegment;

        // 只有活跃时间段才更新进度
        if (segment.IsActive)
        {
            var totalDuration = segment.EndTime - segment.StartTime;
            var elapsed = currentTime - segment.StartTime;
            var progress = (elapsed.TotalSeconds / totalDuration.TotalSeconds) * 100;

            _stateManager.UpdateProgress(Math.Clamp(progress, 0, 100));
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
                        _currentPlan.NextTimePoint = point;
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

        // 调用 ExecutionPlan 的 UpdateCurrentState 方法
        _currentPlan.UpdateCurrentState(currentTime);
    }

    /// <summary>
    /// 应用当前状态
    /// </summary>
    public void ApplyCurrentState()
    {
        if (_currentPlan?.CurrentSegment == null) return;

        var segment = _currentPlan.CurrentSegment;

        Logger.Info("ScheduleManager", $"应用当前状态: {segment.State} - {segment.Name}");

        // 设置状态
        _stateManager.SetState(segment.State, segment.StyleOverrides);

        // 如果是活跃状态，计算当前进度
        if (segment.IsActive)
        {
            var currentTime = _timeService.GetCurrentTime();
            var totalDuration = segment.EndTime - segment.StartTime;
            var elapsed = currentTime - segment.StartTime;
            var progress = (elapsed.TotalSeconds / totalDuration.TotalSeconds) * 100;

            _stateManager.UpdateProgress(Math.Clamp(progress, 0, 100));
        }
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