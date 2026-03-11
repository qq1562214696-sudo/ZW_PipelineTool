using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace ZW_PipelineTool;

// 1.Unity待测试优化

// 3.将max unity main的QF和主要使用部分分离出来
// 4.除了Mesh01其他ms脚本日志待优化
// 5.后续还需要观察是否功能和QF原工具有出入
// 6.UI优化

// 8.面数检查等检查项可以合做一个
// 9.检查面数检查是否为将满足命名条件的对象全部三角面算到一起
// 10.拖入文件功能后续还需优化，区分拖入区块或者窗口
// 11.交互需要进一步优化，现在只有QF模板文件夹，后续很多东西都需要拖入处理
// 12.
public partial class 主窗口 : Window ,INotifyPropertyChanged
{
    private ListBox? 日志列表框;  // 用于后续查找
    private Expander? 运行日志Expander;     
    private Expander? 工具基本设置Expander;  
    private static readonly SemaphoreSlim _logFileSemaphore = new(1, 1);
    public ObservableCollection<日志数据> 日志列表 { get; } = new();

    // MaxScript 日志监控相关字段
    private FileSystemWatcher? _logWatcher;
    private string _logFilePath = string.Empty;
        // 自启动常量
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppStartupName = "ZW_PipelineTool";

        // 自启动属性（绑定用）
    private bool _开机自启;
    public bool 开机自启
    {
        get => _开机自启;
        set
        {
            if (_开机自启 != value)
            {
                _开机自启 = value;
                SetStartupEnabled(value);
                OnPropertyChanged(nameof(开机自启));
            }
        }
    }

    // INotifyPropertyChanged 实现
    public new event PropertyChangedEventHandler? PropertyChanged;


    public 主窗口()
    {
        InitializeComponent();
        DataContext = this;

        工具基本设置Expander = this.FindControl<Expander>("工具基本设置");
        // 日志列表框已在其他分部类中赋值，此处只需确保控件存在
        日志列表框 = this.FindControl<ListBox>("LogListBox");

        // 启用拖放
        EnableDragAndDrop();

        // 事件绑定
        Opened += OnWindowOpened;
        Closing += OnWindowClosing;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void EnableDragAndDrop()
    {
        DragDrop.SetAllowDrop(this, true);
        this.AddHandler(DragDrop.DragEnterEvent, 窗口_拖入);
        this.AddHandler(DragDrop.DragOverEvent, 窗口_拖拽中);
        this.AddHandler(DragDrop.DropEvent, 窗口_放下);
    }

    // ────────────────────────────────────────────────
    // 窗口打开时执行的初始化逻辑
    // ────────────────────────────────────────────────
    private async void OnWindowOpened(object? sender, EventArgs e)
    {
        // 1. 加载 + 应用窗口设置（位置、大小、展开状态、自启动等）
        LoadAndApplyWindowSettings();

        // 2. 尝试自动加载上次保存的 Unity 路径
        await TryLoadLastUnityPathAsync();

        // 3. 初始化日志文件路径并启动监控
        InitializeAndStartLogWatcher();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        保存窗口设置();
        StopMaxLogWatcher();
    }

    // ────────────────────────────────────────────────
    //           存储 / 加载 相关方法（同步）
    // ────────────────────────────────────────────────
    private void LoadAndApplyWindowSettings()
    {
        加载窗口设置();
        应用窗口设置();

        // 同步 UI 控件状态
        开机自启 = _窗口数据.开机自启;
        OnPropertyChanged(nameof(开机自启));

        if (工具基本设置Expander != null)
            工具基本设置Expander.IsExpanded = _窗口数据.工具基本设置展开;

        // 运行日志Expander 已在其他分部类中赋值，此处直接使用
        if (运行日志Expander != null)
            运行日志Expander.IsExpanded = _窗口数据.运行日志展开;

        // 位置处理
        if (_窗口数据.X坐标 <= 0 && _窗口数据.Y坐标 <= 0)
            自动定位到右下角();
        else
            确保窗口在可见区域内();
    }

    // ────────────────────────────────────────────────
    //         自动加载上次 Unity 路径（异步）
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

                // 使用 Substring 保证兼容性
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

    // ────────────────────────────────────────────────
    //           日志文件监控初始化
    // ────────────────────────────────────────────────
    private void InitializeAndStartLogWatcher()
    {
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        _logFilePath = Path.Combine(exeDir, "Max", "Max_Log.txt");
        StartMaxLogWatcher();
    }

    // ────────────────────────────────────────────────
    //           窗口定位相关方法（保持不变）
    // ────────────────────────────────────────────────
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
}