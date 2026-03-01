using ReTime_Testing.Models;
using System.Windows;
using System.Windows.Media;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 进度条状态管理器
    /// </summary>
    public class ProgressStateManager
    {
        private bool _isBatchUpdating = false;
        private bool _pendingNotify = false;

        /// <summary>
        /// 状态变更回调
        /// </summary>
        public Action<ProgressStateConfig>? OnStateChanged { get; set; }

        /// <summary>
        /// 预定义状态
        /// </summary>
        public static class ProgressStates
        {
            /// <summary>
            /// 加载状态 - 蓝色不确定动画
            /// </summary>
            public static ProgressStateConfig Loading => new ProgressStateConfig
            {
                StateType = ProgressStateType.Loading,
                Value = 0,
                IsIndeterminate = true,
                Foreground = ProgressColors.DefaultBlue,
                Background = Brushes.Transparent,
                Visibility = Visibility.Visible,
                IsEnabled = true,
                Opacity = 1.0,
                Minimum = 0,
                Maximum = 100
            }.SetInitialized();

            /// <summary>
            /// 成功状态 - 绿色完成
            /// </summary>
            public static ProgressStateConfig Success => new ProgressStateConfig
            {
                StateType = ProgressStateType.Success,
                Value = 100,
                IsIndeterminate = false,
                Foreground = ProgressColors.SuccessGreen,
                Background = Brushes.Transparent,
                Visibility = Visibility.Visible,
                IsEnabled = true,
                Opacity = 1.0,
                Minimum = 0,
                Maximum = 100
            }.SetInitialized();

            /// <summary>
            /// 错误状态 - 红色
            /// </summary>
            public static ProgressStateConfig Error => new ProgressStateConfig
            {
                StateType = ProgressStateType.Error,
                Value = 100,
                IsIndeterminate = false,
                Foreground = ProgressColors.ErrorRed,
                Background = Brushes.Transparent,
                Visibility = Visibility.Visible,
                IsEnabled = true,
                Opacity = 1.0,
                Minimum = 0,
                Maximum = 100
            }.SetInitialized();

            /// <summary>
            /// 暂停状态 - 橙色
            /// </summary>
            public static ProgressStateConfig Paused => new ProgressStateConfig
            {
                StateType = ProgressStateType.Paused,
                Value = 50,
                IsIndeterminate = false,
                Foreground = ProgressColors.PauseOrange,
                Background = Brushes.Transparent,
                Visibility = Visibility.Visible,
                IsEnabled = true,
                Opacity = 1.0,
                Minimum = 0,
                Maximum = 100
            }.SetInitialized();

            /// <summary>
            /// 进度状态 - 蓝色（需设置 Value）
            /// </summary>
            public static ProgressStateConfig Progress => new ProgressStateConfig
            {
                StateType = ProgressStateType.Progress,
                Value = 0,
                IsIndeterminate = false,
                Foreground = ProgressColors.DefaultBlue,
                Background = Brushes.Transparent,
                Visibility = Visibility.Visible,
                IsEnabled = true,
                Opacity = 1.0,
                Minimum = 0,
                Maximum = 100
            }.SetInitialized();

            /// <summary>
            /// 隐藏状态
            /// </summary>
            public static ProgressStateConfig Hidden => new ProgressStateConfig
            {
                StateType = ProgressStateType.Hidden,
                Value = 0,
                IsIndeterminate = false,
                Foreground = ProgressColors.DefaultBlue,
                Background = Brushes.Transparent,
                Visibility = Visibility.Hidden,
                IsEnabled = true,
                Opacity = 1.0,
                Minimum = 0,
                Maximum = 100
            }.SetInitialized();

            /// <summary>
            /// 半透明状态
            /// </summary>
            public static ProgressStateConfig HalfOpacity => new ProgressStateConfig
            {
                StateType = ProgressStateType.Progress,
                Value = 100,
                IsIndeterminate = false,
                Foreground = ProgressColors.DefaultBlue,
                Background = Brushes.Transparent,
                Visibility = Visibility.Visible,
                IsEnabled = true,
                Opacity = 0.5,
                Minimum = 0,
                Maximum = 100
            }.SetInitialized();

            /// <summary>
            /// 禁用状态
            /// </summary>
            public static ProgressStateConfig Disabled => new ProgressStateConfig
            {
                StateType = ProgressStateType.Disabled,
                Value = 100,
                IsIndeterminate = false,
                Foreground = ProgressColors.DefaultBlue,
                Background = Brushes.Transparent,
                Visibility = Visibility.Visible,
                IsEnabled = false,
                Opacity = 1.0,
                Minimum = 0,
                Maximum = 100
            }.SetInitialized();
        }

        private ProgressStateConfig _currentConfig = ProgressStateConfig.Default();

        /// <summary>
        /// 当前配置
        /// </summary>
        public ProgressStateConfig CurrentConfig => _currentConfig;

        /// <summary>
        /// 触发状态变更通知（在批量更新模式下延迟触发）
        /// </summary>
        private void NotifyStateChanged()
        {
            if (_isBatchUpdating)
            {
                _pendingNotify = true;
            }
            else
            {
                OnStateChanged?.Invoke(_currentConfig);
            }
        }

        /// <summary>
        /// 开始批量更新（期间不会触发回调）
        /// </summary>
        public ProgressStateManager BeginBatchUpdate()
        {
            _isBatchUpdating = true;
            _pendingNotify = false;
            return this;
        }

        /// <summary>
        /// 结束批量更新（触发一次回调）
        /// </summary>
        public ProgressStateManager EndBatchUpdate()
        {
            _isBatchUpdating = false;
            if (_pendingNotify)
            {
                _pendingNotify = false;
                OnStateChanged?.Invoke(_currentConfig);
            }
            return this;
        }

        /// <summary>
        /// 批量更新操作（期间不会触发回调）
        /// </summary>
        public void BatchUpdate(Action<ProgressStateManager> action)
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

        /// <summary>
        /// 设置状态
        /// </summary>
        public ProgressStateManager SetState(ProgressStateConfig config)
        {
            // 直接复制所有属性值，不使用 Clone
            _currentConfig.StateType = config.StateType;
            _currentConfig.Value = config.Value;
            _currentConfig.IsIndeterminate = config.IsIndeterminate;
            _currentConfig.Foreground = config.Foreground;
            _currentConfig.Background = config.Background;
            _currentConfig.Visibility = config.Visibility;
            _currentConfig.IsEnabled = config.IsEnabled;
            _currentConfig.Opacity = config.Opacity;
            _currentConfig.Minimum = config.Minimum;
            _currentConfig.Maximum = config.Maximum;

            // 标记初始化完成，启用验证
            _currentConfig.SetInitialized();

            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置进度值
        /// </summary>
        public ProgressStateManager SetValue(double value)
        {
            _currentConfig.Value = value;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置前景色
        /// </summary>
        public ProgressStateManager SetForeground(Brush foreground)
        {
            _currentConfig.Foreground = foreground;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置透明度
        /// </summary>
        public ProgressStateManager SetOpacity(double opacity)
        {
            _currentConfig.Opacity = opacity;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置可见性
        /// </summary>
        public ProgressStateManager SetVisibility(Visibility visibility)
        {
            _currentConfig.Visibility = visibility;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置启用状态
        /// </summary>
        public ProgressStateManager SetEnabled(bool isEnabled)
        {
            _currentConfig.IsEnabled = isEnabled;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置背景色
        /// </summary>
        public ProgressStateManager SetBackground(Brush background)
        {
            _currentConfig.Background = background;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置范围
        /// </summary>
        public ProgressStateManager SetRange(double minimum, double maximum)
        {
            _currentConfig.Minimum = minimum;
            _currentConfig.Maximum = maximum;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 重置为默认状态
        /// </summary>
        public ProgressStateManager Reset()
        {
            _currentConfig = ProgressStateConfig.Default();
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 创建构建器
        /// </summary>
        public static ProgressStateConfigBuilder CreateBuilder()
        {
            return new ProgressStateConfigBuilder();
        }
    }
}