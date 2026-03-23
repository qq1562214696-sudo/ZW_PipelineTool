using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;

namespace ZW_PipelineTool
{
    public partial class 主窗口//调试分块
    {
        // 喝水提醒相关字段（从 const 改成变量）
        private double _dailyTargetMl = 2000;          // 每日目标，可修改
        private double _reminderIntervalMinutes = 10;  // 提醒间隔（分钟），可修改
        private double _totalDrunkMl = 0;
        private DispatcherTimer? _drinkTimer;
        private 喝水提醒? _currentReminderWindow;

        private void SetupDrinkReminder()
        {
            _drinkTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(_reminderIntervalMinutes)
            };
            _drinkTimer.Tick += OnDrinkTimerTick;

            this.Opened += (s, e) =>
            {
                _drinkTimer?.Start();
                UpdateDrinkProgressUI(); // 打开窗口时更新进度显示
            };

            this.Closing += (s, e) =>
            {
                _drinkTimer?.Stop();
                _currentReminderWindow?.Close();
            };
        }

        private void UpdateDrinkProgressUI()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var progressBar = this.FindControl<ProgressBar>("DrinkProgressBar");
                var progressText = this.FindControl<TextBlock>("DrinkProgressText");

                if (progressBar != null)
                {
                    progressBar.Maximum = _dailyTargetMl;
                    progressBar.Value = _totalDrunkMl;
                }

                if (progressText != null)
                {
                    double percent = _dailyTargetMl > 0 ? (_totalDrunkMl / _dailyTargetMl) * 100 : 0;
                    progressText.Text = $"{_totalDrunkMl:0} / {_dailyTargetMl:0} ml ({percent:0}%)";
                }
            });
        }

        private void OnDrinkTimerTick(object? sender, EventArgs e)
        {
            if (_totalDrunkMl >= _dailyTargetMl)
            {
                _drinkTimer?.Stop();
                return;
            }

            if (_currentReminderWindow != null)
            {
                _currentReminderWindow.Activate();
                return;
            }

            _currentReminderWindow = new 喝水提醒
            {
                TotalDrunk = _totalDrunkMl,
                DailyTarget = _dailyTargetMl   // 添加这一行
            };

            _currentReminderWindow.Closed += (s, args) => _currentReminderWindow = null;

            _currentReminderWindow.DrinkConfirmed += (s, ml) =>
            {
                _totalDrunkMl += ml;
                Dispatcher.UIThread.InvokeAsync(UpdateDrinkProgressUI); // 同样用 InvokeAsync

                if (_totalDrunkMl >= _dailyTargetMl)
                {
                    _drinkTimer?.Stop();
                    日志("今日饮水目标达成！定时提醒已停止");
                }
            };

            _currentReminderWindow.Show();
        }

        private void ApplyDrinkSettings_Click(object? sender, RoutedEventArgs e)
        {
            var targetNumeric = this.FindControl<NumericUpDown>("DrinkTargetNumeric");
            var intervalNumeric = this.FindControl<NumericUpDown>("ReminderIntervalNumeric");

            // 处理目标饮水量
            if (targetNumeric != null)
            {
                double newTarget = (double)(targetNumeric.Value ?? 2000);           // null 时用默认
                _dailyTargetMl = Math.Max(500, newTarget);                // 最小 500ml
            }

            // 处理提醒间隔
            if (intervalNumeric != null)
            {
                double newInterval = (double)(intervalNumeric.Value ?? 10);         // null 时用默认
                if (Math.Abs(newInterval - _reminderIntervalMinutes) > 0.001) // 避免浮点比较问题
                {
                    _reminderIntervalMinutes = Math.Max(0.05, newInterval);  // 最小 5 分钟
                    if (_drinkTimer != null)
                    {
                        _drinkTimer.Stop();
                        _drinkTimer.Interval = TimeSpan.FromMinutes(_reminderIntervalMinutes);
                        _drinkTimer.Start(); // 重置计时，从现在开始重新倒计时
                        日志($"提醒间隔已改为 {_reminderIntervalMinutes} 分钟，计时已重置");
                    }
                }
            }

            UpdateDrinkProgressUI(); // 刷新进度显示（目标变了也要更新）
            日志($"设置已应用：目标 {_dailyTargetMl:0} ml，间隔 {_reminderIntervalMinutes:0} 分钟");
        }

        private void 手动喝水弹窗_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_currentReminderWindow != null)
            {
                _currentReminderWindow.Activate();
                _currentReminderWindow.Focus();
                return;
            }

            // 创建弹窗，同时传入累计值和目标值
            _currentReminderWindow = new 喝水提醒
            {
                TotalDrunk = _totalDrunkMl,
                DailyTarget = _dailyTargetMl   // 传入用户设置的目标值
            };

            _currentReminderWindow.Closed += (s, args) => _currentReminderWindow = null;

            _currentReminderWindow.DrinkConfirmed += (s, ml) =>
            {
                _totalDrunkMl += ml;
                // 确保 UI 更新在主线程执行（避免线程问题）
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(UpdateDrinkProgressUI);

                日志($"[手动喝水] 记录 +{ml:0} ml，当前累计：{_totalDrunkMl:0} ml");

                if (_totalDrunkMl >= _dailyTargetMl)
                {
                    _drinkTimer?.Stop();
                    日志("饮水目标已达成，定时器已停止");
                }
            };

            _currentReminderWindow.Show();
        }
    }

    public partial class 喝水提醒 : Window
    {
        public event EventHandler<double>? DrinkConfirmed;

        // 累计已喝水量（由主窗口传入）
        public double TotalDrunk
        {
            get => GetValue(TotalDrunkProperty);
            set => SetValue(TotalDrunkProperty, value);
        }
        public static readonly StyledProperty<double> TotalDrunkProperty =
            AvaloniaProperty.Register<喝水提醒, double>(nameof(TotalDrunk), 0);

        // 新增：每日目标值（由主窗口传入）
        public double DailyTarget
        {
            get => GetValue(DailyTargetProperty);
            set => SetValue(DailyTargetProperty, value);
        }
        public static readonly StyledProperty<double> DailyTargetProperty =
            AvaloniaProperty.Register<喝水提醒, double>(nameof(DailyTarget), 2000);

        public 喝水提醒()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnConfirmClick(object? sender, RoutedEventArgs e)
        {
            double amount = slider.Value;
            DrinkConfirmed?.Invoke(this, amount);
            Close();
        }
    }
}