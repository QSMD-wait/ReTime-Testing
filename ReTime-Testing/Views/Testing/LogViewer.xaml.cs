using System.Collections.Specialized;
using System.Windows;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.Testing
{
    /// <summary>
    /// 日志查看器窗口
    /// 展示 Logger 内存缓冲中的实时日志，支持等级/关键字过滤
    /// </summary>
    public partial class LogViewer : Window
    {
        private readonly LogViewerViewModel _viewModel;

        public LogViewer()
        {
            InitializeComponent();

            _viewModel = new LogViewerViewModel();
            DataContext = _viewModel;

            _viewModel.Entries.CollectionChanged += OnEntriesChanged;
        }

        private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_viewModel.AutoScroll) return;

            Dispatcher.BeginInvoke(() =>
            {
                if (LogGrid.Items.Count > 0)
                {
                    LogGrid.ScrollIntoView(LogGrid.Items[LogGrid.Items.Count - 1]);
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _viewModel.Entries.CollectionChanged -= OnEntriesChanged;
            _viewModel.Dispose();
        }
    }
}