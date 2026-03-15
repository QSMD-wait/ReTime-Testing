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
            private bool _initialized = false;  // 标记是否已完成初始化
    
            /// <summary>
            /// 状态类型
            /// </summary>
            public ProgressStateType StateType { get; set; }
    
            /// <summary>
            /// 进度值（初始化时不验证，运行时才限制在 Min-Max 范围内）
            /// </summary>
            public double Value
            {
                get => _value;
                set
                {
                    if (_initialized)
                    {
                        _value = Math.Clamp(value, Minimum, Maximum);
                    }
                    else
                    {
                        _value = value;
                    }
                }
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
        /// 透明度（初始化时不验证，运行时才限制在 0-1 范围内）
        /// </summary>
        public double Opacity
        {
            get => _opacity;
            set
            {
                if (_initialized)
                {
                    _opacity = Math.Clamp(value, 0.0, 1.0);
                }
                else
                {
                    _opacity = value;
                }
            }
        }

        /// <summary>
        /// 最小值
        /// </summary>
        public double Minimum
        {
            get => _minimum;
            set
            {
                // 只在初始化完成后验证
                if (_initialized && value >= Maximum)
                    throw new ArgumentException("Minimum must be less than Maximum");
                _minimum = value;
                // 确保当前值在范围内（只在初始化完成后）
                if (_initialized && _value < _minimum) _value = _minimum;
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
                // 只在初始化完成后验证
                if (_initialized && value <= Minimum)
                    throw new ArgumentException("Maximum must be greater than Minimum");
                _maximum = value;
                // 确保当前值在范围内（只在初始化完成后）
                if (_initialized && _value > _maximum) _value = _maximum;
            }
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static ProgressStateConfig Default()
        {
            var config = new ProgressStateConfig
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
            };
            config.SetInitialized();
            return config;
        }

        /// <summary>
        /// 克隆当前配置
        /// </summary>
        public ProgressStateConfig Clone()
        {
            var config = new ProgressStateConfig
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
            config.SetInitialized();
            return config;
        }

        /// <summary>
        /// 设置配置为已初始化状态（启用验证）
        /// </summary>
        internal ProgressStateConfig SetInitialized()
        {
            _initialized = true;
            return this;
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
        private static readonly Brush _defaultBlue = new SolidColorBrush(Color.FromRgb(0x00, 0x67, 0xc0));
        private static readonly Brush _errorRed = new SolidColorBrush(Color.FromRgb(0xc4, 0x2b, 0x1c));
        private static readonly Brush _pauseOrange = new SolidColorBrush(Color.FromRgb(0x9d, 0x5d, 0x00));
        private static readonly Brush _successGreen = new SolidColorBrush(Color.FromRgb(0x00, 0x99, 0x00));
        private static readonly Brush _gray = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
        private static readonly Brush _defaultBackground = new SolidColorBrush(Color.FromRgb(0x86, 0x86, 0x86));

        static ProgressColors()
        {
            // 冻结所有 Brush 以提高性能
            _defaultBlue.Freeze();
            _errorRed.Freeze();
            _pauseOrange.Freeze();
            _successGreen.Freeze();
            _gray.Freeze();
            _defaultBackground.Freeze();
        }

        public static Brush DefaultBlue => _defaultBlue;
        public static Brush ErrorRed => _errorRed;
        public static Brush PauseOrange => _pauseOrange;
        public static Brush SuccessGreen => _successGreen;
        public static Brush Gray => _gray;
        public static Brush DefaultBackground => _defaultBackground;
    }
}