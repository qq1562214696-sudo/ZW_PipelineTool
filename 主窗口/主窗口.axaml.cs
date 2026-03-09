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
using System.Threading.Tasks;
using System.Text;
using System.Threading; // 新增：用于 SemaphoreSlim

namespace ZW_PipelineTool;

public partial class 主窗口 : Window
{
    private const string 设置文件名 = "主窗口数据.json";
    private static readonly string 存储路径;
    private 窗口数据 _窗口数据 = new 窗口数据();
    protected TextBox? 日志文本框;
    private Expander? 工具基本设置Expander;
    private Expander? 运行日志Expander;

    // MaxScript 日志监控相关字段
    private FileSystemWatcher? _logWatcher;
    private string _logFilePath;

    // 用于确保日志文件读写原子性的异步锁
    private static readonly SemaphoreSlim _logFileSemaphore = new SemaphoreSlim(1, 1);

    static 主窗口()
    {
        // 注册 GB2312 编码支持（必须在使用之前调用）
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string appData目录 = Path.Combine(AppContext.BaseDirectory, "AppData");
        Directory.CreateDirectory(appData目录);
        存储路径 = Path.Combine(appData目录, 设置文件名);
    }

    public 主窗口()
    {
        InitializeComponent();
        日志文本框 = this.FindControl<TextBox>("LogBox");
        工具基本设置Expander = this.FindControl<Expander>("工具基本设置");
        运行日志Expander = this.FindControl<Expander>("运行日志");

        // 拖放支持
        DragDrop.SetAllowDrop(this, true);
        this.AddHandler(DragDrop.DragEnterEvent, 窗口_拖入);
        this.AddHandler(DragDrop.DragOverEvent, 窗口_拖拽中);
        this.AddHandler(DragDrop.DropEvent, 窗口_放下);

        Opened += (s, e) =>
        {
            加载窗口设置();
            应用窗口设置();
            if (工具基本设置Expander != null)
                工具基本设置Expander.IsExpanded = _窗口数据.工具基本设置展开;
            if (运行日志Expander != null)
                运行日志Expander.IsExpanded = _窗口数据.运行日志展开;
            if (_窗口数据.X坐标 <= 0 && _窗口数据.Y坐标 <= 0)
                自动定位到右下角();
            else
                确保窗口在可见区域内();

            // 设置日志路径：与拖入 .ms 文件的目录一致（程序目录\MAX\Max_Log.txt）
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            _logFilePath = Path.Combine(exeDir, "MAX", "Max_Log.txt");
            // 启动 Max 日志监控
            StartMaxLogWatcher();

            // 判断是否开机自启
            if (开机自启CheckBox != null)
            {
                开机自启CheckBox.IsChecked = IsStartupEnabled();
            }
        };

        Closing += (s, e) =>
        {
            保存窗口设置();
            StopMaxLogWatcher();
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // ========== Max 日志监控相关方法 ==========
    private void StartMaxLogWatcher()
    {
        try
        {
            string dir = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _logWatcher = new FileSystemWatcher
            {
                Path = dir,
                Filter = "Max_Log.txt",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            _logWatcher.Changed += OnMaxLogChanged;
            _logWatcher.Created += OnMaxLogChanged;
            记录日志($"日志记录激活");
        }
        catch (Exception ex)
        {
            记录日志($"[系统] Max 日志监控启动失败：{ex.Message}");
        }
    }

    private void StopMaxLogWatcher()
    {
        if (_logWatcher != null)
        {
            _logWatcher.Changed -= OnMaxLogChanged;
            _logWatcher.Created -= OnMaxLogChanged;
            _logWatcher.EnableRaisingEvents = false;
            _logWatcher.Dispose();
            _logWatcher = null;
        }
    }

    private async void OnMaxLogChanged(object? sender, FileSystemEventArgs e)
    {
        await Task.Delay(400); // 初始延迟，避免文件刚写入时锁定
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // 使用异步信号量保证同一时间只有一个线程读写文件
            await _logFileSemaphore.WaitAsync();
            try
            {
                const int maxRetries = 5;
                int retryDelay = 200; // 毫秒
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        if (!File.Exists(_logFilePath))
                            return;

                        string content = "";
                        // 使用 GB2312 编码读取（MaxScript 默认写入 ANSI 中文）
                        using (var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                        using (var sr = new StreamReader(fs, Encoding.GetEncoding("GB2312")))
                        {
                            content = await sr.ReadToEndAsync();
                            // 清空文件，以便下次只读新增内容
                            fs.SetLength(0);
                            await fs.FlushAsync(); // 确保清空立即生效
                        }

                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            记录日志("┌──── MaxScript 日志 ──────");
                            记录日志(content.Trim());
                            记录日志("└───────────────────────────");
                        }
                        break; // 成功则退出循环
                    }
                    catch (IOException ioEx) when (ioEx.Message.Contains("being used by another process") ||
                                                    ioEx.Message.Contains("正由另一进程使用"))
                    {
                        if (i == maxRetries - 1)
                        {
                            记录日志($"读取 Max 日志文件失败（重试 {maxRetries} 次后）：{ioEx.Message}");
                        }
                        else
                        {
                            await Task.Delay(retryDelay);
                        }
                    }
                    catch (Exception ex)
                    {
                        记录日志($"读取 Max 日志文件失败：{ex.Message}");
                        break;
                    }
                }
            }
            finally
            {
                _logFileSemaphore.Release();
            }
        });
    }

    // ========== 其余原有方法保持不变 ==========
    private void 初始化按钮_点击(object? sender, RoutedEventArgs e)
    {
        记录日志("初始化按钮被点击（待实现具体逻辑）");
    }

    private void 窗口_拖入(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void 窗口_拖拽中(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    protected async void 窗口_放下(object? sender, DragEventArgs e)
    {
        var 文件列表 = e.Data.GetFiles();
        if (文件列表 == null || !文件列表.Any())
        {
            记录日志("未检测到任何文件/文件夹");
            return;
        }
        var 第一个项目 = 文件列表.First();
        var 路径 = 第一个项目.TryGetLocalPath();
        if (string.IsNullOrEmpty(路径))
        {
            记录日志("无法获取本地路径");
            return;
        }
        if (!Directory.Exists(路径))
        {
            记录日志($"拖入的不是文件夹：{路径}");
            return;
        }
        记录日志($"成功接收文件夹：{路径}");
        var 路径文本框 = this.FindControl<TextBox>("FolderPathTextBox");
        if (路径文本框 != null)
            路径文本框.Text = 路径;
        await 规范整理文件夹(路径);
        e.Handled = true;
    }

    private void 保存窗口设置()
    {
        try
        {
            if (WindowState == WindowState.Normal)
            {
                _窗口数据.宽度 = Width;
                _窗口数据.高度 = Height;
                _窗口数据.X坐标 = Position.X;
                _窗口数据.Y坐标 = Position.Y;
            }
            _窗口数据.窗口状态 = WindowState;
            _窗口数据.置顶 = Topmost;
            if (工具基本设置Expander != null)
                _窗口数据.工具基本设置展开 = 工具基本设置Expander.IsExpanded;
            if (运行日志Expander != null)
                _窗口数据.运行日志展开 = 运行日志Expander.IsExpanded;

            string json = JsonSerializer.Serialize(_窗口数据, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(存储路径, json);
        }
        catch (Exception ex)
        {
            记录日志($"窗口保存布局失败：{ex.Message}");
        }
    }

    private void 加载窗口设置()
    {
        try
        {
            if (File.Exists(存储路径))
            {
                string json = File.ReadAllText(存储路径);
                _窗口数据 = JsonSerializer.Deserialize<窗口数据>(json) ?? new 窗口数据();
            }
        }
        catch (Exception ex)
        {
            记录日志($"加载窗口布局失败：{ex.Message}");
            _窗口数据 = new 窗口数据();
        }
    }

    private void 应用窗口设置()
    {
        try
        {
            if (_窗口数据.窗口状态 == WindowState.Normal)
            {
                if (_窗口数据.宽度 > 0) Width = _窗口数据.宽度;
                if (_窗口数据.高度 > 0) Height = _窗口数据.高度;
                if (_窗口数据.X坐标 >= 0 && _窗口数据.Y坐标 >= 0)
                {
                    Position = new PixelPoint((int)_窗口数据.X坐标, (int)_窗口数据.Y坐标);
                }
            }
            WindowState = _窗口数据.窗口状态;
            Topmost = _窗口数据.置顶;
        }
        catch { }
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

    protected virtual void 记录日志(string 消息)
    {
        if (日志文本框 == null) return;
        string 时间 = DateTime.Now.ToString("HH:mm:ss");
        string 一行 = $"[{时间}] {消息}\n";
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                if (日志文本框 != null && 日志文本框.IsVisible)
                {
                    日志文本框.Text += 一行;
                    日志文本框.CaretIndex = 日志文本框.Text.Length;
                }
            }
            catch { }
        });
    }
}

public class 窗口数据
{
    public double 宽度 { get; set; } = 300;
    public double 高度 { get; set; } = 600;
    public double X坐标 { get; set; }
    public double Y坐标 { get; set; }
    public WindowState 窗口状态 { get; set; } = WindowState.Normal;
    public bool 置顶 { get; set; } = true;
    public bool 工具基本设置展开 { get; set; } = true;
    public bool 运行日志展开 { get; set; } = false;
}