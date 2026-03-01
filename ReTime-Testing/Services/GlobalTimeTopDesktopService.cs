using ReTime_Testing.Models;
using System.Windows;
using System.Windows.Media;

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
        
        private GlobalTimeTopDesktopService()
        {
            _stateManager = new ProgressStateManager();
        }
        
        // ==================== 状态设置 ====================
        
        /// <summary>
        /// 设置为加载状态（蓝色不确定动画）
        /// </summary>
        public void SetLoading() => _stateManager.SetState(ProgressStateManager.ProgressStates.Loading);
        
        /// <summary>
        /// 设置为进度状态（蓝色确定进度）
        /// </summary>
        public void SetProgress(double value)
        {
            _stateManager.SetState(ProgressStateManager.ProgressStates.Progress);
            _stateManager.SetValue(value);
        }
        
        /// <summary>
        /// 设置为成功状态（绿色完成）
        /// </summary>
        public void SetSuccess() => _stateManager.SetState(ProgressStateManager.ProgressStates.Success);
        
        /// <summary>
        /// 设置为错误状态（红色）
        /// </summary>
        public void SetError() => _stateManager.SetState(ProgressStateManager.ProgressStates.Error);
        
        /// <summary>
        /// 设置为暂停状态（橙色）
        /// </summary>
        public void SetPaused() => _stateManager.SetState(ProgressStateManager.ProgressStates.Paused);
        
        /// <summary>
        /// 设置为隐藏状态
        /// </summary>
        public void SetHidden() => _stateManager.SetState(ProgressStateManager.ProgressStates.Hidden);
        
        /// <summary>
        /// 设置为禁用状态
        /// </summary>
        public void SetDisabled() => _stateManager.SetState(ProgressStateManager.ProgressStates.Disabled);
        
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
    }
}