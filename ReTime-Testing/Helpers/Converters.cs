using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Effects;

namespace ReTime_Testing.Helpers
{
    public class BindingProxy : Freezable
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

        public object Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        protected override Freezable CreateInstanceCore() => new BindingProxy();
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
                return v == Visibility.Visible;
            return false;
        }
    }

    /// <summary>
    /// 将 true 转换为 Collapsed，false 转换为 Visible
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
                return v != Visibility.Visible;
            return false;
        }
    }

    /// <summary>
    /// 将 null 转换为 Visible，非 null 转换为 Collapsed
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将非 null 转换为 Visible，null 转换为 Collapsed
    /// </summary>
    public class NotNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到DropShadowEffect的转换器
    /// </summary>
    public class BooleanToDropShadowEffectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool enableShadow && enableShadow)
            {
                return new DropShadowEffect
                {
                    ShadowDepth = 2,
                    BlurRadius = 8,
                    Color = System.Windows.Media.Colors.Black,
                    Opacity = 0.3
                };
            }
            
            return null!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack 方法不常用，返回默认值
            return true;
        }
    }
    
    /// <summary>
    /// 布尔值到Effect的转换器（通用）
    /// </summary>
    public class BooleanToEffectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool enableEffect && enableEffect)
            {
                // 如果没有提供参数，则使用默认的DropShadowEffect
                if (parameter is Effect effect)
                {
                    return effect;
                }
                
                // 默认创建DropShadowEffect
                return new DropShadowEffect
                {
                    ShadowDepth = 2,
                    BlurRadius = 8,
                    Color = System.Windows.Media.Colors.Black,
                    Opacity = 0.3
                };
            }
            
            return null!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }
    }

    /// <summary>
    /// 时间字符串 (HH:mm:ss) 与 DateTime 之间的双向转换器
    /// 用于 TimePicker 控件绑定
    /// </summary>
    public class TimeStringToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string timeStr && TimeSpan.TryParse(timeStr, out var timeSpan))
            {
                return DateTime.Today.Add(timeSpan);
            }
            return DateTime.Today.AddHours(9);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                return dt.ToString("HH:mm:ss");
            }
            return "00:00:00";
        }
    }
}