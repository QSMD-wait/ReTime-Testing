using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Common.IconKeys;

namespace ReTime_Testing.Views.Controls
{
    public partial class SettingsCard : UserControl
    {
        public SettingsCard()
        {
            InitializeComponent();
        }

        #region 依赖属性

        /// <summary>
        /// 图标（使用Fluent System Icons或Segoe Fluent Icons）
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(FontIconData), typeof(SettingsCard),
                new PropertyMetadata(null));

        public FontIconData Icon
        {
            get => (FontIconData)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// 标题
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingsCard),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// 描述文本
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsCard),
                new PropertyMetadata(null));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// 右侧控件内容
        /// </summary>
        public static readonly DependencyProperty ControlContentProperty =
            DependencyProperty.Register(nameof(ControlContent), typeof(object), typeof(SettingsCard),
                new PropertyMetadata(null));

        public object ControlContent
        {
            get => GetValue(ControlContentProperty);
            set => SetValue(ControlContentProperty, value);
        }

        #endregion
    }
}