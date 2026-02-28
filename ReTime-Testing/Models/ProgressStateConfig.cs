using System.Windows;
using System.Windows.Media;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 进度条状态类型枚举
    /// </summary>
    public enum ProgressStateType
    {
        /// <summary>
        /// 加载中
        /// </summary>
        Loading,

        /// <summary>
        /// 进行中
        /// </summary>
        Progress,

        /// <summary>
        /// 成功
        /// </summary>
        Success,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 暂停
        /// </summary>
        Paused,

        /// <summary>
        /// 隐藏
        /// </summary>
        Hidden,

        /// <summary>
        /// 禁用
        /// </summary>
        Disabled
    }

    /// <summary>
    /// 进度条状态配置类
    /// </summary>
    public class ProgressStateConfig
    {
        private double _value;
        private double _opacity;
        private double _minimum;
        private double _maximum;

        /// <summary>
        /// 状态类型
        /// </summary>
        public ProgressStateType StateType { get; set; }

        /// <summary>
        /// 进度值（自动限制在 Min-Max 范围内）
        /// </summary>
        public double Value
        {
            get => _value;
            set => _value = Math.Clamp(value, Minimum, Maximum);
        }

        /// <summary>
        /// 不确定模式
        /// </summary>
        public bool IsIndeterminate { get; set; }

        /// <summary>
        /// 前景色
        /// </summary>
        public Brush? Foreground { get; set; }

        /// <summary>
        /// 背景色
        /// </summary>
        public Brush? Background { get; set; }

        /// <summary>
        /// 可见性
        /// </summary>
        public Visibility Visibility { get; set; }

        /// <summary>
        /// 启用状态
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 透明度（自动限制在 0-1 范围内）
        /// </summary>
        public double Opacity
        {
            get => _opacity;
            set => _opacity = Math.Clamp(value, 0.0, 1.0);
        }

        /// <summary>
        /// 最小值
        /// </summary>
        public double Minimum
        {
            get => _minimum;
            set
            {
                // 只在非初始化时验证（当 _maximum 不为默认值时）
                if (_maximum != 0 && value >= Maximum)
                    throw new ArgumentException("Minimum must be less than Maximum");
                _minimum = value;
                // 确保当前值在范围内
                if (_value < _minimum) _value = _minimum;
            }
        }

        /// <summary>
        /// 最大值
        /// </summary>
        public double Maximum
        {
            get => _maximum;
            set
            {
                // 只在非初始化时验证（当 _minimum 不为默认值时）
                if (_minimum != 0 && value <= Minimum)
                    throw new ArgumentException("Maximum must be greater than Minimum");
                _maximum = value;
                // 确保当前值在范围内
                if (_value > _maximum) _value = _maximum;
            }
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static ProgressStateConfig Default()
        {
            return new ProgressStateConfig
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
            };
        }

        /// <summary>
        /// 克隆当前配置
        /// </summary>
        public ProgressStateConfig Clone()
        {
            return new ProgressStateConfig
            {
                StateType = StateType,
                Value = Value,
                IsIndeterminate = IsIndeterminate,
                Foreground = Foreground,  // Brush 是引用类型，直接赋值
                Background = Background,  // Brush 是引用类型，直接赋值
                Visibility = Visibility,
                IsEnabled = IsEnabled,
                Opacity = Opacity,
                Minimum = Minimum,
                Maximum = Maximum
            };
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public bool IsValid()
        {
            return Minimum < Maximum
                && Value >= Minimum && Value <= Maximum
                && Opacity >= 0.0 && Opacity <= 1.0;
        }

        /// <summary>
        /// 获取验证错误信息
        /// </summary>
        public string? GetValidationError()
        {
            if (Minimum >= Maximum)
                return "Minimum must be less than Maximum";

            if (Value < Minimum || Value > Maximum)
                return $"Value must be between {Minimum} and {Maximum}";

            if (Opacity < 0.0 || Opacity > 1.0)
                return "Opacity must be between 0.0 and 1.0";

            return null;
        }
    }

    /// <summary>
    /// 进度条颜色定义
    /// </summary>
    public static class ProgressColors
    {
        public static readonly Brush DefaultBlue = new SolidColorBrush(Color.FromRgb(0x00, 0x67, 0xc0));
        public static readonly Brush ErrorRed = new SolidColorBrush(Color.FromRgb(0xc4, 0x2b, 0x1c));
        public static readonly Brush PauseOrange = new SolidColorBrush(Color.FromRgb(0x9d, 0x5d, 0x00));
        public static readonly Brush SuccessGreen = new SolidColorBrush(Color.FromRgb(0x00, 0x99, 0x00));
        public static readonly Brush Gray = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
    }
}