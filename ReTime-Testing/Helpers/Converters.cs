using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ReTime_Testing.Helpers
{
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
    /// 在中文显示名称和实际值之间转换
    /// </summary>
    public class StringMappingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                // 根据参数决定转换方向：displayToValue 或 valueToDisplay
                var direction = parameter as string;
                
                if (direction == "valueToDisplay")
                {
                    // 将实际值转换为显示名称
                    return strValue switch
                    {
                        "registry" => "注册表索引",
                        "startupFolder" => "启动文件夹索引",
                        "light" => "明亮",
                        "dark" => "暗黑",
                        _ => strValue
                    };
                }
                else
                {
                    // 将显示名称转换为实际值
                    return strValue switch
                    {
                        "注册表索引" => "registry",
                        "启动文件夹索引" => "startupFolder",
                        "明亮" => "light",
                        "暗黑" => "dark",
                        _ => strValue
                    };
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack 用于将显示名称转换回实际值
            if (value is string strValue)
            {
                var direction = parameter as string;
                
                if (direction != "valueToDisplay") // 默认或非"valueToDisplay"都认为是反向转换
                {
                    return strValue switch
                    {
                        "注册表索引" => "registry",
                        "启动文件夹索引" => "startupFolder",
                        "明亮" => "light",
                        "暗黑" => "dark",
                        _ => strValue
                    };
                }
            }
            return value;
        }
    }
}