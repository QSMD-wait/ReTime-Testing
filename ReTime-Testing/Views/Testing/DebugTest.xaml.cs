using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ReTime_Testing.Controls;
using ReTime_Testing.Helpers;
using ReTime_Testing.Models.UI;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.Testing
{
    public partial class DebugTest : Window
    {
        private DebugTestViewModel? _viewModel;

        public DebugTest()
        {
            InitializeComponent();

            if (DataContext is null)
            {
                var app = System.Windows.Application.Current as App;
                var services = app?.Services ?? throw new InvalidOperationException("DI 容器未初始化");

                DataContext = ActivatorUtilities.CreateInstance<DebugTestViewModel>(services);
            }

            _viewModel = DataContext as DebugTestViewModel;

            ToastOverlayControl.AttachToHost(this);

            if (_viewModel != null)
                _viewModel.ToastRequested += OnToastRequested;

            MainDrawerHost.DrawerStateChanged += OnDrawerStateChanged;

            if (_viewModel?.DrawerTest != null)
                _viewModel.DrawerTest.PropertyChanged += OnDrawerTestPropertyChanged;
        }

        private void OnDrawerTestPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_viewModel?.DrawerTest == null) return;
            if (e.PropertyName == nameof(ViewModels.Testing.DrawerTestViewModel.IsDrawerOpen))
                MainDrawerHost.IsDrawerOpen = _viewModel.DrawerTest.IsDrawerOpen;
            if (e.PropertyName == nameof(ViewModels.Testing.DrawerTestViewModel.DrawerWidth))
                MainDrawerHost.DrawerWidth = _viewModel.DrawerTest.DrawerWidth;
        }

        private void OnDrawerStateChanged(object? sender, DrawerStateChangedEventArgs e)
        {
            if (_viewModel?.DrawerTest != null)
                _viewModel.DrawerTest.IsDrawerOpen = e.IsOpen;
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

            MainDrawerHost.DrawerStateChanged -= OnDrawerStateChanged;

            if (_viewModel?.DrawerTest != null)
                _viewModel.DrawerTest.PropertyChanged -= OnDrawerTestPropertyChanged;

            if (_viewModel != null)
            {
                _viewModel.ToastRequested -= OnToastRequested;
                _viewModel.Cleanup();
            }
        }
    }
}