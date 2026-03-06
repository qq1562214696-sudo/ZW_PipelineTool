using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Linq;

namespace ZW_PipelineTool;

public partial class MainWindow : Window
{
    private const string 设置文件名 = "主窗口数据.json";
    private static readonly string 存储路径;

    private 窗口数据 _窗口数据 = new 窗口数据();
    protected TextBox? _logBox;

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

        // 启用拖放支持（具体处理逻辑放到子类或Main.cs）
        DragDrop.SetAllowDrop(this, true);
        this.AddHandler(DragDrop.DragEnterEvent, Window_DragEnter);
        this.AddHandler(DragDrop.DragOverEvent, Window_DragOver);
        this.AddHandler(DragDrop.DropEvent, Window_Drop);

        Opened += (s, e) =>
        {
            ApplyWindowSettings();

            if (_窗口数据.x坐标 <= 0 && _窗口数据.y坐标 <= 0)
            {
                自动定位右下角();
            }
            else
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

    // ── 拖放基础事件 ────────────────────────────────────────────────
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

    // ── 拖放具体处理（重写父类方法） ────────────────────────────────
    protected async void Window_Drop(object? sender, DragEventArgs e)
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

        await QF_ProcessFolderNorm(path);

        e.Handled = true;
    }

    // ── 窗口位置与状态保存/加载 ─────────────────────────────────

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
                Log("已加载上次窗口布局");
            }
            else
            {
                _窗口数据 = new 窗口数据();
            }
        }
        catch (Exception ex)
        {
            Log($"加载布局失败：{ex.Message}");
            _窗口数据 = new 窗口数据();
        }
    }

    private void ApplyWindowSettings()
    {
        try
        {
            if (_窗口数据.窗口大小状态 == WindowState.Normal)
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

    // ── 日志（线程安全，可被子类调用） ─────────────────────────────────────

    protected virtual void Log(string message)
    {
        if (_logBox == null) return;

        string time = DateTime.Now.ToString("HH:mm:ss");
        string line = $"[{time}] {message}\n";

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                if (_logBox != null && _logBox.IsVisible)
                {
                    _logBox.Text += line;
                    _logBox.CaretIndex = _logBox.Text.Length;
                }
            }
            catch { }
        });
    }
}

public class 窗口数据
{
    public double 宽 { get; set; } = 500;
    public double 高 { get; set; } = 600;
    public double x坐标 { get; set; }
    public double y坐标 { get; set; }
    public WindowState 窗口大小状态 { get; set; } = WindowState.Normal;
    public bool 置顶 { get; set; } = true;
    // public bool 日志展开 { get; set; } = false;
}