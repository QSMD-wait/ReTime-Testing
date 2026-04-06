using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
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
    public class ScheduleItemListItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string _startTime = "";
        public string StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
            }
        }
        public string _endTime = "";
        public string EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
            }
        }
        public string TypeIcon { get; set; } = "\uE787";
        public bool IsTimePoint { get; set; }
        public ProgressStateType ToState { get; set; }

        // 验证错误信息
        private string _startTimeError = "";
        public string StartTimeError
        {
            get => _startTimeError;
            set
            {
                _startTimeError = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasStartTimeError));
            }
        }

        private string _endTimeError = "";
        public string EndTimeError
        {
            get => _endTimeError;
            set
            {
                _endTimeError = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasEndTimeError));
            }
        }

        public bool HasStartTimeError => !string.IsNullOrEmpty(StartTimeError);
        public bool HasEndTimeError => !string.IsNullOrEmpty(EndTimeError);
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

            // 加载后进行初始验证
            ValidateAllItems();
        }

        /// <summary>
        /// 验证所有列表项（时间重叠、结束时间<开始时间）
        /// </summary>
        private void ValidateAllItems()
        {
            // 清空所有错误
            foreach (var item in ScheduleItems)
            {
                item.StartTimeError = "";
                item.EndTimeError = "";
            }

            var segments = ScheduleItems.Where(i => !i.IsTimePoint).ToList();
            var timePoints = ScheduleItems.Where(i => i.IsTimePoint).ToList();

            // 验证时间段结束时间 >= 开始时间
            foreach (var seg in segments)
            {
                if (!string.IsNullOrEmpty(seg.StartTime) && !string.IsNullOrEmpty(seg.EndTime))
                {
                    try
                    {
                        var start = TimeSpan.Parse(seg.StartTime);
                        var end = TimeSpan.Parse(seg.EndTime);
                        if (end < start)
                        {
                            seg.EndTimeError = "结束时间不能早于开始时间";
                        }
                    }
                    catch
                    {
                        seg.StartTimeError = "时间格式无效";
                    }
                }
            }

            // 验证时间段重叠（只标记有问题的项）
            for (int i = 0; i < segments.Count; i++)
            {
                for (int j = i + 1; j < segments.Count; j++)
                {
                    var a = segments[i];
                    var b = segments[j];

                    if (!TryParseTime(a.StartTime, out var aStart) ||
                        !TryParseTime(a.EndTime, out var aEnd) ||
                        !TryParseTime(b.StartTime, out var bStart) ||
                        !TryParseTime(b.EndTime, out var bEnd))
                        continue;

                    // 处理跨午夜
                    if (aEnd < aStart) aEnd = aEnd.Add(TimeSpan.FromDays(1));
                    if (bEnd < bStart) bEnd = bEnd.Add(TimeSpan.FromDays(1));

                    // 允许边界重合：a.End == b.Start 或 b.End == a.Start
                    if (aEnd > bStart && bStart < aEnd && aStart < bEnd && bStart < aEnd)
                    {
                        a.StartTimeError = "与其他时间段重叠";
                        b.StartTimeError = "与其他时间段重叠";
                    }
                }
            }

            // 验证时间点不在时间段内部
            foreach (var tp in timePoints)
            {
                if (!TryParseTime(tp.StartTime, out var tpTime))
                    continue;

                foreach (var seg in segments)
                {
                    if (!TryParseTime(seg.StartTime, out var segStart) ||
                        !TryParseTime(seg.EndTime, out var segEnd))
                        continue;

                    if (segEnd < segStart) segEnd = segEnd.Add(TimeSpan.FromDays(1));

                    // 时间点在时间段内部（注意边界）
                    if (tpTime > segStart && tpTime < segEnd)
                    {
                        tp.StartTimeError = "位于时间段内部";
                    }
                }
            }
        }

        private bool TryParseTime(string timeString, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (string.IsNullOrEmpty(timeString)) return false;
            return TimeSpan.TryParse(timeString, out result);
        }

        private bool IsOverlap(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
        {
            return start1 < end2 && start2 < end1;
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

            string startTime = "09:00:00";
            string endTime = "10:00:00";

            // 如果选中了时间段，使用该时间段的结束时间作为起始时间
            if (SelectedScheduleItem != null && !SelectedScheduleItem.IsTimePoint && !string.IsNullOrEmpty(SelectedScheduleItem.EndTime))
            {
                try
                {
                    var baseTime = TimeSpan.Parse(SelectedScheduleItem.EndTime);
                    startTime = baseTime.ToString(@"hh\:mm\:ss");
                    endTime = baseTime.Add(TimeSpan.FromMinutes(10)).ToString(@"hh\:mm\:ss");
                }
                catch
                {
                    // 解析失败，使用默认值
                }
            }

            var newSegment = new TimeScheduleItem
            {
                Id = $"segment_{DateTime.Now:yyyyMMddHHmmss}",
                Name = "新时间段",
                StartTime = startTime,
                EndTime = endTime
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

            string time = "09:00:00";

            // 如果选中了时间点或时间段，使用该时间/结束时间作为时间
            if (SelectedScheduleItem != null && !string.IsNullOrEmpty(SelectedScheduleItem.StartTime))
            {
                try
                {
                    var baseTime = TimeSpan.Parse(SelectedScheduleItem.StartTime);
                    time = baseTime.ToString(@"hh\:mm\:ss");
                }
                catch
                {
                    // 解析失败，使用默认值
                }
            }

            var newTimePoint = new CustomTimePoint
            {
                Id = $"tp_{DateTime.Now:yyyyMMddHHmmss}",
                Name = "新时间点",
                Time = time,
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
            ValidateAllItems();
        }

        /// <summary>
        /// 时间输入框文本变化时触发验证
        /// </summary>
        private void OnTimeTextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateAllItems();
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

            // 先验证
            ValidateAllItems();

            // 检查是否有验证错误
            var hasError = ScheduleItems.Any(i => i.HasStartTimeError || i.HasEndTimeError);
            if (hasError)
            {
                SaveValidationInfoBar.Message = "存在验证错误，请修正后再保存";
                SaveValidationInfoBar.IsOpen = true;
                return;
            }

            // 将 UI 修改同步回 _currentSchedule
            _currentSchedule.Schedules ??= new List<TimeScheduleItem>();
            _currentSchedule.TimePoints ??= new List<CustomTimePoint>();
            _currentSchedule.Schedules.Clear();
            _currentSchedule.TimePoints.Clear();

            foreach (var item in ScheduleItems)
            {
                if (item.IsTimePoint)
                {
                    _currentSchedule.TimePoints.Add(new CustomTimePoint
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Time = item.StartTime,
                        ToState = item.ToState
                    });
                }
                else
                {
                    _currentSchedule.Schedules.Add(new TimeScheduleItem
                    {
                        Id = item.Id,
                        Name = item.Name,
                        StartTime = item.StartTime,
                        EndTime = item.EndTime
                    });
                }
            }

            // 使用完整验证器进行最终验证
            var validator = new TimeScheduleValidator();
            var result = validator.Validate(_currentSchedule);
            if (!result.IsValid)
            {
                SaveValidationInfoBar.Message = string.Join("\n", result.Errors);
                SaveValidationInfoBar.IsOpen = true;
                return;
            }

            _scheduleManager.SaveSchedule(_currentSchedule);

            // 保存成功后关闭 InfoBar
            SaveValidationInfoBar.IsOpen = false;
        }
    }
}
