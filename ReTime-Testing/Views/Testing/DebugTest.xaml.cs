using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ReTime_Testing.Helpers;
using ReTime_Testing.Models.UI;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.Testing
{
    public partial class DebugTest : Window
    {
        public DebugTest()
        {
            InitializeComponent();

            if (DataContext is null)
            {
                var app = System.Windows.Application.Current as App;
                var services = app?.Services ?? throw new InvalidOperationException("DI 容器未初始化");

                DataContext = ActivatorUtilities.CreateInstance<DebugTestViewModel>(services);
            }

            ToastOverlayControl.AttachToHost(this);

            if (DataContext is DebugTestViewModel viewModel)
            {
                viewModel.ToastRequested += OnToastRequested;
            }
        }

        private void OnToastRequested(ToastMessage message)
        {
            Dispatcher.BeginInvoke(() =>
            {
                this.ShowToast(message);
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (DataContext is DebugTestViewModel viewModel)
            {
                viewModel.ToastRequested -= OnToastRequested;
                viewModel.Cleanup();
            }
        }
    }
}