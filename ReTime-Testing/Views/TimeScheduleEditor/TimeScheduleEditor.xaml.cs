using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Extensions.DependencyInjection;
using ReTime_Testing.Controls;
using ReTime_Testing.Helpers;
using ReTime_Testing.Models;
using ReTime_Testing.Models.UI;
using ReTime_Testing.Services;
using ReTime_Testing.ViewModels.TimeScheduleEditor;

namespace ReTime_Testing.Views.TimeScheduleEditor
{
    public partial class TimeScheduleEditor : Window
    {
        private readonly TimeScheduleEditorViewModel _viewModel;
        private bool _isWindowClosing = false;
        private bool _forceRealClose = false;
        private bool _isCloseFlowActive = false;
        private ContentDialog? _activeDialog = null;

        public TimeScheduleEditor()
        {
            InitializeComponent();

            var app = Application.Current as App;
            var services = app?.Services ?? throw new InvalidOperationException("DI 容器未初始化");

            _viewModel = services.GetRequiredService<TimeScheduleEditorViewModel>();

            ToastOverlayControl.AttachToHost(this);

            this.DataContext = _viewModel;

            _viewModel.ForceSaveConfirmRequested += OnForceSaveConfirmRequested;
            _viewModel.ToastRequested += OnToastRequested;
            _viewModel.CreateGroupNameRequested += OnCreateGroupNameRequested;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            GroupDrawer.AttachViewModel(_viewModel);
            WeekArranger.AttachViewModel(_viewModel);
            InfoDrawerHost.DrawerStateChanged += OnDrawerStateChanged;

            this.Closing += OnWindowClosing;
        }

