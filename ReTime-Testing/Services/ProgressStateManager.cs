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
        /// 预定义状态（向后兼容）
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
                Background = ProgressColors.DefaultBackground,
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
                Background = ProgressColors.DefaultBackground,
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
                Background = ProgressColors.DefaultBackground,
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
                Background = ProgressColors.DefaultBackground,
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
                Background = ProgressColors.DefaultBackground,
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
                Background = ProgressColors.DefaultBackground,
                Visibility = Visibility.Hidden,
                IsEnabled = true,
                Opacity = 1.0,
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
                Foreground = ProgressColors.Gray,
                Background = ProgressColors.DefaultBackground,
                Visibility = Visibility.Visible,
                IsEnabled = false,
                Opacity = 0.5,
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
        /// 设置状态（应用样式）
        /// 只更新样式属性，不更新进度值
        /// </summary>
        /// <param name="stateType">状态类型</param>
        /// <param name="overrides">样式覆盖（可选）</param>
        public void SetState(ProgressStateType stateType, StyleOverrides? overrides = null)
        {
            Logger.Info("ProgressStateManager", $"设置状态: {_currentConfig.StateType} → {stateType}");

            // 1. 获取基础样式（按优先级：配置文件 > 默认值）
            var baseStyle = GetBaseStyle(stateType);

            // 2. 应用覆盖样式
            var finalStyle = ApplyOverrides(baseStyle, overrides);

            // 3. 批量更新所有样式属性
            BeginBatchUpdate();

            _currentConfig.StateType = stateType;
            
            // 样式属性（可被覆盖）
            _currentConfig.Foreground = finalStyle.ForegroundColor;
            _currentConfig.Background = finalStyle.BackgroundColor ?? Brushes.Transparent;
            _currentConfig.Opacity = finalStyle.Opacity;

            // 状态属性（使用状态默认值，不可被样式覆盖）
            _currentConfig.IsIndeterminate = stateType == ProgressStateType.Loading;
            _currentConfig.Visibility = GetDefaultVisibility(stateType);
            _currentConfig.IsEnabled = stateType != ProgressStateType.Disabled;

            _currentConfig.SetInitialized();

            // 标记需要通知（确保 EndBatchUpdate 触发回调）
            _pendingNotify = true;

            EndBatchUpdate();

            Logger.Info("ProgressStateManager", $"状态设置完成: {_currentConfig.StateType}");
        }

        /// <summary>
        /// 获取状态默认可见性
        /// </summary>
        /// <param name="stateType">状态类型</param>
        /// <returns>默认可见性</returns>
        private Visibility GetDefaultVisibility(ProgressStateType stateType)
        {
            return stateType switch
            {
                ProgressStateType.Hidden => Visibility.Hidden,
                _ => Visibility.Visible
            };
        }

        /// <summary>
        /// 更新进度值
        /// 只更新进度值，不修改样式属性
        /// </summary>
        /// <param name="value">进度值</param>
        public void UpdateProgress(double value)
        {
            _currentConfig.Value = value;
            NotifyStateChanged();
        }

        /// <summary>
        /// 获取基础样式（按优先级：配置文件 > 默认值）
        /// </summary>
        /// <param name="stateType">状态类型</param>
        /// <returns>基础样式配置</returns>
        private StyleConfig GetBaseStyle(ProgressStateType stateType)
        {
            // 1. 获取默认样式
            var defaultStyle = GetDefaultStyle(stateType);

            // 2. 尝试从配置文件读取样式配置
            try
            {
                var configManager = Services.ConfigurationManager.Instance;
                var timeTopSetting = configManager.LoadTimeTopSetting();
                var stateName = stateType.ToString();
                
                if (timeTopSetting.StateStyles != null && timeTopSetting.StateStyles.Enabled
                    && timeTopSetting.StateStyles.Styles != null
                    && timeTopSetting.StateStyles.Styles.TryGetValue(stateName, out var styleData)
                    && styleData.Enabled)
                {
                    // 合并配置文件样式到默认样式
                    return MergeStyleConfig(defaultStyle, styleData);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("ProgressStateManager", $"读取样式配置失败: {ex.Message}");
            }

            // 3. 返回默认样式
            return defaultStyle;
        }

        /// <summary>
        /// 获取默认样式
        /// </summary>
        /// <param name="stateType">状态类型</param>
        /// <returns>默认样式配置</returns>
        private StyleConfig GetDefaultStyle(ProgressStateType stateType)
        {
            return stateType switch
            {
                ProgressStateType.Loading => new StyleConfig
                {
                    ForegroundColor = ProgressColors.DefaultBlue,
                    BackgroundColor = ProgressColors.DefaultBackground,
                    Opacity = 1.0
                },
                ProgressStateType.Progress => new StyleConfig
                {
                    ForegroundColor = ProgressColors.DefaultBlue,
                    BackgroundColor = ProgressColors.DefaultBackground,
                    Opacity = 1.0
                },
                ProgressStateType.Success => new StyleConfig
                {
                    ForegroundColor = ProgressColors.SuccessGreen,
                    BackgroundColor = ProgressColors.DefaultBackground,
                    Opacity = 1.0
                },
                ProgressStateType.Error => new StyleConfig
                {
                    ForegroundColor = ProgressColors.ErrorRed,
                    BackgroundColor = ProgressColors.DefaultBackground,
                    Opacity = 1.0
                },
                ProgressStateType.Paused => new StyleConfig
                {
                    ForegroundColor = ProgressColors.PauseOrange,
                    BackgroundColor = ProgressColors.DefaultBackground,
                    Opacity = 1.0
                },
                ProgressStateType.Hidden => new StyleConfig
                {
                    ForegroundColor = ProgressColors.DefaultBlue,
                    BackgroundColor = ProgressColors.DefaultBackground,
                    Opacity = 1.0
                },
                ProgressStateType.Disabled => new StyleConfig
                {
                    ForegroundColor = ProgressColors.Gray,
                    BackgroundColor = ProgressColors.DefaultBackground,
                    Opacity = 0.5
                },
                _ => new StyleConfig()
            };
        }

        /// <summary>
        /// 合并样式配置（配置文件样式覆盖默认样式）
        /// </summary>
        /// <param name="defaultStyle">默认样式</param>
        /// <param name="styleData">样式配置数据</param>
        /// <returns>合并后的样式</returns>
        private StyleConfig MergeStyleConfig(StyleConfig defaultStyle, StateStyleEntry styleData)
        {
            var result = new StyleConfig
            {
                ForegroundColor = defaultStyle.ForegroundColor,
                BackgroundColor = defaultStyle.BackgroundColor,
                Opacity = defaultStyle.Opacity
            };

            // 覆盖前景色
            if (!string.IsNullOrEmpty(styleData.ForegroundColor))
            {
                result.ForegroundColor = ParseColor(styleData.ForegroundColor);
            }

            // 覆盖背景色
            if (!string.IsNullOrEmpty(styleData.BackgroundColor))
            {
                result.BackgroundColor = ParseColor(styleData.BackgroundColor);
            }

            // 覆盖透明度
            if (styleData.Opacity.HasValue)
            {
                result.Opacity = styleData.Opacity.Value;
            }

            return result;
        }

        /// <summary>
        /// 解析颜色字符串
        /// </summary>
        /// <param name="colorString">颜色字符串（如 #007ACC）</param>
        /// <returns>Brush</returns>
        private Brush ParseColor(string colorString)
        {
            try
            {
                if (colorString.StartsWith("#"))
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorString);
                    return new SolidColorBrush(color);
                }
                return Brushes.Transparent;
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        /// <summary>
        /// 应用覆盖样式
        /// </summary>
        /// <param name="baseStyle">基础样式</param>
        /// <param name="overrides">覆盖样式</param>
        /// <returns>最终样式</returns>
        private StyleConfig ApplyOverrides(StyleConfig baseStyle, StyleOverrides? overrides)
        {
            if (overrides == null || !overrides.HasAnyOverride)
            {
                return baseStyle;
            }

            var result = new StyleConfig();

            // ForegroundColor
            result.ForegroundColor = overrides.ForegroundColor != null ? overrides.ForegroundColor : baseStyle.ForegroundColor;

            // BackgroundColor
            result.BackgroundColor = overrides.BackgroundColor != null ? overrides.BackgroundColor : baseStyle.BackgroundColor;

            // Opacity
            if (overrides.Opacity.HasValue)
            {
                result.Opacity = overrides.Opacity.Value;
            }
            else
            {
                result.Opacity = baseStyle.Opacity;
            }

            return result;
        }

        /// <summary>
        /// 设置状态（旧方法，保持兼容性）
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
        /// 设置进度值（旧方法，保持兼容性）
        /// </summary>
        public ProgressStateManager SetValue(double value)
        {
            _currentConfig.Value = value;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置前景色（旧方法，保持兼容性）
        /// </summary>
        public ProgressStateManager SetForeground(Brush foreground)
        {
            _currentConfig.Foreground = foreground;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置透明度（旧方法，保持兼容性）
        /// </summary>
        public ProgressStateManager SetOpacity(double opacity)
        {
            _currentConfig.Opacity = opacity;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置可见性（旧方法，保持兼容性）
        /// </summary>
        public ProgressStateManager SetVisibility(Visibility visibility)
        {
            _currentConfig.Visibility = visibility;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置启用状态（旧方法，保持兼容性）
        /// </summary>
        public ProgressStateManager SetEnabled(bool isEnabled)
        {
            _currentConfig.IsEnabled = isEnabled;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置背景色（旧方法，保持兼容性）
        /// </summary>
        public ProgressStateManager SetBackground(Brush background)
        {
            _currentConfig.Background = background;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 设置范围（旧方法，保持兼容性）
        /// </summary>
        public ProgressStateManager SetRange(double minimum, double maximum)
        {
            _currentConfig.Minimum = minimum;
            _currentConfig.Maximum = maximum;
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 重置为默认状态（旧方法，保持兼容性）
        /// </summary>
        public ProgressStateManager Reset()
        {
            _currentConfig = ProgressStateConfig.Default();
            NotifyStateChanged();
            return this;
        }

        /// <summary>
        /// 创建构建器（旧方法，保持兼容性）
        /// </summary>
        public static ProgressStateConfigBuilder CreateBuilder()
        {
            return new ProgressStateConfigBuilder();
        }
    }

    /// <summary>
    /// 样式配置（内部使用）
    /// </summary>
    internal class StyleConfig
    {
        public Brush ForegroundColor { get; set; } = ProgressColors.DefaultBlue;
        public Brush? BackgroundColor { get; set; }
        public double Opacity { get; set; } = 1.0;
    }
}