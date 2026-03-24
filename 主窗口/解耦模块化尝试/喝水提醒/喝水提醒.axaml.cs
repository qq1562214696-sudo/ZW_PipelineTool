using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace ZW_PipelineTool
{
    public partial class 主窗口 // 喝水分块
    {
        /// <summary>
        /// 确保定时器在需要提醒的情况下运行（简化版，直接 Start 即可）
        /// </summary>
        private void EnsureTimerRunning()
        {
            if (!_窗口数据.开启饮水提醒)
            {
                return;
            }
            if (_窗口数据.累计饮水量 >= _窗口数据.每日饮水量)
            {
                return;
            }
            if (饮水提醒定时器 == null)
            {
                日志("[错误] 定时器未初始化");
                return;
            }

            // 直接 Start，如果定时器已运行会重置计时，如果已停止会重新启动
            饮水提醒定时器.Start();
        }

        private void OnDrinkTimerTick(object? sender, EventArgs e)
        {
            DoDrinkCheck();
        }

        /// <summary>
        /// 程序启动时强制进行一次喝水提醒判断，确保至少触发一次逻辑
        /// </summary>
        private async Task PerformInitialDrinkCheck()
        {
            // 先停止定时器，防止干扰
            饮水提醒定时器?.Stop();

            // 执行核心判断（与定时器 Tick 逻辑一致）
            await Dispatcher.UIThread.InvokeAsync(() => DoDrinkCheck());

            // 根据当前状态重新调整定时器（决定是否启动、是否更新间隔）
            RestartTimerBasedOnSettings();
        }

        /// <summary>
        /// 根据当前设置重启计时器（若提醒开启且未达标则启动，否则停止）
        /// </summary>
        private void RestartTimerBasedOnSettings()
        {
            if (饮水提醒定时器 == null)
            {
                日志("[错误] 定时器未初始化，无法重启");
                return;
            }

            // 先停止
            饮水提醒定时器.Stop();

            // 如果提醒未开启，保持停止
            if (!_窗口数据.开启饮水提醒)
            {
                return;
            }

            // 如果已达标，停止定时器
            if (_窗口数据.累计饮水量 >= _窗口数据.每日饮水量)
            {
                return;
            }

            // 重新设置间隔（可能已被用户修改）
            饮水提醒定时器.Interval = TimeSpan.FromMinutes(_窗口数据.饮水提醒间隔);

            // 启动定时器
            饮水提醒定时器.Start();
        }

        private void SetupDrinkReminder()
        {
            饮水提醒定时器 = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(_窗口数据.饮水提醒间隔)
            };
            饮水提醒定时器.Tick += OnDrinkTimerTick;

            // Opened 事件中不再启动定时器，只刷新 UI（防止重复启动）
            this.Opened += (s, e) =>
            {
                UpdateDrinkProgressUI();
            };

            this.Closing += (s, e) =>
            {
                饮水提醒定时器?.Stop();
                饮水提醒弹窗?.Close();
            };
        }

        private void CheckAndResetDrinkData()
        {
            DateTime today = DateTime.Today;
            if (_窗口数据.上次饮水日期.HasValue && _窗口数据.上次饮水日期.Value.Date != today)
            {
                _窗口数据.累计饮水量 = 0;
                _窗口数据.上次饮水日期 = today;
                UpdateDrinkProgressUI();
                EnsureTimerRunning(); // 重置后可能低于目标，需要确保定时器运行
            }
            else if (!_窗口数据.上次饮水日期.HasValue)
            {
                _窗口数据.上次饮水日期 = today;
            }
        }

        private void UpdateDrinkProgressUI()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var progressBar = this.FindControl<ProgressBar>("DrinkProgressBar");
                var progressText = this.FindControl<TextBlock>("DrinkProgressText");

                if (progressBar != null)
                {
                    progressBar.Maximum = _窗口数据.每日饮水量;
                    progressBar.Value = _窗口数据.累计饮水量;
                }

                if (progressText != null)
                {
                    double percent = _窗口数据.每日饮水量 > 0 ? (_窗口数据.累计饮水量 / _窗口数据.每日饮水量) * 100 : 0;
                    progressText.Text = $"{_窗口数据.累计饮水量:0} / {_窗口数据.每日饮水量:0} ml ({percent:0}%)";
                }
            });
        }

        private void DoDrinkCheck()
        {
            // 重新检查日期重置
            CheckAndResetDrinkData();

            // 如果提醒未开启，直接返回（不弹窗）
            if (!_窗口数据.开启饮水提醒)
            {
                return;
            }

            // 如果已经达标，不弹窗并记录日志
            if (_窗口数据.累计饮水量 >= _窗口数据.每日饮水量)
            {
                return;
            }

            // 防止重复弹窗
            if (饮水提醒弹窗 != null)
            {
                饮水提醒弹窗.Activate();
                日志("[提醒] 弹窗已存在，激活现有窗口");
                return;
            }

            // 创建弹窗
            饮水提醒弹窗 = new 饮水提醒
            {
                TotalDrunk = _窗口数据.累计饮水量,
                DailyTarget = _窗口数据.每日饮水量
            };

            // ========== 新增：让弹窗显示在鼠标光标所在的屏幕中央 ==========
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Screens != null)
            {
        #if WINDOWS
                // 获取鼠标光标位置（Windows 专用，原代码中已有 GetCursorPos）
                if (GetCursorPos(out var pt))
                {
                    var mousePoint = new PixelPoint(pt.X, pt.Y);
                    var screen = topLevel.Screens.ScreenFromPoint(mousePoint) ?? topLevel.Screens.Primary;
                    if (screen != null)
                    {
                        var workArea = screen.WorkingArea;
                        var winWidth = 饮水提醒弹窗.Width;   // 从 XAML 定义中读取（380）
                        var winHeight = 饮水提醒弹窗.Height; // 从 XAML 定义中读取（300）
                        var newX = workArea.X + (workArea.Width - winWidth) / 2;
                        var newY = workArea.Y + (workArea.Height - winHeight) / 2;
                        newX = Math.Clamp(newX, workArea.X, workArea.X + workArea.Width - winWidth);
                        newY = Math.Clamp(newY, workArea.Y, workArea.Y + workArea.Height - winHeight);
                        
                        饮水提醒弹窗.WindowStartupLocation = WindowStartupLocation.Manual;
                        饮水提醒弹窗.Position = new PixelPoint((int)newX, (int)newY);
                    }
                }
        #endif
            }
            // ========== 新增结束 ==========

            // 注册事件
            饮水提醒弹窗.Closed += (s, args) => 饮水提醒弹窗 = null;
            饮水提醒弹窗.DrinkConfirmed += (s, ml) =>
            {
                _窗口数据.累计饮水量 += ml;
                _窗口数据.上次饮水日期 = DateTime.Today;
                Dispatcher.UIThread.InvokeAsync(UpdateDrinkProgressUI);

                if (_窗口数据.累计饮水量 >= _窗口数据.每日饮水量)
                {
                    饮水提醒定时器?.Stop();
                    日志("今日饮水目标达成！定时提醒已停止");
                }
                else
                {
                    // 未达标，确保定时器继续运行
                    RestartTimerBasedOnSettings();
                }
            };

            饮水提醒弹窗.Show();
        }

        private void OnDrinkReminderCheckChanged(object? sender, RoutedEventArgs e)
        {
            // 当勾选/取消勾选时，重启计时器（会基于新状态决定启停）
            RestartTimerBasedOnSettings();

            // 保存窗口数据
            保存窗口设置();
        }

        private void ApplyDrinkSettings_Click(object? sender, RoutedEventArgs e)
        {
            var targetNumeric = this.FindControl<NumericUpDown>("DrinkTargetNumeric");
            var intervalNumeric = this.FindControl<NumericUpDown>("ReminderIntervalNumeric");

            bool targetChanged = false;

            if (targetNumeric != null)
            {
                double newTarget = (double)(targetNumeric.Value ?? 2000);
                newTarget = Math.Max(500, newTarget);
                if (Math.Abs(newTarget - _窗口数据.每日饮水量) > 0.001)
                {
                    _窗口数据.每日饮水量 = newTarget;
                    targetChanged = true;
                }
            }

            if (intervalNumeric != null)
            {
                double newInterval = (double)(intervalNumeric.Value ?? 10);
                newInterval = Math.Max(0.05, newInterval);
                if (Math.Abs(newInterval - _窗口数据.饮水提醒间隔) > 0.001)
                {
                    _窗口数据.饮水提醒间隔 = newInterval;
                    // 注意：定时器重启会使用新间隔，不需额外处理
                }
            }

            UpdateDrinkProgressUI();

            // 关键：重启计时器（会应用新间隔、重新评估开启状态/达标状态）
            RestartTimerBasedOnSettings();

            // 保存窗口数据（确保所有修改持久化）
            保存窗口设置();

            if (targetChanged)
                日志($"目标饮水量已改为 {_窗口数据.每日饮水量:0} ml");
            日志($"设置已应用：间隔 {_窗口数据.饮水提醒间隔:0} 分钟");
        }
    }

    public partial class 饮水提醒 : Window
    {
        public event EventHandler<double>? DrinkConfirmed;

        private ProgressBar? _progressBar;
        private TextBlock? _progressText;

        public 饮水提醒()
        {
            InitializeComponent();
            _progressBar = this.FindControl<ProgressBar>("DrinkProgressBar");
            _progressText = this.FindControl<TextBlock>("DrinkProgressText");
            UpdateUI();
        }

        public double TotalDrunk
        {
            get => GetValue(TotalDrunkProperty);
            set
            {
                SetValue(TotalDrunkProperty, value);
                UpdateUI();
            }
        }
        public static readonly StyledProperty<double> TotalDrunkProperty =
            AvaloniaProperty.Register<饮水提醒, double>(nameof(TotalDrunk), 0);

        public double DailyTarget
        {
            get => GetValue(DailyTargetProperty);
            set
            {
                SetValue(DailyTargetProperty, value);
                UpdateUI();
            }
        }
        public static readonly StyledProperty<double> DailyTargetProperty =
            AvaloniaProperty.Register<饮水提醒, double>(nameof(DailyTarget), 2000);

        private void UpdateUI()
        {
            if (_progressBar != null)
            {
                _progressBar.Maximum = DailyTarget;
                _progressBar.Value = TotalDrunk;
            }
            if (_progressText != null)
            {
                double percent = DailyTarget > 0 ? (TotalDrunk / DailyTarget) * 100 : 0;
                _progressText.Text = $"{TotalDrunk:0} / {DailyTarget:0} ml ({percent:0}%)";
            }
        }

        private void OnConfirmClick(object? sender, RoutedEventArgs e)
        {
            double amount = slider.Value;
            DrinkConfirmed?.Invoke(this, amount);
            Close();
        }
    }
}