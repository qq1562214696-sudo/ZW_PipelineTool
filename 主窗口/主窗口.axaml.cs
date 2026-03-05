using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Text.Json;
using Avalonia.Platform.Storage;
using System.Diagnostics;
using System.Linq;

namespace ZW_PipelineTool;

public partial class MainWindow : Window
{
    private const string 设置文件名 = "主窗口数据.json";
    private static readonly string 存储路径;

    private 窗口数据 _窗口数据 = new 窗口数据();
    private bool _settingsLoaded = false;
    private TextBox? _logBox;

    static MainWindow()
    {
        string appDataDir = Path.Combine(AppContext.BaseDirectory, "AppData");
        Directory.CreateDirectory(appDataDir);
        存储路径 = Path.Combine(appDataDir, 设置文件名);
    }

    public MainWindow()
    {
        InitializeComponent();

        LoadWindowSettings();

        DragDrop.SetAllowDrop(this, true);
        this.AddHandler(DragDrop.DragEnterEvent, Window_DragEnter);
        this.AddHandler(DragDrop.DragOverEvent, Window_DragOver);
        this.AddHandler(DragDrop.DropEvent, Window_Drop);

        Opened += (s, e) =>
        {
            ApplyWindowSettings();

            if (!_settingsLoaded ||
                (_窗口数据.x坐标 <= 0 && _窗口数据.y坐标 <= 0 && _窗口数据.窗口大小状态 == WindowState.Normal))
            {
                自动定位右下角();
            }
            else if (_窗口数据.窗口大小状态 == WindowState.Normal)
            {
                EnsureWindowVisible();
            }
        };

        Closing += (s, e) => SaveWindowSettings();
        _logBox = this.FindControl<TextBox>("LogBox");
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void ToggleTopmost_Checked(object? sender, RoutedEventArgs e) => Topmost = true;
    private void ToggleTopmost_Unchecked(object? sender, RoutedEventArgs e) => Topmost = false;

    private void Window_DragEnter(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void Window_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void Window_Drop(object? sender, DragEventArgs e)
    {
        Log("=== 拖放事件触发 ===");

        var files = e.Data.GetFiles();
        if (files == null || !files.Any())
        {
            Log("未检测到任何文件/文件夹");
            return;
        }

        var firstItem = files.First();
        var path = firstItem.TryGetLocalPath();

        if (string.IsNullOrEmpty(path))
        {
            Log("无法获取本地路径");
            return;
        }

        if (!Directory.Exists(path))
        {
            Log($"拖入的不是文件夹：{path}");
            return;
        }

        Log($"成功接收文件夹：{path}");

        var textBox = this.FindControl<TextBox>("FolderPathTextBox");
        if (textBox != null)
            textBox.Text = path;

        await HandleFolderDrop(path);
        e.Handled = true;
    }

    // 这个方法被 QF_Partial.cs 使用，需要在这里声明为 partial 或 public/protected
    // 但因为是 partial class，我们直接调用即可（编译器会合并）
    private async System.Threading.Tasks.Task HandleFolderDrop(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            Log("无效的文件夹路径");
            return;
        }
        Log($"开始规范整理：{folderPath}");
        await QF_ProcessFolderNorm(folderPath);  // 调用另一个 partial 中的方法
    }

    // Log 方法也要在这里（因为 _logBox 是这里定义的）
    protected virtual void Log(string message)
    {
        if (_logBox == null) return;

        string time = DateTime.Now.ToString("HH:mm:ss");
        string line = $"[{time}] {message}\n";

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _logBox.Text += line;
            _logBox.CaretIndex = _logBox.Text.Length;
            var lineCount = _logBox.Text.Split('\n').Length;
            if (lineCount > 0)
                _logBox.ScrollToLine(lineCount - 1);
        });
    }

    // ──────────────────────────────
    // 窗口位置、大小、置顶 保存/加载
    // ──────────────────────────────

    private void SaveWindowSettings()
    {
        try
        {
            if (WindowState == WindowState.Normal)
            {
                _窗口数据.宽 = Width;
                _窗口数据.高 = Height;
                _窗口数据.x坐标 = Position.X;
                _窗口数据.y坐标 = Position.Y;
            }

            _窗口数据.窗口大小状态 = WindowState;
            _窗口数据.置顶 = Topmost;

            string json = JsonSerializer.Serialize(_窗口数据, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(存储路径, json);
            Log("窗口布局已保存");
        }
        catch (Exception ex)
        {
            Log($"保存布局失败：{ex.Message}");
        }
    }

    private void LoadWindowSettings()
    {
        try
        {
            if (File.Exists(存储路径))
            {
                string json = File.ReadAllText(存储路径);
                _窗口数据 = JsonSerializer.Deserialize<窗口数据>(json) ?? new 窗口数据();
                _settingsLoaded = true;
                Log("已加载上次窗口布局");
            }
            else
            {
                _窗口数据 = new 窗口数据();
                _settingsLoaded = false;
            }
        }
        catch
        {
            _窗口数据 = new 窗口数据();
            _settingsLoaded = false;
        }
    }

    private void ApplyWindowSettings()
    {
        try
        {
            if (_settingsLoaded && _窗口数据.窗口大小状态 == WindowState.Normal)
            {
                if (_窗口数据.宽 > 0) Width = _窗口数据.宽;
                if (_窗口数据.高 > 0) Height = _窗口数据.高;
                if (_窗口数据.x坐标 >= 0 && _窗口数据.y坐标 >= 0)
                {
                    Position = new PixelPoint((int)_窗口数据.x坐标, (int)_窗口数据.y坐标);
                }
            }

            WindowState = _窗口数据.窗口大小状态;
            Topmost = _窗口数据.置顶;
        }
        catch { }
    }

    private void EnsureWindowVisible()
    {
        var screen = Screens.Primary;
        if (screen == null) return;

        var workingArea = screen.WorkingArea;
        int x = Position.X, y = Position.Y;
        int width = (int)Width, height = (int)Height;

        int left = workingArea.X;
        int top = workingArea.Y;
        int right = workingArea.X + workingArea.Width;
        int bottom = workingArea.Y + workingArea.Height;

        if (x + width > right) x = right - width;
        if (x < left) x = left;
        if (y + height > bottom) y = bottom - height;
        if (y < top) y = top;

        Position = new PixelPoint(x, y);
    }

    private void 自动定位右下角()
    {
        var screen = Screens.Primary;
        if (screen == null) return;

        var scaling = screen.Scaling;
        var windowPixelSize = new PixelSize(
            (int)(ClientSize.Width * scaling),
            (int)(ClientSize.Height * scaling));

        var workingArea = screen.WorkingArea;
        int margin = (int)(15 * scaling);
        int x = workingArea.X + workingArea.Width - windowPixelSize.Width - margin;
        int y = workingArea.Y + workingArea.Height - windowPixelSize.Height - margin;

        x = Math.Max(workingArea.X, x);
        y = Math.Max(workingArea.Y, y);

        Position = new PixelPoint(x, y);
        WindowStartupLocation = WindowStartupLocation.Manual;
    }

    // MaxScript 相关（保持不变）
    private void OnScriptButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Content is not string scriptName || string.IsNullOrWhiteSpace(scriptName))
            return;

        Log($"点击按钮：{scriptName}");
        RunMaxScript(scriptName);
    }

    private void RunMaxScript(string scriptName)
    {
        string projectRoot = AppContext.BaseDirectory;
        string maxFolder = Path.Combine(projectRoot, "MAX");

        string msPath = Path.Combine(maxFolder, scriptName + ".ms");
        if (File.Exists(msPath))
        {
            Log($"找到脚本：{scriptName}.ms");
            SendMsTo3dsMax(msPath);
            return;
        }

        string mcrPath = Path.Combine(maxFolder, scriptName + ".mcr");
        if (File.Exists(mcrPath))
        {
            Log($"找到脚本：{scriptName}.mcr");
            SendMsTo3dsMax(mcrPath);
            return;
        }

        Log($"未找到脚本：{scriptName} (.ms 或 .mcr)");
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct DROPFILES
    {
        public uint pFiles;
        public POINT pt;
        public bool fNC;
        public bool fWide;
    }

    private const uint WM_DROPFILES = 0x0233;

    private void SendMsTo3dsMax(string scriptPath)
    {
        if (!File.Exists(scriptPath))
        {
            Log("文件不存在（异常情况）");
            return;
        }

        IntPtr hMax = FindWindow("3DSMAX", null);
        if (hMax == IntPtr.Zero) hMax = FindWindow(null, "3ds Max");
        if (hMax == IntPtr.Zero)
        {
            Log("未找到 3ds Max 窗口");
            return;
        }

        Log("找到 3ds Max 窗口，尝试发送");

        SetForegroundWindow(hMax);

        if (!GetWindowRect(hMax, out RECT rect))
        {
            Log("无法获取窗口位置");
            return;
        }

        int centerX = rect.Left + (rect.Right - rect.Left) / 2;
        int centerY = rect.Top + (rect.Bottom - rect.Top) / 2;

        string pathWithDoubleNull = scriptPath + "\0\0";
        byte[] pathBytes = Encoding.Unicode.GetBytes(pathWithDoubleNull);

        int dfSize = Marshal.SizeOf<DROPFILES>();
        int totalSize = dfSize + pathBytes.Length;

        IntPtr hGlobal = Marshal.AllocHGlobal(totalSize);
        if (hGlobal == IntPtr.Zero)
        {
            Log("内存分配失败");
            return;
        }

        try
        {
            var df = new DROPFILES
            {
                pFiles = (uint)dfSize,
                pt = new POINT { X = centerX, Y = centerY },
                fNC = false,
                fWide = true
            };

            Marshal.StructureToPtr(df, hGlobal, false);
            Marshal.Copy(pathBytes, 0, hGlobal + dfSize, pathBytes.Length);

            bool posted = PostMessage(hMax, WM_DROPFILES, hGlobal, IntPtr.Zero);
            Log(posted ? "已发送 WM_DROPFILES 消息" : "PostMessage 返回 false");
        }
        catch (Exception ex)
        {
            Log($"发送过程中异常：{ex.Message}");
        }
        finally
        {
            Marshal.FreeHGlobal(hGlobal);
        }
    }
}

public class 窗口数据
{
    public double 宽 { get; set; } = 400;
    public double 高 { get; set; } = 400;
    public double x坐标 { get; set; }
    public double y坐标 { get; set; }
    public WindowState 窗口大小状态 { get; set; } = WindowState.Normal;
    public bool 置顶 { get; set; } = true;
}