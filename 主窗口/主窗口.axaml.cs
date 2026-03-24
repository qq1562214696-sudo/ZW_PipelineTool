using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace ZW_PipelineTool
{
    public partial class 主窗口 : Window, INotifyPropertyChanged
    {
        public 主窗口()
        {
            InitializeComponent();
            DataContext = this;

            工具基本设置Expander = this.FindControl<Expander>("工具基本设置");
            饮水提醒Expander = this.FindControl<Expander>("饮水提醒");
            日志列表框 = this.FindControl<ListBox>("LogListBox");

            EnableDragAndDrop();

            Opened += OnWindowOpened;
            Closing += OnWindowClosing;

            SetupDrinkReminder();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private async void OnWindowOpened(object? sender, EventArgs e)
        {
            LoadAndApplyWindowSettings();
            CheckAndResetDrinkData();
            UpdateDrinkProgressUI();

            // 新增：强制触发一次喝水提醒判断（确保程序启动至少有一次判断）
            await PerformInitialDrinkCheck();

            await TryLoadLastUnityPathAsync();
            InitializeAndStartLogWatcher();

        #if WINDOWS
            RegisterGlobalHotKey();
        #endif
        }

        private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            保存窗口设置();
            StopMaxLogWatcher();

#if WINDOWS
            UnregisterGlobalHotKey();
#endif
        }

#if WINDOWS
        private void RegisterGlobalHotKey()
        {
            _windowHandle = this.TryGetPlatformHandle()?.Handle;
            if (!_windowHandle.HasValue) return;

            bool success = RegisterHotKey(_windowHandle.Value, HotKeyId_F5, ModifierNone, VK_F5);
            if (success)
            {
                _wndProcCallback = WndProcHook;
                Win32Properties.AddWndProcHookCallback(this, _wndProcCallback);
                日志("按下F5弹出该工具");
            }
            else
            {
                日志("注册全局 F5 热键失败，可能已被其他程序占用");
            }
        }

        private void UnregisterGlobalHotKey()
        {
            if (_windowHandle.HasValue && _wndProcCallback != null)
            {
                UnregisterHotKey(_windowHandle.Value, HotKeyId_F5);
                Win32Properties.RemoveWndProcHookCallback(this, _wndProcCallback);
            }
        }

        private IntPtr WndProcHook(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == HotKeyMessage && (int)wParam == HotKeyId_F5)
            {
                Dispatcher.UIThread.Post(全局F5处理);
                handled = true;
                return IntPtr.Zero;
            }
            return IntPtr.Zero;
        }

        private void 全局F5处理()
        {
            try
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }

                Activate();

                if (GetCursorPos(out var pt))
                {
                    var mousePoint = new PixelPoint(pt.X, pt.Y);
                    var targetScreen = Screens.ScreenFromPoint(mousePoint) ?? Screens.Primary;

                    if (targetScreen != null)
                    {
                        var workArea = targetScreen.WorkingArea;
                        var newX = workArea.X + (workArea.Width - Width) / 2;
                        var newY = workArea.Y + (workArea.Height - Height) / 2;

                        newX = Math.Clamp(newX, workArea.X, workArea.X + workArea.Width - Width);
                        newY = Math.Clamp(newY, workArea.Y, workArea.Y + workArea.Height - Height);

                        Position = new PixelPoint((int)newX, (int)newY);
                    }
                }

                日志("全局 F5 → 窗口已还原并移至鼠标所在屏幕中央");
            }
            catch (Exception ex)
            {
                日志($"全局 F5 处理失败：{ex.Message}");
            }
        }
#endif

        // ────────────────────────────────────────────────
        // 以下是原有方法（实现体只在这里，其他文件删除重复）
        // ────────────────────────────────────────────────

        private async Task TryLoadLastUnityPathAsync()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Max", "QF_Config.txt");
                if (!File.Exists(configPath)) return;

                var lines = await File.ReadAllLinesAsync(configPath);
                foreach (var line in lines)
                {
                    if (!line.StartsWith("AssetsPath=", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string savedPath = line.Substring("AssetsPath=".Length).Trim();
                    if (string.IsNullOrEmpty(savedPath) || !Directory.Exists(savedPath))
                        continue;

                    var unityBox = this.FindControl<TextBox>("QF_UnityPathInput");
                    if (unityBox == null) continue;

                    unityBox.Text = savedPath;
                    return;
                }
            }
            catch (Exception ex)
            {
                日志($"自动加载 Unity 路径失败：{ex.Message}");
            }
        }

        private void InitializeAndStartLogWatcher()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            _logFilePath = Path.Combine(exeDir, "Max", "Max_Log.txt");
            StartMaxLogWatcher();
        }

        private void 确保窗口在可见区域内()
        {
            var 主屏幕 = Screens.Primary;
            if (主屏幕 == null) return;

            var 工作区 = 主屏幕.WorkingArea;
            int x = Position.X, y = Position.Y;
            int 宽 = (int)Width, 高 = (int)Height;

            if (x + 宽 > 工作区.X + 工作区.Width) x = 工作区.X + 工作区.Width - 宽;
            if (x < 工作区.X) x = 工作区.X;
            if (y + 高 > 工作区.Y + 工作区.Height) y = 工作区.Y + 工作区.Height - 高;
            if (y < 工作区.Y) y = 工作区.Y;

            Position = new PixelPoint(x, y);
        }

        private void 自动定位到右下角()
        {
            var 主屏幕 = Screens.Primary;
            if (主屏幕 == null) return;

            var 缩放 = 主屏幕.Scaling;
            var 窗口像素尺寸 = new PixelSize(
                (int)(ClientSize.Width * 缩放),
                (int)(ClientSize.Height * 缩放));

            var 工作区 = 主屏幕.WorkingArea;
            int 边距 = (int)(15 * 缩放);

            int x = 工作区.X + 工作区.Width - 窗口像素尺寸.Width - 边距;
            int y = 工作区.Y + 工作区.Height - 窗口像素尺寸.Height - 边距;

            x = Math.Max(工作区.X, x);
            y = Math.Max(工作区.Y, y);

            Position = new PixelPoint(x, y);
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        private void LoadAndApplyWindowSettings()
        {
            加载窗口设置();
            应用窗口设置();

            开机自启 = _窗口数据.开机自启;
            OnPropertyChanged(nameof(开机自启));

            if (工具基本设置Expander != null)
                工具基本设置Expander.IsExpanded = _窗口数据.工具基本设置展开;

                // 加载饮水提醒Expander状态
            if (饮水提醒Expander != null)
                饮水提醒Expander.IsExpanded = _窗口数据.饮水提醒展开;

            if (运行日志Expander != null)
                运行日志Expander.IsExpanded = _窗口数据.运行日志展开;

                // ========== 新增：加载饮水提醒设置到UI ==========
            var targetNumeric = this.FindControl<NumericUpDown>("DrinkTargetNumeric");
            if (targetNumeric != null)
                targetNumeric.Value = (decimal?)_窗口数据.每日饮水量;

            var intervalNumeric = this.FindControl<NumericUpDown>("ReminderIntervalNumeric");
            if (intervalNumeric != null)
                intervalNumeric.Value = (decimal?)_窗口数据.饮水提醒间隔;

                // 在加载其他设置后添加
            开启饮水提醒 = _窗口数据.开启饮水提醒;
            OnPropertyChanged(nameof(开启饮水提醒));

            if (_窗口数据.X坐标 <= 0 && _窗口数据.Y坐标 <= 0)
                自动定位到右下角();
            else
                确保窗口在可见区域内();
        }
    }
}