        private async Task<bool> OnForceSaveConfirmRequested(string title, List<string> errors)
        {
            if (_isWindowClosing) return false;

            var errorText = string.Join("\n", errors.Take(5));
            if (errors.Count > 5)
            {
                errorText += $"\n...还有 {errors.Count - 5} 个错误";
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "以下验证错误将被忽略：",
                            Margin = new Thickness(0, 0, 0, 8)
                        },
                        new ScrollViewer
                        {
                            MaxHeight = 200,
                            Content = new TextBlock
                            {
                                Text = errorText,
                                TextWrapping = TextWrapping.Wrap,
                                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C42B1C"))
                            }
                        }
                    
                    }
                },
                PrimaryButtonText = "强制保存",
                CloseButtonText = "返回修改",
                DefaultButton = ContentDialogButton.Close,
                IsShadowEnabled = false
            };

            _activeDialog = dialog;
            var result = await dialog.ShowAsync();
            _activeDialog = null;

            return result == ContentDialogResult.Primary;
        }

        private void OnToastRequested(ToastMessage message)
        {
            Dispatcher.BeginInvoke(() =>
            {
                this.ShowToast(message);
            });
        }

        private void OnScheduleListPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListView listView)
            {
                var point = e.GetPosition(listView);
                var hitResult = VisualTreeHelper.HitTest(listView, point);
                if (hitResult != null)
                {
                    var listViewItem = FindAncestor<System.Windows.Controls.ListViewItem>(hitResult.VisualHit);
                    if (listViewItem != null)
                    {
                        listViewItem.IsSelected = true;
                    }
                }
            }
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T result)
                    return result;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private async Task<string?> OnCreateGroupNameRequested(string defaultName)
        {
            if (_isWindowClosing) return null;

            var nameBox = new TextBox
            {
                Text = defaultName,
                MinWidth = 250
            };

            var dialog = new ContentDialog
            {
                Title = "新建计划表组",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "请输入组名称：", Margin = new Thickness(0, 0, 0, 8) },
                        nameBox
                    }
                },
                PrimaryButtonText = "创建",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                IsShadowEnabled = false
            };

            _activeDialog = dialog;
            var result = await dialog.ShowAsync();
            _activeDialog = null;

            if (_isWindowClosing) return null;

            if (result == ContentDialogResult.Primary)
            {
                var name = nameBox.Text?.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            return null;
        }

        /// <summary>
        /// 窗口从后台隐藏状态再次显示前调用：
        /// 刷新计划表列表保证与磁盘一致；存在未保存更改时跳过刷新以保留编辑现场
        /// </summary>
        public void PrepareForShow()
        {
            if (!_viewModel.HasAnyUnpersistedChanges)
            {
                _viewModel.RefreshScheduleList();
            }
        }

        private async void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            // 提交一级编排页与二级编辑抽屉中尚未触发的防抖保存，避免关闭时丢失 500ms 内的修改
            WeekArranger.FlushPendingSave();
            GroupDrawer.FlushPendingSave();

            // 应用退出（Shutdown 已启动）或强制关闭时放行真正关闭
            if (_forceRealClose ||
                (Application.Current?.Dispatcher.HasShutdownStarted ?? false))
            {
                _isWindowClosing = true;
                return;
            }

            // 常驻后台模式：拦截关闭请求改为隐藏窗口，
            // 保留实例避免下次打开时重建视觉树并重播列表入场动画（复选框闪烁）
            e.Cancel = true;

            // 已有弹窗或关闭确认流程进行中时忽略新的关闭请求
            if (_activeDialog != null || _isCloseFlowActive) return;

            if (!_viewModel.HasAnyUnpersistedChanges)
            {
                Hide();
                return;
            }

            _isCloseFlowActive = true;
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "保存更改",
                    Content = "您有未持久化的更改，是否保存？",
                    PrimaryButtonText = "保存",
                    SecondaryButtonText = "不保存",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary,
                    IsShadowEnabled = false
                };

                _activeDialog = dialog;
                var result = await dialog.ShowAsync();
                _activeDialog = null;

                if (result == ContentDialogResult.Primary)
                {
                    if (_viewModel.TryAutoSaveAllBeforeLeave())
                    {
                        Hide();
                    }
                    else
                    {
                        var forceDialog = new ContentDialog
                        {
                            Title = "验证错误",
                            Content = "部分计划表存在验证错误，无法自动保存。\n是否强制保存所有更改？",
                            PrimaryButtonText = "强制保存",
                            SecondaryButtonText = "丢弃更改",
                            CloseButtonText = "取消",
                            DefaultButton = ContentDialogButton.Close,
                            IsShadowEnabled = false
                        };

                        _activeDialog = forceDialog;
                        var forceResult = await forceDialog.ShowAsync();
                        _activeDialog = null;

                        if (forceResult == ContentDialogResult.Primary)
                        {
                            _viewModel.ForceSaveAll();
                            Hide();
                        }
                        else if (forceResult == ContentDialogResult.Secondary)
                        {
                            _viewModel.DiscardAllUnpersistedChanges();
                            Hide();
                        }
                    }
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    _viewModel.DiscardAllUnpersistedChanges();
                    Hide();
                }
                // 取消 → 保持窗口打开
            }
            finally
            {
                _isCloseFlowActive = false;
            }
        }

        private async void OnReloadButtonClick(object sender, RoutedEventArgs e)
        {
            if (_isWindowClosing) return;

            var items = _viewModel.BuildScheduleListItems();
            var listView = CreateScheduleListView(items);
            var dialog = CreateSelectScheduleDialog(listView);

            _activeDialog = dialog;
            var result = await dialog.ShowAsync();
            _activeDialog = null;

            if (_isWindowClosing) return;

            if (result == ContentDialogResult.Primary)
            {
                return;
            }

            if (listView.SelectedItem is ScheduleListItem selectedItem)
            {
                _viewModel.ApplyScheduleSelection(selectedItem);
                var (success, errorMessage) = await _viewModel.HotReloadScheduleAsync(selectedItem.Id);

                if (_isWindowClosing) return;

                if (success)
                {
                    var confirmDialog = new ContentDialog
                    {
                        Title = "切换成功",
                        Content = $"已切换到时间计划表 \"{selectedItem.Name}\"\n\n已重启调度并应用新的时间计划表",
                        CloseButtonText = "确定",
                        DefaultButton = ContentDialogButton.Close,
                        IsShadowEnabled = false
                    };
                    _activeDialog = confirmDialog;
                    await confirmDialog.ShowAsync();
                    _activeDialog = null;
                }
                else
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "切换失败",
                        Content = $"切换到时间计划表 \"{selectedItem.Name}\" 失败\n\n错误：{errorMessage}",
                        CloseButtonText = "确定",
                        DefaultButton = ContentDialogButton.Close,
                        IsShadowEnabled = false
                    };
                    _activeDialog = errorDialog;
                    await errorDialog.ShowAsync();
                    _activeDialog = null;
                }
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
                _activeDialog = warnDialog;
                await warnDialog.ShowAsync();
                _activeDialog = null;
            }
        }

        private System.Windows.Controls.ListView CreateScheduleListView(System.Collections.Generic.List<ScheduleListItem> items)
        {
            var listView = new System.Windows.Controls.ListView
            {
                ItemsSource = items,
                SelectionMode = SelectionMode.Single,
                MinWidth = 300,
                MinHeight = 200,
                Margin = new Thickness(0, 8, 0, 0)
            };

            listView.ItemTemplate = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Grid));
            factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            var textBlock = new FrameworkElementFactory(typeof(TextBlock));
            textBlock.SetValue(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));
            textBlock.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(textBlock);
            listView.ItemTemplate.VisualTree = factory;

            var currentItem = items.FirstOrDefault(i => i.Id == _viewModel.SelectedSchedule?.Id);
            if (currentItem != null)
            {
                listView.SelectedItem = currentItem;
            }

            return listView;
        }

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

        private void OnEditScheduleInfoClick(object sender, RoutedEventArgs e)
        {
            if (_isWindowClosing) return;
            var schedule = _viewModel.SelectedSchedule;
            if (schedule == null) return;

            InfoDrawer.LoadSchedule(schedule);
            InfoDrawer.SaveCompleted -= OnInfoDrawerSaveCompleted;
            InfoDrawer.SaveCompleted += OnInfoDrawerSaveCompleted;

            GroupDrawer.Visibility = Visibility.Collapsed;
            InfoDrawer.Visibility = Visibility.Visible;
            InfoDrawerHost.IsDrawerOpen = true;
        }

        private void OnInfoDrawerSaveCompleted(string newName, string scheduleId)
        {
            this.ShowSuccessToast("保存成功", $"计划表 \"{newName}\" 信息已更新");
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimeScheduleEditorViewModel.SelectedGroup))
            {
                var groupData = _viewModel.GetSelectedGroupData();
                if (groupData != null)
                {
                    GroupDrawer.LoadGroup(groupData);
                    WeekArranger.LoadGroup(groupData);
                }
                else
                {
                    WeekArranger.Clear();
                }
            }
        }

        /// <summary>
        /// 抽屉开合协同：打开期间禁用一级编排页（避免双写），
        /// 关闭时先提交抽屉防抖保存再让编排页重载，同步轮换配置等改动
        /// </summary>
        private void OnDrawerStateChanged(object? sender, DrawerStateChangedEventArgs e)
        {
            if (e.IsOpen)
            {
                WeekArranger.IsEnabled = false;
                return;
            }

            GroupDrawer.FlushPendingSave();
            WeekArranger.IsEnabled = true;
            WeekArranger.Reload();
        }

        private void OnEditGroupClick(object sender, RoutedEventArgs e)
        {
            if (_isWindowClosing) return;

            // 先提交一级编排页的防抖保存，保证二级抽屉读取到最新数据
            WeekArranger.FlushPendingSave();

            var groupData = _viewModel.GetSelectedGroupData();
            if (groupData == null) return;

            GroupDrawer.LoadGroup(groupData);
            GroupDrawer.SaveCompleted -= OnGroupDrawerSaveCompleted;
            GroupDrawer.SaveCompleted += OnGroupDrawerSaveCompleted;

            InfoDrawer.Visibility = Visibility.Collapsed;
            GroupDrawer.Visibility = Visibility.Visible;
            InfoDrawerHost.IsDrawerOpen = true;
        }

        private void OnGroupDrawerSaveCompleted(string groupId, string groupName)
        {
            this.ShowSuccessToast("保存成功", $"表组 \"{groupName}\" 信息已更新");
        }
    }
}