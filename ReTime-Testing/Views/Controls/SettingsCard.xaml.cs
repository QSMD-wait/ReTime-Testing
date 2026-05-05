using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        /// <summary>
        /// 是否可展开（显示右侧箭头）
        /// </summary>
        public static readonly DependencyProperty IsExpandableProperty =
            DependencyProperty.Register(nameof(IsExpandable), typeof(bool), typeof(SettingsCard),
                new PropertyMetadata(false, OnIsExpandableChanged));

        public bool IsExpandable
        {
            get => (bool)GetValue(IsExpandableProperty);
            set => SetValue(IsExpandableProperty, value);
        }

        /// <summary>
        /// 是否已展开
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(SettingsCard),
                new PropertyMetadata(false, OnIsExpandedChanged));

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>
        /// 展开后的内容
        /// </summary>
        public static readonly DependencyProperty ExpandContentProperty =
            DependencyProperty.Register(nameof(ExpandContent), typeof(object), typeof(SettingsCard),
                new PropertyMetadata(null));

        public object ExpandContent
        {
            get => GetValue(ExpandContentProperty);
            set => SetValue(ExpandContentProperty, value);
        }

        #endregion

        #region 回调

        private static void OnIsExpandableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var card = (SettingsCard)d;
            card.HeaderRow.Cursor = (bool)e.NewValue ? Cursors.Hand : null;
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var card = (SettingsCard)d;
            card.UpdateExpandState((bool)e.NewValue);
        }

        #endregion

        #region 交互

        private void OnHeaderClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsExpandable)
            {
                IsExpanded = !IsExpanded;
            }
        }

        private void UpdateExpandState(bool expanded)
        {
            ExpandContentBorder.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            ChevronRotate.Angle = expanded ? 90 : 0;
        }

        #endregion
    }
}
