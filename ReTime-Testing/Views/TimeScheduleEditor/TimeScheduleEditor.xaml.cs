using System.Collections.ObjectModel;
using System.Windows;
using iNKORE.UI.WPF.Modern.Controls;

namespace ReTime_Testing.Views.TimeScheduleEditor
{
    /// <summary>
    /// 计划表列表项（临时示例）
    /// </summary>
    public class TimeScheduleListItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsActivated { get; set; }
    }

    /// <summary>
    /// 时间点列表项（临时示例）
    /// </summary>
    public class TimePointListItem
    {
        public string Name { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string TypeIcon { get; set; } = "\uE47A"; // 默认上课图标
    }

    /// <summary>
    /// TimeScheduleEditor.xaml 的交互逻辑
    /// </summary>
    public partial class TimeScheduleEditor : Window
    {
        // 临时示例数据
        public ObservableCollection<TimeScheduleListItem> TimeSchedules { get; } = new()
        {
            new TimeScheduleListItem { Id = "1", Name = "默认工作时间", IsActivated = true },
            new TimeScheduleListItem { Id = "2", Name = "学校课表", IsActivated = false },
            new TimeScheduleListItem { Id = "3", Name = "周末计划", IsActivated = false }
        };

        public ObservableCollection<TimePointListItem> TimePoints { get; } = new()
        {
            new TimePointListItem { Name = "上午工作", StartTime = "09:00:00", EndTime = "12:00:00", TypeIcon = "\uE47A" },
            new TimePointListItem { Name = "午休", StartTime = "12:00:00", EndTime = "13:00:00", TypeIcon = "\uE8FD" },
            new TimePointListItem { Name = "下午工作", StartTime = "13:00:00", EndTime = "18:00:00", TypeIcon = "\uE47A" }
        };

        public TimeScheduleEditor()
        {
            InitializeComponent();

            // 设置数据上下文（临时方案）
            this.DataContext = this;
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            // TODO: 实现保存逻辑
        }
    }
}
