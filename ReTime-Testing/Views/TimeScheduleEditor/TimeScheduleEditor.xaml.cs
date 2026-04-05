using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using iNKORE.UI.WPF.Modern.Controls;
using ReTime_Testing.Models;
using ReTime_Testing.Services;

namespace ReTime_Testing.Views.TimeScheduleEditor
{
    /// <summary>
    /// 计划表列表项（绑定到ScheduleInfo）
    /// </summary>
    public class ScheduleListItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsActivated { get; set; }
    }

    /// <summary>
    /// 时间段/时间点列表项（统一展示）
    /// </summary>
    public class ScheduleItemListItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string TypeIcon { get; set; } = "\uE787"; // 默认时间段图标
        public bool IsTimePoint { get; set; } // false=时间段, true=时间点
    }

    /// <summary>
    /// TimeScheduleEditor.xaml 的交互逻辑
    /// </summary>
    public partial class TimeScheduleEditor : Window
    {
        private readonly TimeScheduleManager _scheduleManager;

        // 计划表列表
        public ObservableCollection<ScheduleListItem> Schedules { get; } = new();

        // 选中计划表的所有时间段和时间点
        public ObservableCollection<ScheduleItemListItem> ScheduleItems { get; } = new();

        // 当前选中的计划表
        private ScheduleListItem? _selectedSchedule;
        public ScheduleListItem? SelectedSchedule
        {
            get => _selectedSchedule;
            set
            {
                _selectedSchedule = value;
                LoadScheduleItems();
            }
        }

        // 当前选中的时间段/时间点
        private ScheduleItemListItem? _selectedScheduleItem;

        public TimeScheduleEditor()
        {
            InitializeComponent();

            _scheduleManager = TimeScheduleManager.Instance;

            // 加载计划表列表
            RefreshScheduleList();

            // 设置数据上下文
            this.DataContext = this;
        }

        /// <summary>
        /// 刷新计划表列表
        /// </summary>
        private void RefreshScheduleList()
        {
            Schedules.Clear();

            var scheduleList = _scheduleManager.GetScheduleList();
            var currentSelectedId = ConfigurationManager.Instance.LoadTimeTopSetting().SelectedScheduleId;

            foreach (var info in scheduleList)
            {
                Schedules.Add(new ScheduleListItem
                {
                    Id = info.Id,
                    Name = info.Name,
                    IsActivated = info.Id == currentSelectedId
                });
            }

            // 选中第一个或激活的计划表
            if (Schedules.Count > 0 && _selectedSchedule == null)
            {
                var active = Schedules.FirstOrDefault(s => s.IsActivated) ?? Schedules[0];
                SelectedSchedule = active;
            }
        }

        /// <summary>
        /// 加载选中计划表的时间段和时间点
        /// </summary>
        private void LoadScheduleItems()
        {
            ScheduleItems.Clear();

            if (_selectedSchedule == null) return;

            var schedule = _scheduleManager.LoadSchedule(_selectedSchedule.Id);
            if (schedule == null) return;

            // 加载时间段
            if (schedule.Schedules != null)
            {
                foreach (var item in schedule.Schedules)
                {
                    ScheduleItems.Add(new ScheduleItemListItem
                    {
                        Id = item.Id,
                        Name = item.Name,
                        StartTime = item.StartTime,
                        EndTime = item.EndTime,
                        TypeIcon = "\uE787", // 时间段图标
                        IsTimePoint = false
                    });
                }
            }

            // 加载时间点
            if (schedule.TimePoints != null)
            {
                foreach (var point in schedule.TimePoints)
                {
                    ScheduleItems.Add(new ScheduleItemListItem
                    {
                        Id = point.Id,
                        Name = point.Name,
                        StartTime = point.Time,
                        EndTime = "", // 时间点只有单个时间
                        TypeIcon = "\uE823", // 时间点图标
                        IsTimePoint = true
                    });
                }
            }
        }

        /// <summary>
        /// 添加计划表
        /// </summary>
        private void OnAddScheduleClick(object sender, RoutedEventArgs e)
        {
            var newId = $"schedule_{DateTime.Now:yyyyMMddHHmmss}";
            var newName = "新计划表";

            var schedule = _scheduleManager.CreateNewSchedule(newId, newName);
            if (schedule != null)
            {
                RefreshScheduleList();
                // 选中新创建的计划表
                SelectedSchedule = Schedules.FirstOrDefault(s => s.Id == newId);
            }
        }

        /// <summary>
        /// 复制计划表
        /// </summary>
        private void OnCopyScheduleClick(object sender, RoutedEventArgs e)
        {
            if (_selectedSchedule == null) return;

            var newId = $"schedule_{DateTime.Now:yyyyMMddHHmmss}";
            var newSchedule = _scheduleManager.CopySchedule(_selectedSchedule.Id, newId);

            if (newSchedule != null)
            {
                RefreshScheduleList();
                SelectedSchedule = Schedules.FirstOrDefault(s => s.Id == newId);
            }
        }

        /// <summary>
        /// 添加时间段
        /// </summary>
        private void OnAddTimeSegmentClick(object sender, RoutedEventArgs e)
        {
            if (_selectedSchedule == null) return;

            var newSegment = new TimeScheduleItem
            {
                Id = $"segment_{DateTime.Now:yyyyMMddHHmmss}",
                Name = "新时间段",
                StartTime = "09:00:00",
                EndTime = "12:00:00"
            };

            if (_scheduleManager.AddTimeSegment(_selectedSchedule.Id, newSegment))
            {
                LoadScheduleItems();
            }
        }

        /// <summary>
        /// 添加时间点
        /// </summary>
        private void OnAddTimePointClick(object sender, RoutedEventArgs e)
        {
            if (_selectedSchedule == null) return;

            var newTimePoint = new CustomTimePoint
            {
                Id = $"timepoint_{DateTime.Now:yyyyMMddHHmmss}",
                Name = "新时间点",
                Time = "09:00:00",
                ToState = ProgressStateType.Progress
            };

            if (_scheduleManager.AddTimePoint(_selectedSchedule.Id, newTimePoint))
            {
                LoadScheduleItems();
            }
        }

        /// <summary>
        /// 删除选中的时间段或时间点
        /// </summary>
        private void OnDeleteScheduleItemClick(object sender, RoutedEventArgs e)
        {
            if (_selectedSchedule == null || _selectedScheduleItem == null) return;

            bool success;
            if (_selectedScheduleItem.IsTimePoint)
            {
                success = _scheduleManager.RemoveTimePoint(_selectedSchedule.Id, _selectedScheduleItem.Id);
            }
            else
            {
                success = _scheduleManager.RemoveTimeSegment(_selectedSchedule.Id, _selectedScheduleItem.Id);
            }

            if (success)
            {
                LoadScheduleItems();
            }
        }

        /// <summary>
        /// 重新排序时间段（按开始时间）
        /// </summary>
        private void OnRefreshOrderClick(object sender, RoutedEventArgs e)
        {
            // TODO: 实现重新排序逻辑
        }

        /// <summary>
        /// 编辑计划表信息
        /// </summary>
        private void OnEditScheduleInfoClick(object sender, RoutedEventArgs e)
        {
            // TODO: 打开计划表信息编辑对话框
        }

        /// <summary>
        /// 删除计划表
        /// </summary>
        private void OnDeleteScheduleClick(object sender, RoutedEventArgs e)
        {
            if (_selectedSchedule == null) return;

            // 不允许删除默认计划表
            if (_selectedSchedule.Id == "Default")
            {
                return;
            }

            if (_scheduleManager.DeleteSchedule(_selectedSchedule.Id))
            {
                RefreshScheduleList();
                if (Schedules.Count > 0)
                {
                    SelectedSchedule = Schedules[0];
                }
            }
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            // 保存操作由 TimeScheduleManager 自动处理
            // 这里可以添加额外的保存逻辑或提示
        }
    }
}
