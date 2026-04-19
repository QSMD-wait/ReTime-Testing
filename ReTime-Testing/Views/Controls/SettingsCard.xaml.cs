using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        /// 图标字符
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(SettingsCard),
                new PropertyMetadata(null, OnIconChanged));

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card && e.NewValue is string icon)
            {
                card.IconTextBlock.Text = icon;
            }
        }

        /// <summary>
        /// 标题
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingsCard),
                new PropertyMetadata(null, OnTitleChanged));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card && e.NewValue is string title)
            {
                card.TitleTextBlock.Text = title;
            }
        }

        /// <summary>
        /// 描述文本
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsCard),
                new PropertyMetadata(null, OnDescriptionChanged));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card && e.NewValue is string desc)
            {
                card.DescriptionTextBlock.Text = desc;
            }
        }

        /// <summary>
        /// 右侧控件内容
        /// </summary>
        public static readonly DependencyProperty ControlContentProperty =
            DependencyProperty.Register(nameof(ControlContent), typeof(object), typeof(SettingsCard),
                new PropertyMetadata(null, OnControlContentChanged));

        public object ControlContent
        {
            get => GetValue(ControlContentProperty);
            set => SetValue(ControlContentProperty, value);
        }

        private static void OnControlContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card)
            {
                card.ContentPresenter.Content = e.NewValue;
            }
        }

        /// <summary>
        /// 背景色
        /// </summary>
        public static readonly DependencyProperty CardBackgroundProperty =
            DependencyProperty.Register(nameof(CardBackground), typeof(Brush), typeof(SettingsCard),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(249, 249, 249)), OnCardBackgroundChanged));

        public Brush CardBackground
        {
            get => (Brush)GetValue(CardBackgroundProperty);
            set => SetValue(CardBackgroundProperty, value);
        }

        private static void OnCardBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card && e.NewValue is Brush brush)
            {
                card.CardBorder.Background = brush;
            }
        }

        /// <summary>
        /// 图标颜色
        /// </summary>
        public static readonly DependencyProperty IconForegroundProperty =
            DependencyProperty.Register(nameof(IconForeground), typeof(Brush), typeof(SettingsCard),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(51, 51, 51)), OnIconForegroundChanged));

        public Brush IconForeground
        {
            get => (Brush)GetValue(IconForegroundProperty);
            set => SetValue(IconForegroundProperty, value);
        }

        private static void OnIconForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SettingsCard card && e.NewValue is Brush brush)
            {
                card.IconTextBlock.Foreground = brush;
            }
        }

        #endregion
    }
}