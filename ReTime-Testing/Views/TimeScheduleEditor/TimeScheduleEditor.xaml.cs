using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// 统一列表项（时间段+时间点）
    /// </summary>
    public class ScheduleItemListItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string TypeIcon { get; set; } = "\uE787";
        public bool IsTimePoint { get; set; }
        public ProgressStateType ToState { get; set; }
    }

    /// <summary>
    /// TimeScheduleEditor.xaml 的交互逻辑
    /// </summary>
    public partial class TimeScheduleEditor : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private readonly TimeScheduleManager _scheduleManager;

        // 当前编辑的计划表（内存中）
        private TimeSchedule? _currentSchedule;

        // 计划表列表
        public ObservableCollection<ScheduleListItem> Schedules { get; } = new();

        // 时间段+时间点统一列表
        public ObservableCollection<ScheduleItemListItem> ScheduleItems { get; } = new();

        // 当前选中的计划表
        private ScheduleListItem? _selectedSchedule;
        public ScheduleListItem? SelectedSchedule
        {
            get => _selectedSchedule;
            set
            {
                _selectedSchedule = value;
                // 加载选中的计划表到内存
                if (value != null)
                {
                    _currentSchedule = _scheduleManager.LoadSchedule(value.Id);
                }
                else
                {
                    _currentSchedule = null;
                }
                LoadScheduleItems();
            }
        }

        // 当前选中的时间段或时间点
        private ScheduleItemListItem? _selectedScheduleItem;
        public ScheduleItemListItem? SelectedScheduleItem
        {
            get => _selectedScheduleItem;
            set
            {
                _selectedScheduleItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditPanelVisibility));
                OnPropertyChanged(nameof(EmptyStateVisibility));
                OnPropertyChanged(nameof(IsSegmentSelected));
                OnPropertyChanged(nameof(IsTimePointSelected));
            }
        }

        /// <summary>
        /// 编辑面板可见性
        /// </summary>
        public Visibility EditPanelVisibility => SelectedScheduleItem != null
            ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 空状态占位可见性
        /// </summary>
        public Visibility EmptyStateVisibility => SelectedScheduleItem == null
            ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 是否选中时间段（非时间点）
        /// </summary>
        public bool IsSegmentSelected => SelectedScheduleItem != null && !SelectedScheduleItem.IsTimePoint;

        /// <summary>
        /// 是否选中时间点
        /// </summary>
        public bool IsTimePointSelected => SelectedScheduleItem != null && SelectedScheduleItem.IsTimePoint;

        /// <summary>
        /// 目标状态选项列表
        /// </summary>
        public Array ToStateOptions => Enum.GetValues(typeof(ProgressStateType));

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
        /// 加载选中计划表的时间段和时间点（混合显示，按起始时间排序）
        /// </summary>
        private void LoadScheduleItems()
        {
            ScheduleItems.Clear();

            if (_currentSchedule == null) return;

            var items = new List<ScheduleItemListItem>();

            // 加载时间段
            if (_currentSchedule.Schedules != null)
            {
                foreach (var item in _currentSchedule.Schedules)
                {
                    items.Add(new ScheduleItemListItem
                    {
                        Id = item.Id,
                        Name = item.Name,
                        StartTime = item.StartTime,
                        EndTime = item.EndTime,
                        TypeIcon = "\uE787",
                        IsTimePoint = false
                    });
                }
            }

            // 加载时间点
            if (_currentSchedule.TimePoints != null)
            {
                foreach (var point in _currentSchedule.TimePoints)
                {
                    items.Add(new ScheduleItemListItem
                    {
                        Id = point.Id,
                        Name = point.Name,
                        StartTime = point.Time,
                        TypeIcon = "\uE823",
                        IsTimePoint = true,
                        ToState = point.ToState
                    });
                }
            }

            // 按起始时间排序后添加到列表
            var sortedItems = items.OrderBy(i => TimeSpan.Parse(i.StartTime)).ToList();
            foreach (var item in sortedItems)
            {
                ScheduleItems.Add(item);
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
            if (_currentSchedule == null) return;

            var newSegment = new TimeScheduleItem
            {
                Id = $"segment_{DateTime.Now:yyyyMMddHHmmss}",
                Name = "新时间段",
                StartTime = "09:00:00",
                EndTime = "12:00:00"
            };

            _currentSchedule.Schedules ??= new List<TimeScheduleItem>();
            _currentSchedule.Schedules.Add(newSegment);
            LoadScheduleItems();
        }

        /// <summary>
        /// 添加时间点
        /// </summary>
        private void OnAddTimePointClick(object sender, RoutedEventArgs e)
        {
            if (_currentSchedule == null) return;

            var newTimePoint = new CustomTimePoint
            {
                Id = $"tp_{DateTime.Now:yyyyMMddHHmmss}",
                Name = "新时间点",
                Time = "09:00:00",
                ToState = ProgressStateType.Success
            };

            _currentSchedule.TimePoints ??= new List<CustomTimePoint>();
            _currentSchedule.TimePoints.Add(newTimePoint);
            LoadScheduleItems();
        }

        /// <summary>
        /// 删除选中的时间段或时间点
        /// </summary>
        private void OnDeleteScheduleItemClick(object sender, RoutedEventArgs e)
        {
            if (_currentSchedule == null || _selectedScheduleItem == null) return;

            if (_selectedScheduleItem.IsTimePoint)
            {
                var point = _currentSchedule.TimePoints?.FirstOrDefault(p => p.Id == _selectedScheduleItem.Id);
                if (point != null)
                {
                    _currentSchedule.TimePoints!.Remove(point);
                }
            }
            else
            {
                var segment = _currentSchedule.Schedules?.FirstOrDefault(s => s.Id == _selectedScheduleItem.Id);
                if (segment != null)
                {
                    _currentSchedule.Schedules!.Remove(segment);
                }
            }

            LoadScheduleItems();
        }

        /// <summary>
        /// 重新排序（按开始时间）
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
            if (_currentSchedule == null) return;

            _scheduleManager.SaveSchedule(_currentSchedule);
        }
    }
}
