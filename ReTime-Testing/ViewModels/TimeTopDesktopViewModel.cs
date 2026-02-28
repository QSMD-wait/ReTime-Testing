using CommunityToolkit.Mvvm.ComponentModel;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReTime_Testing.ViewModels
{
    public partial class TimeTopDesktopViewModel : ObservableObject
    {
        private DispatcherTimer? _timer;
        private const int TimerInterval = 100; // 100ms更新一次

        // 80秒测试流程（毫秒）
        private const int LoadingDuration = 5000;      // 5s
        private const int Progress1Duration = 8000;    // 8s
        private const int PauseDuration = 3000;        // 3s
        private const int Progress2Duration = 8000;    // 8s
        private const int OpacityTestDuration = 5000;  // 5s
        private const int BackgroundTestDuration = 4000; // 4s
        private const int VisibilityTestDuration = 3000; // 3s
        private const int IsEnabledTestDuration = 3000; // 3s
        private const int MinMaxTestDuration = 3000;   // 3s
        private const int ErrorDuration = 4000;        // 4s
        private const int SuccessDuration = 10000;     // 10s

        // 状态
        private enum ProgressState
        {
            Loading,          // 蓝色不确定加载，默认值
            Progress1,        // 蓝色进度 0%→50%，Value测试
            Paused,           // 橙色暂停，50%
            Progress2,        // 蓝色进度 50%→100%
            OpacityTest,      // Opacity 淡入淡出
            BackgroundTest,   // Background 变化
            VisibilityTest,   // Visible→Hidden→Visible
            IsEnabledTest,    // IsEnabled false→true
            MinMaxTest,       // Min=0,Max=200,Value=100
            Error,            // 红色错误状态
            Success           // 绿色进度 0%→100%
        }

        private ProgressState _currentState = ProgressState.Loading;
        private DateTime _stateStartTime;
        private readonly ProgressStateManager _stateManager;

        [ObservableProperty]
        private double _progressValue = 0;

        [ObservableProperty]
        private bool _isIndeterminate = true;

        [ObservableProperty]
        private Brush? _foreground = ProgressColors.DefaultBlue;

        [ObservableProperty]
        private Brush? _background = Brushes.Transparent;

        [ObservableProperty]
        private Visibility _visibility = Visibility.Visible;

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private double _opacity = 1.0;

        [ObservableProperty]
        private double _minimum = 0;

        [ObservableProperty]
        private double _maximum = 100;

        public TimeTopDesktopViewModel()
        {
            _stateStartTime = DateTime.Now;
            _stateManager = new ProgressStateManager();
            _stateManager.OnStateChanged = OnStateChanged;

            StartProgressCycle();
        }

        /// <summary>
        /// 状态变更回调
        /// </summary>
        private void OnStateChanged(ProgressStateConfig config)
        {
            ProgressValue = config.Value;
            IsIndeterminate = config.IsIndeterminate;
            Foreground = config.Foreground;
            Background = config.Background;
            Visibility = config.Visibility;
            IsEnabled = config.IsEnabled;
            Opacity = config.Opacity;
            Minimum = config.Minimum;
            Maximum = config.Maximum;
        }

        private void StartProgressCycle()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(TimerInterval)
            };

            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _stateStartTime;
            var elapsedMs = elapsed.TotalMilliseconds;

            switch (_currentState)
            {
                case ProgressState.Loading:
                    // 蓝色不确定加载 5s，所有默认值
                    ProgressValue = 0;
                    IsIndeterminate = true;
                    Foreground = ProgressColors.DefaultBlue;
                    Background = Brushes.Transparent;
                    Visibility = Visibility.Visible;
                    IsEnabled = true;
                    Opacity = 1.0;
                    Minimum = 0;
                    Maximum = 100;

                    if (elapsedMs >= LoadingDuration)
                    {
                        TransitionToState(ProgressState.Progress1);
                    }
                    break;

                case ProgressState.Progress1:
                    // 蓝色进度 8s（0% → 50%），Value测试
                    IsIndeterminate = false;
                    Foreground = ProgressColors.DefaultBlue;
                    Background = Brushes.Transparent;
                    Visibility = Visibility.Visible;
                    IsEnabled = true;
                    Opacity = 1.0;
                    Minimum = 0;
                    Maximum = 100;

                    var progress1 = (elapsedMs / Progress1Duration) * 50;
                    ProgressValue = Math.Min(progress1, 50);

                    if (elapsedMs >= Progress1Duration)
                    {
                        TransitionToState(ProgressState.Paused);
                    }
                    break;

                case ProgressState.Paused:
                    // 橙色暂停 3s（50%）
                    ProgressValue = 50;
                    IsIndeterminate = false;
                    Foreground = ProgressColors.PauseOrange;
                    Background = Brushes.Transparent;
                    Visibility = Visibility.Visible;
                    IsEnabled = true;
                    Opacity = 1.0;
                    Minimum = 0;
                    Maximum = 100;

                    if (elapsedMs >= PauseDuration)
                    {
                        TransitionToState(ProgressState.Progress2);
                    }
                    break;

                case ProgressState.Progress2:
                    // 蓝色进度 8s（50% → 100%）
                    IsIndeterminate = false;
                    Foreground = ProgressColors.DefaultBlue;
                    Background = Brushes.Transparent;
                    Visibility = Visibility.Visible;
                    IsEnabled = true;
                    Opacity = 1.0;
                    Minimum = 0;
                    Maximum = 100;

                    var progress2 = 50 + (elapsedMs / Progress2Duration) * 50;
                    ProgressValue = Math.Min(progress2, 100);

                    if (elapsedMs >= Progress2Duration)
                    {
                        TransitionToState(ProgressState.OpacityTest);
                    }
                    break;

                case ProgressState.OpacityTest:
                    // Opacity 淡入淡出 5s
                    IsIndeterminate = false;
                    Foreground = ProgressColors.DefaultBlue;
                    Background = Brushes.Transparent;
                    Visibility = Visibility.Visible;
                    IsEnabled = true;
                    Minimum = 0;
                    Maximum = 100;
                    ProgressValue = 100;

                    // 0→2.5s: 1.0→0.5, 2.5s→5s: 0.5→1.0
                    if (elapsedMs < OpacityTestDuration / 2)
                    {
                        Opacity = 1.0 - (elapsedMs / (OpacityTestDuration / 2)) * 0.5;
                    }
                    else
                    {
                        Opacity = 0.5 + ((elapsedMs - OpacityTestDuration / 2) / (OpacityTestDuration / 2)) * 0.5;
                    }

                    if (elapsedMs >= OpacityTestDuration)
                    {
                        Opacity = 1.0;
                        TransitionToState(ProgressState.BackgroundTest);
                    }
                    break;

                case ProgressState.BackgroundTest:
                    // Background 变化 4s
                    IsIndeterminate = false;
                    Foreground = ProgressColors.DefaultBlue;
                    Visibility = Visibility.Visible;
                    IsEnabled = true;
                    Opacity = 1.0;
                    Minimum = 0;
                    Maximum = 100;
                    ProgressValue = 100;

                    // 0→2s: Transparent→Gray, 2s→4s: Gray→Transparent
                    if (elapsedMs < BackgroundTestDuration / 2)
                    {
                        Background = ProgressColors.Gray;
                    }
                    else
                    {
                        Background = Brushes.Transparent;
                    }

                    if (elapsedMs >= BackgroundTestDuration)
                    {
                        TransitionToState(ProgressState.VisibilityTest);
                    }
                    break;

                case ProgressState.VisibilityTest:
                    // Visible→Hidden(1s)→Visible 3s
                    IsIndeterminate = false;
                    Foreground = ProgressColors.DefaultBlue;
                    Background = Brushes.Transparent;
                    IsEnabled = true;
                    Opacity = 1.0;
                    Minimum = 0;
                    Maximum = 100;
                    ProgressValue = 100;

                    // 0→0.5s: Visible, 0.5s→2.5s: Hidden, 2.5s→3s: Visible
                    if (elapsedMs < 500)
                    {
                        Visibility = Visibility.Visible;
                    }
                    else if (elapsedMs < 2500)
                    {
                        Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        Visibility = Visibility.Visible;
                    }

                    if (elapsedMs >= VisibilityTestDuration)
                    {
                        TransitionToState(ProgressState.IsEnabledTest);
                    }
                    break;

                case ProgressState.IsEnabledTest:
                    // IsEnabled false(1.5s)→true 3s
                    IsIndeterminate = false;
                    Foreground = ProgressColors.DefaultBlue;
                    Background = Brushes.Transparent;
                    Visibility = Visibility.Visible;
                    Opacity = 1.0;
                    Minimum = 0;
                    Maximum = 100;
                    ProgressValue = 100;

                    // 0→1.5s: false, 1.5s→3s: true
                    if (elapsedMs < 1500)
                    {
                        IsEnabled = false;
                    }
                    else
                    {
                        IsEnabled = true;
                    }

                    if (elapsedMs >= IsEnabledTestDuration)
                    {
                        TransitionToState(ProgressState.MinMaxTest);
                    }
                    break;

                case ProgressState.MinMaxTest:
                    // Min=0,Max=200,Value=100（50%位置）3s
                    IsIndeterminate = false;
                    Foreground = ProgressColors.DefaultBlue;
                    Background = Brushes.Transparent;
                    Visibility = Visibility.Visible;
                    IsEnabled = true;
                    Opacity = 1.0;
                    Minimum = 0;
                    Maximum = 200;
                    ProgressValue = 100;

                    if (elapsedMs >= MinMaxTestDuration)
                    {
                        // 恢复默认值
                        Minimum = 0;
                        Maximum = 100;
                        TransitionToState(ProgressState.Error);
                    }
                    break;

                case ProgressState.Error:
                    // 红色错误状态 4s
                    ProgressValue = 100;
                    IsIndeterminate = false;
                    Foreground = ProgressColors.ErrorRed;
                    Background = Brushes.Transparent;
                    Visibility = Visibility.Visible;
                    IsEnabled = true;
                    Opacity = 1.0;
                    Minimum = 0;
                    Maximum = 100;

                    if (elapsedMs >= ErrorDuration)
                    {
                        TransitionToState(ProgressState.Success);
                    }
                    break;

                case ProgressState.Success:
                    // 绿色进度 10s（0% → 100%）
                    IsIndeterminate = false;
                    Foreground = ProgressColors.SuccessGreen;
                    Background = Brushes.Transparent;
                    Visibility = Visibility.Visible;
                    IsEnabled = true;
                    Opacity = 1.0;
                    Minimum = 0;
                    Maximum = 100;

                    var progress3 = (elapsedMs / SuccessDuration) * 100;
                    ProgressValue = Math.Min(progress3, 100);

                    if (elapsedMs >= SuccessDuration)
                    {
                        // 循环回到加载状态
                        TransitionToState(ProgressState.Loading);
                    }
                    break;
            }
        }

        private void TransitionToState(ProgressState newState)
        {
            _currentState = newState;
            _stateStartTime = DateTime.Now;
        }
    }
}