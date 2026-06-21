using ReTime_Testing.Models;
using System.Windows;
using System.Windows.Media;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 进度条状态配置构建器
    /// </summary>
    public class ProgressStateConfigBuilder
    {
        private readonly ProgressStateConfig _config;

        public ProgressStateConfigBuilder()
        {
            _config = ProgressStateConfig.Default();
        }

        /// <summary>
        /// 设置进度值
        /// </summary>
        public ProgressStateConfigBuilder WithProgress(double value)
        {
            _config.Value = value;
            return this;
        }

        /// <summary>
        /// 设置前景色
        /// </summary>
        public ProgressStateConfigBuilder WithColor(Brush color)
        {
            _config.Foreground = color;
            return this;
        }

        /// <summary>
        /// 设置透明度
        /// </summary>
        public ProgressStateConfigBuilder WithOpacity(double opacity)
        {
            _config.Opacity = opacity;
            return this;
        }

        /// <summary>
        /// 设置可见性
        /// </summary>
        public ProgressStateConfigBuilder WithVisibility(Visibility visibility)
        {
            _config.Visibility = visibility;
            return this;
        }

        /// <summary>
        /// 设置启用状态
        /// </summary>
        public ProgressStateConfigBuilder WithEnabled(bool isEnabled)
        {
            _config.IsEnabled = isEnabled;
            return this;
        }

        /// <summary>
        /// 设置不确定模式
        /// </summary>
        public ProgressStateConfigBuilder WithIndeterminate(bool isIndeterminate)
        {
            _config.IsIndeterminate = isIndeterminate;
            return this;
        }

        /// <summary>
        /// 设置范围
        /// </summary>
        public ProgressStateConfigBuilder WithRange(double min, double max)
        {
            _config.Minimum = min;
            _config.Maximum = max;
            return this;
        }

        /// <summary>
        /// 设置背景色
        /// </summary>
        public ProgressStateConfigBuilder WithBackground(Brush background)
        {
            _config.Background = background;
            return this;
        }

        /// <summary>
        /// 设置状态类型
        /// </summary>
        public ProgressStateConfigBuilder WithStateType(ProgressStateType stateType)
        {
            _config.StateType = stateType;
            return this;
        }

        /// <summary>
        /// 基于预定义状态构建
        /// </summary>
        public ProgressStateConfigBuilder From(ProgressStateConfig baseConfig)
        {
            _config.StateType = baseConfig.StateType;
            _config.Value = baseConfig.Value;
            _config.IsIndeterminate = baseConfig.IsIndeterminate;
            _config.Foreground = baseConfig.Foreground;
            _config.Background = baseConfig.Background;
            _config.Visibility = baseConfig.Visibility;
            _config.IsEnabled = baseConfig.IsEnabled;
            _config.Opacity = baseConfig.Opacity;
            _config.Minimum = baseConfig.Minimum;
            _config.Maximum = baseConfig.Maximum;
            return this;
        }

        /// <summary>
        /// 构建配置
        /// </summary>
        public ProgressStateConfig Build()
        {
            // 验证配置
            var error = _config.GetValidationError();
            if (error != null)
            {
                throw new InvalidOperationException($"配置验证失败: {error}");
            }
            return _config.Clone();
        }
    }
}