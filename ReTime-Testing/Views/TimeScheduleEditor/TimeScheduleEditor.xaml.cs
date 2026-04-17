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
        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasChanges));
            }
        }

        public bool HasChanges { get; private set; }

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
    /// 时间格式验证器
    /// </summary>
    public static class TimeFormatValidator
    {
        // 匹配 HH:mm:ss 格式（小时:分钟:秒）
        private static readonly System.Text.RegularExpressions.Regex TimeFormatRegex =
            new(@"^(\d{1,2}):([0-5]?\d):([0-5]?\d)$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// 验证时间格式是否为 HH:mm:ss
        /// </summary>
        public static bool IsValidFormat(string? timeString)
        {
            if (string.IsNullOrEmpty(timeString)) return false;
            return TimeFormatRegex.IsMatch(timeString);
        }
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

        // 是否有未保存的更改
        private bool _hasUnsavedChanges = false;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set
            {
                _hasUnsavedChanges = value;
                OnPropertyChanged();
            }
        }

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

            // 订阅Closing事件
            this.Closing += OnWindowClosing;
        }

        /// <summary>
        /// 窗口关闭时检查未保存的更改
        /// </summary>
        private async void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            if (!HasUnsavedChanges)
                return;

            e.Cancel = true; // 阻止默认关闭

            var dialog = new ContentDialog
            {
                Title = "保存更改",
                Content = "您有未保存的更改，是否保存？",
                PrimaryButtonText = "保存",
                SecondaryButtonText = "不保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                IsShadowEnabled = false
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // 验证并保存（使用统一的验证方法）
                if (ValidateAndSave())
                {
                    HasUnsavedChanges = false;
                    this.Close();
                }
                // 验证失败，保持窗口打开
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // 不保存，直接关闭
                HasUnsavedChanges = false;
                this.Close();
            }
            // 取消：什么都不做，保持窗口打开
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
                    items.Add(ScheduleItemConverter.ToListItem(item));
                }
            }

            // 加载时间点
            if (_currentSchedule.TimePoints != null)
            {
                foreach (var point in _currentSchedule.TimePoints)
                {
                    items.Add(ScheduleItemConverter.ToListItem(point));
                }
            }

            // 按起始时间排序后添加到列表
            var sortedItems = items
                .Where(i => TryParseTime(i.StartTime, out _))  // 跳过无效时间
                .OrderBy(i => TimeSpan.Parse(i.StartTime))
                .ToList();
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

            // 验证时间格式（先检查空值，再检查格式，最后检查逻辑）
            foreach (var seg in segments)
            {
                // 检查空值
                bool hasStartError = false;
                bool hasEndError = false;

                if (string.IsNullOrEmpty(seg.StartTime))
                {
                    seg.StartTimeError = "不能为空";
                    hasStartError = true;
                }
                else if (!TimeFormatValidator.IsValidFormat(seg.StartTime))
                {
                    seg.StartTimeError = "格式应为 HH:mm:ss";
                    hasStartError = true;
                }

                if (string.IsNullOrEmpty(seg.EndTime))
                {
                    seg.EndTimeError = "不能为空";
                    hasEndError = true;
                }
                else if (!TimeFormatValidator.IsValidFormat(seg.EndTime))
                {
                    seg.EndTimeError = "格式应为 HH:mm:ss";
                    hasEndError = true;
                }

                // 格式正确后，验证结束时间 >= 开始时间
                if (!hasStartError && !hasEndError && !string.IsNullOrEmpty(seg.StartTime) && !string.IsNullOrEmpty(seg.EndTime))
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

            // 验证时间点格式
            foreach (var tp in timePoints)
            {
                if (string.IsNullOrEmpty(tp.StartTime))
                {
                    tp.StartTimeError = "不能为空";
                }
                else if (!TimeFormatValidator.IsValidFormat(tp.StartTime))
                {
                    tp.StartTimeError = "格式应为 HH:mm:ss";
                }
            }

            // 验证时间段重叠（只标记有问题的项）
            for (int i = 0; i < segments.Count; i++)
            {
                for (int j = i + 1; j < segments.Count; j++)
                {
                    var a = segments[i];
                    var b = segments[j];

                    // 跳过格式无效的项
                    if (!TimeFormatValidator.IsValidFormat(a.StartTime) ||
                        !TimeFormatValidator.IsValidFormat(a.EndTime) ||
                        !TimeFormatValidator.IsValidFormat(b.StartTime) ||
                        !TimeFormatValidator.IsValidFormat(b.EndTime))
                        continue;

                    if (!TryParseTime(a.StartTime, out var aStart) ||
                        !TryParseTime(a.EndTime, out var aEnd) ||
                        !TryParseTime(b.StartTime, out var bStart) ||
                        !TryParseTime(b.EndTime, out var bEnd))
                        continue;

                    // 处理跨午夜
                    if (aEnd < aStart) aEnd = aEnd.Add(TimeSpan.FromDays(1));
                    if (bEnd < bStart) bEnd = bEnd.Add(TimeSpan.FromDays(1));

                    // 允许边界重合：a.End == b.Start 或 b.End == a.Start 时不视为重叠
                    // 重叠条件：a.Start < b.End && b.Start < a.End
                    if (aStart < bEnd && bStart < aEnd)
                    {
                        a.StartTimeError = "与其他时间段重叠";
                        b.StartTimeError = "与其他时间段重叠";
                    }
                }
            }

            // 验证时间点不在时间段内部
            foreach (var tp in timePoints)
            {
                // 跳过格式无效的时间点
                if (!TimeFormatValidator.IsValidFormat(tp.StartTime))
                    continue;

                if (!TryParseTime(tp.StartTime, out var tpTime))
                    continue;

                foreach (var seg in segments)
                {
                    // 跳过格式无效的时间段
                    if (!TimeFormatValidator.IsValidFormat(seg.StartTime) ||
                        !TimeFormatValidator.IsValidFormat(seg.EndTime))
                        continue;

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

        /// <summary>
        /// 计算新时间项的默认时间
        /// </summary>
        /// <param name="isTimePoint">是否为时间点</param>
        /// <param name="defaultStartTime">默认起始时间（时间段）或时间（时间点）</param>
        /// <param name="defaultEndTime">默认结束时间（仅时间段）</param>
        private void ComputeDefaultTime(bool isTimePoint, out string defaultStartTime, out string defaultEndTime)
        {
            defaultStartTime = "09:00:00";
            defaultEndTime = "10:00:00";

            // 如果选中了时间项，使用其时间作为默认值
            if (SelectedScheduleItem != null && !string.IsNullOrEmpty(SelectedScheduleItem.StartTime))
            {
                if (TryParseTime(SelectedScheduleItem.StartTime, out var baseTime))
                {
                    defaultStartTime = baseTime.ToString(@"hh\:mm\:ss");
                    if (!isTimePoint)
                    {
                        // 时间段：结束时间 = 起始时间 + 10分钟
                        defaultEndTime = baseTime.Add(TimeSpan.FromMinutes(10)).ToString(@"hh\:mm\:ss");
                    }
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
                HasUnsavedChanges = true;
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
                HasUnsavedChanges = true;
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

            // 计算默认时间
            ComputeDefaultTime(isTimePoint: false, out var startTime, out var endTime);

            var newSegment = new ScheduleItemListItem
            {
                Id = $"segment_{DateTime.Now:yyyyMMddHHmmss}",
                Name = "新时间段",
                StartTime = startTime,
                EndTime = endTime,
                IsTimePoint = false
            };

            // 添加到 ViewModel
            ScheduleItems.Add(newSegment);
            HasUnsavedChanges = true;
            SelectedScheduleItem = newSegment;
        }

        /// <summary>
        /// 添加时间点
        /// </summary>
        private void OnAddTimePointClick(object sender, RoutedEventArgs e)
        {
            if (_currentSchedule == null) return;

            // 计算默认时间
            ComputeDefaultTime(isTimePoint: true, out var startTime, out _);

            var newTimePoint = new ScheduleItemListItem
            {
                Id = $"tp_{DateTime.Now:yyyyMMddHHmmss}",
                Name = "新时间点",
                StartTime = startTime,
                IsTimePoint = true,
                ToState = ProgressStateType.Success
            };

            // 添加到 ViewModel
            ScheduleItems.Add(newTimePoint);
            HasUnsavedChanges = true;
            SelectedScheduleItem = newTimePoint;
        }

        /// <summary>
        /// 删除选中的时间段或时间点
        /// </summary>
        private void OnDeleteScheduleItemClick(object sender, RoutedEventArgs e)
        {
            if (_currentSchedule == null || _selectedScheduleItem == null) return;

            // 从 ViewModel 删除
            ScheduleItems.Remove(_selectedScheduleItem);

            // 清空选中
            SelectedScheduleItem = null;
            HasUnsavedChanges = true;
        }

        /// <summary>
        /// 重新排序（按开始时间）
        /// </summary>
        private void OnRefreshOrderClick(object sender, RoutedEventArgs e)
        {
            // 按起始时间排序
            var sortedItems = ScheduleItems
                .Where(i => TryParseTime(i.StartTime, out _))
                .OrderBy(i => TimeSpan.Parse(i.StartTime))
                .ToList();

            // 检查是否需要重新排序
            bool needsReorder = false;
            for (int i = 0; i < sortedItems.Count; i++)
            {
                if (ScheduleItems[i].Id != sortedItems[i].Id)
                {
                    needsReorder = true;
                    break;
                }
            }

            if (needsReorder)
            {
                ScheduleItems.Clear();
                foreach (var item in sortedItems)
                {
                    ScheduleItems.Add(item);
                }
                HasUnsavedChanges = true;
            }

            // 验证所有项
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
        /// 项目名称失去焦点时标记为已修改
        /// </summary>
        private void OnItemNameLostFocus(object sender, RoutedEventArgs e)
        {
            if (_selectedScheduleItem != null)
            {
                HasUnsavedChanges = true;
            }
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
        /// 加载按钮点击事件 - 弹出 ContentDialog 选择计划表
        /// </summary>
        private async void OnReloadButtonClick(object sender, RoutedEventArgs e)
        {
            var items = BuildScheduleListItems();
            var listView = CreateScheduleListView(items);
            var dialog = CreateSelectScheduleDialog(listView);
            await HandleScheduleSelection(dialog, listView, items);
        }

        /// <summary>
        /// 构建计划表列表项
        /// </summary>
        private System.Collections.Generic.List<ScheduleListItem> BuildScheduleListItems()
        {
            var scheduleList = _scheduleManager.GetScheduleList();
            var currentSelectedId = ConfigurationManager.Instance.LoadTimeTopSetting().SelectedScheduleId;

            var items = new System.Collections.Generic.List<ScheduleListItem>();
            foreach (var info in scheduleList)
            {
                items.Add(new ScheduleListItem
                {
                    Id = info.Id,
                    Name = info.Name,
                    IsActivated = info.Id == currentSelectedId
                });
            }
            return items;
        }

        /// <summary>
        /// 创建计划表列表视图
        /// </summary>
        private System.Windows.Controls.ListView CreateScheduleListView(
            System.Collections.Generic.List<ScheduleListItem> items)
        {
            var listView = new System.Windows.Controls.ListView
            {
                ItemsSource = items,
                SelectionMode = System.Windows.Controls.SelectionMode.Single,
                MinWidth = 300,
                MinHeight = 200,
                Margin = new Thickness(0, 8, 0, 0)
            };

            // 设置数据模板
            listView.ItemTemplate = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Grid));
            factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            var textBlock = new FrameworkElementFactory(typeof(TextBlock));
            textBlock.SetValue(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));
            textBlock.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(textBlock);
            listView.ItemTemplate.VisualTree = factory;

            // 选中当前计划表
            var currentItem = items.FirstOrDefault(i => i.Id == _selectedSchedule?.Id);
            if (currentItem != null)
            {
                listView.SelectedItem = currentItem;
            }

            return listView;
        }

        /// <summary>
        /// 创建选择计划表对话框
        /// </summary>
        private ContentDialog CreateSelectScheduleDialog(System.Windows.Controls.ListView listView)
        {
            return new ContentDialog
            {
                Title = "选择时间计划表",
                Content = new ScrollViewer
                {
                    Content = listView,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    MaxHeight = 300
                },
                CloseButtonText = "加载",
                PrimaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
                IsShadowEnabled = false
            };
        }

        /// <summary>
        /// 处理计划表选择结果
        /// </summary>
        private async Task HandleScheduleSelection(
            ContentDialog dialog,
            System.Windows.Controls.ListView listView,
            System.Collections.Generic.List<ScheduleListItem> items)
        {
            var result = await dialog.ShowAsync();

            // 处理结果 - None 是点击加载（默认按钮），Primary 是点击取消
            if (result == ContentDialogResult.Primary)
            {
                return;
            }

            // 用户点击加载
            if (listView.SelectedItem is ScheduleListItem selectedItem)
            {
                var setting = ConfigurationManager.Instance.LoadTimeTopSetting();
                setting.SelectedScheduleId = selectedItem.Id;
                ConfigurationManager.Instance.SaveTimeTopSetting(setting);

                var confirmDialog = new ContentDialog
                {
                    Title = "切换成功",
                    Content = $"已切换到计划表 \"{selectedItem.Name}\"\n\n请重启应用以使更改生效。",
                    CloseButtonText = "确定",
                    DefaultButton = ContentDialogButton.Close,
                    IsShadowEnabled = false
                };
                await confirmDialog.ShowAsync();
            }
            else if (result == ContentDialogResult.None && listView.SelectedItem == null && listView.Items.Count > 0)
            {
                var warnDialog = new ContentDialog
                {
                    Title = "提示",
                    Content = "请选择一个计划表",
                    CloseButtonText = "确定",
                    DefaultButton = ContentDialogButton.Close,
                    IsShadowEnabled = false
                };
                await warnDialog.ShowAsync();
            }
        }

        /// <summary>
        /// 验证并保存（返回是否验证通过）
        /// </summary>
        private bool ValidateAndSave()
        {
            if (_currentSchedule == null) return false;

            // 验证所有项
            ValidateAllItems();

            // 检查是否有验证错误
            bool hasErrors = ScheduleItems.Any(i => i.HasStartTimeError || i.HasEndTimeError);
            if (hasErrors)
            {
                SaveValidationInfoBar.Message = "存在验证错误，请修正后再保存";
                SaveValidationInfoBar.IsOpen = true;
                return false;
            }

            // 将 UI 修改同步回 _currentSchedule（增量更新，保留原有对象的属性）
            _currentSchedule.Schedules ??= new List<TimeScheduleItem>();
            _currentSchedule.TimePoints ??= new List<CustomTimePoint>();

            // 收集 ViewModel 中当前的 ID 集合
            var currentItemIds = ScheduleItems.Select(i => i.Id).ToHashSet();

            // 删除不在 ViewModel 中的项目
            _currentSchedule.Schedules?.RemoveAll(s => !currentItemIds.Contains(s.Id));
            _currentSchedule.TimePoints?.RemoveAll(t => !currentItemIds.Contains(t.Id));

            // 更新或添加项目
            foreach (var item in ScheduleItems)
            {
                if (item.IsTimePoint)
                {
                    // 查找现有时间点并更新
                    var existingPoint = _currentSchedule.TimePoints?.FirstOrDefault(t => t.Id == item.Id);
                    if (existingPoint != null)
                    {
                        existingPoint.Name = item.Name;
                        existingPoint.Time = item.StartTime;
                        existingPoint.ToState = item.ToState;
                    }
                    else
                    {
                        // 新增时间点（保留 Style 等属性）
                        _currentSchedule.TimePoints?.Add(new CustomTimePoint
                        {
                            Id = item.Id,
                            Name = item.Name,
                            Time = item.StartTime,
                            ToState = item.ToState
                        });
                    }
                }
                else
                {
                    // 查找现有时间段并更新
                    var existingSegment = _currentSchedule.Schedules?.FirstOrDefault(s => s.Id == item.Id);
                    if (existingSegment != null)
                    {
                        existingSegment.Name = item.Name;
                        existingSegment.StartTime = item.StartTime;
                        existingSegment.EndTime = item.EndTime;
                    }
                    else
                    {
                        // 新增时间段（保留 Styles 等属性）
                        _currentSchedule.Schedules?.Add(new TimeScheduleItem
                        {
                            Id = item.Id,
                            Name = item.Name,
                            StartTime = item.StartTime,
                            EndTime = item.EndTime
                        });
                    }
                }
            }

            // 使用完整验证器进行最终验证
            var validator = new TimeScheduleValidator();
            var result = validator.Validate(_currentSchedule);
            if (!result.IsValid)
            {
                SaveValidationInfoBar.Message = string.Join("\n", result.Errors);
                SaveValidationInfoBar.IsOpen = true;
                return false;
            }

            _scheduleManager.SaveSchedule(_currentSchedule);

            // 保存成功后关闭 InfoBar
            SaveValidationInfoBar.IsOpen = false;
            return true;
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            ValidateAndSave();
        }
    }
}
