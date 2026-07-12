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
using ReTime_Testing.Models.UI;
using ReTime_Testing.ViewModels.TimeScheduleEditor;

namespace ReTime_Testing.Views.TimeScheduleEditor
{
    public partial class TimeScheduleEditor : Window
    {
        private readonly TimeScheduleEditorViewModel _viewModel;
        private bool _isWindowClosing = false;
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

        private async void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            _isWindowClosing = true;

            if (_activeDialog != null)
            {
                _activeDialog.Hide();
                _activeDialog = null;
            }

            if (!_viewModel.HasAnyUnpersistedChanges)
                return;

            e.Cancel = true;
            _isWindowClosing = false;

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

            if (_isWindowClosing) return;

            if (result == ContentDialogResult.Primary)
            {
                if (_viewModel.TryAutoSaveAllBeforeLeave())
                {
                    this.Close();
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

                    if (_isWindowClosing) return;

                    if (forceResult == ContentDialogResult.Primary)
                    {
                        _viewModel.ForceSaveAll();
                        this.Close();
                    }
                    else if (forceResult == ContentDialogResult.Secondary)
                    {
                        _viewModel.DiscardAllUnpersistedChanges();
                        this.Close();
                    }
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                _viewModel.DiscardAllUnpersistedChanges();
                this.Close();
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
    }
}