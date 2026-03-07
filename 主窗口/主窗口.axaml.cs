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

/// <summary>
/// 主窗口 - 负责窗口生命周期、位置/大小/置顶状态持久化、日志显示、文件夹拖放支持
/// </summary>
public partial class MainWindow : Window
{
    // 用于保存窗口状态的文件名（相对路径）
    private const string 设置文件名 = "主窗口数据.json";
    // 保存路径：程序目录下的 AppData 文件夹
    private static readonly string 存储路径;

    // 窗口状态数据对象
    private 窗口数据 _窗口数据 = new 窗口数据();

    // 日志显示用的 TextBox 控件引用（延迟查找）
    protected TextBox? _logBox;

    // 静态构造函数：确保 AppData 文件夹存在，并计算存储路径
    static MainWindow()
    {
        // 使用程序运行目录下的 AppData 子文件夹存放配置
        string appDataDir = Path.Combine(AppContext.BaseDirectory, "AppData");
        Directory.CreateDirectory(appDataDir);
        存储路径 = Path.Combine(appDataDir, 设置文件名);
    }

    public MainWindow()
    {
        InitializeComponent();

        // 第一次加载时尝试读取上次的窗口位置、大小、置顶等状态
        LoadWindowSettings();

        // 启用窗口接受拖放文件/文件夹
        DragDrop.SetAllowDrop(this, true);

        // 注册拖放相关事件（主要是文件夹拖入）
        this.AddHandler(DragDrop.DragEnterEvent, Window_DragEnter);
        this.AddHandler(DragDrop.DragOverEvent, Window_DragOver);
        this.AddHandler(DragDrop.DropEvent, Window_Drop);

        // 窗口打开完成后应用保存的位置/大小/置顶状态
        Opened += (s, e) =>
        {
            ApplyWindowSettings();

            // 如果没有有效的保存位置，则自动放到屏幕右下角
            if (_窗口数据.x坐标 <= 0 && _窗口数据.y坐标 <= 0)
            {
                自动定位右下角();
            }
            else
            {
                // 确保窗口不会跑到屏幕外面
                EnsureWindowVisible();
            }
        };

        // 窗口关闭前保存当前状态
        Closing += (s, e) => SaveWindowSettings();

        // 查找日志 TextBox（XAML 中定义的控件）
        _logBox = this.FindControl<TextBox>("LogBox");
    }

    /// <summary>
    /// 加载 Avalonia XAML 定义的 UI 结构
    /// </summary>
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // 置顶复选框勾选/取消事件
    private void ToggleTopmost_Checked(object? sender, RoutedEventArgs e) => Topmost = true;
    private void ToggleTopmost_Unchecked(object? sender, RoutedEventArgs e) => Topmost = false;

    #region 拖放支持（只接受文件夹）

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

    /// <summary>
    /// 核心拖放处理：只接受单个文件夹，并交给 QF_ProcessFolderNorm 处理
    /// </summary>
    protected async void Window_Drop(object? sender, DragEventArgs e)
    {
        Log("=== 拖放事件触发 ===");

        var files = e.Data.GetFiles();
        if (files == null || !files.Any())
        {
            Log("未检测到任何文件/文件夹");
            return;
        }

        // 只处理第一个拖入的项目（我们只接受文件夹）
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

        // 更新界面上的路径显示
        var textBox = this.FindControl<TextBox>("FolderPathTextBox");
        if (textBox != null)
            textBox.Text = path;

        // 执行文件夹规范整理逻辑（核心业务）
        await QF_ProcessFolderNorm(path);

        e.Handled = true;
    }

    #endregion

    #region 窗口位置与状态持久化

    /// <summary>
    /// 保存当前窗口位置、大小、状态、置顶到 json 文件
    /// </summary>
    private void SaveWindowSettings()
    {
        try
        {
            // 只在正常窗口状态下保存大小和位置（最大化/最小化不保存像素值）
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

    /// <summary>
    /// 从 json 文件读取上次保存的窗口状态
    /// </summary>
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

    /// <summary>
    /// 将保存的状态应用到当前窗口
    /// </summary>
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
        catch { /* 静默忽略无效值 */ }
    }

    /// <summary>
    /// 防止窗口跑到屏幕外（多显示器或分辨率变化时保护）
    /// </summary>
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

    /// <summary>
    /// 第一次打开或无有效位置时，自动把窗口放到屏幕右下角（留边距）
    /// </summary>
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

    #endregion

    #region 日志输出（线程安全）

    /// <summary>
    /// 向日志 TextBox 添加一行带时间戳的消息（UI线程安全）
    /// 可被子类/派生类重写
    /// </summary>
    protected virtual void Log(string message)
    {
        if (_logBox == null) return;

        string time = DateTime.Now.ToString("HH:mm:ss");
        string line = $"[{time}] {message}\n";

        // 必须在 UI 线程执行 TextBox 操作
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                if (_logBox != null && _logBox.IsVisible)
                {
                    _logBox.Text += line;
                    // 自动滚动到最新一行
                    _logBox.CaretIndex = _logBox.Text.Length;
                }
            }
            catch { /* 静默忽略 */ }
        });
    }

    #endregion
}

/// <summary>
/// 用于序列化的窗口状态数据类
/// </summary>
public class 窗口数据
{
    public double 宽 { get; set; } = 500;
    public double 高 { get; set; } = 600;
    public double x坐标 { get; set; }
    public double y坐标 { get; set; }
    public WindowState 窗口大小状态 { get; set; } = WindowState.Normal;
    public bool 置顶 { get; set; } = true;
}