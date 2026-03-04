using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using SocketIOClient;
using System.Collections.Generic;

namespace ZW_PipelineTool;

public partial class MainWindow : Window
{
    private const string SettingsFileName = "window_settings.json";
    private static readonly string SettingsDirectory;
    private static readonly string SettingsPath;
    private WindowSettings _settings = new WindowSettings();

    // Socket.IO 客户端
    private SocketIO? _socketIO;
    private bool _isConnected = false;

    // Node.js server 进程
    private Process? _nodeProcess;

    static MainWindow()
    {
        string baseDirectory = AppContext.BaseDirectory;
        SettingsDirectory = Path.Combine(baseDirectory, "AppData");
        SettingsPath = Path.Combine(SettingsDirectory, SettingsFileName);
    }

    public MainWindow()
    {
        InitializeComponent();
        LoadWindowSettings();
        Closing += (s, e) =>
        {
            SaveWindowSettings();
            KillNodeProcess();
        };

        this.Topmost = true;

        Opened += async (s, e) =>
        {
            await StartNodeServerAsync();
            await ConnectToSocketIOServerAsync();
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // ==================== 自动启动 Node.js server.js ====================
    private async Task StartNodeServerAsync()
    {
        try
        {
            string projectRoot = AppContext.BaseDirectory;
            string? qfFolder = FindQFDirectory(projectRoot, maxDepth: 5);

            if (string.IsNullOrEmpty(qfFolder))
            {
                await ShowMessageAsync("启动失败", "找不到 QF 文件夹，无法定位 server.js");
                return;
            }

            string serverJsPath = Path.Combine(qfFolder, "server.js");

            if (!File.Exists(serverJsPath))
            {
                await ShowMessageAsync("启动失败", $"server.js 不存在:\n{serverJsPath}");
                return;
            }

            if (!IsNodeInstalled())
            {
                await ShowMessageAsync("Node.js 未找到", "请先安装 Node.js 并确保 node 命令在系统 PATH 中");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = $"\"{serverJsPath}\"",
                WorkingDirectory = qfFolder,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _nodeProcess = new Process { StartInfo = psi };
            _nodeProcess.OutputDataReceived += (sender, args) => Console.WriteLine($"[node] {args.Data}");
            _nodeProcess.ErrorDataReceived += (sender, args) => Console.WriteLine($"[node ERR] {args.Data}");

            _nodeProcess.Start();
            _nodeProcess.BeginOutputReadLine();
            _nodeProcess.BeginErrorReadLine();

            await Task.Delay(2000); // 等待服务器启动

            if (!_nodeProcess.HasExited)
            {
                Console.WriteLine("Node.js Socket.IO 服务器已启动");
            }
            else
            {
                await ShowMessageAsync("服务器启动失败", "node 进程已退出，请检查 server.js");
            }
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("启动 Node.js 失败", ex.Message);
        }
    }

    private string? FindQFDirectory(string startPath, int maxDepth)
    {
        string current = startPath;
        for (int i = 0; i <= maxDepth; i++)
        {
            string test = Path.Combine(current, "QF");
            if (Directory.Exists(test))
                return test;

            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent)) break;
            current = parent!;
        }
        return null;
    }

    private bool IsNodeInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(3000);
            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private void KillNodeProcess()
    {
        try
        {
            if (_nodeProcess != null && !_nodeProcess.HasExited)
            {
                _nodeProcess.Kill(true);
                _nodeProcess.Dispose();
            }
        }
        catch { }
    }

    // ==================== Socket.IO 连接 ====================
    private async Task ConnectToSocketIOServerAsync()
    {
        try
        {
            var uri = new Uri("http://127.0.0.1:8765");
            _socketIO = new SocketIO(uri);

            // OnConnected uses EventHandler signature (sender, args)
            _socketIO.OnConnected += async (sender, args) =>
            {
                _isConnected = true;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Console.WriteLine("已连接到 Socket.IO 服务器");
                });
            };

            _socketIO.On("connect_error", async response =>
            {
                Console.WriteLine($"连接错误: {response}");
                await Task.CompletedTask;
            });

            _socketIO.On("status", async response =>
            {
                Console.WriteLine($"服务器消息: {response}");
                await Task.CompletedTask;
            });

            _socketIO.On("command_result", async response =>
            {
                var result = response.ToString();
                Console.WriteLine($"Max 返回: {result}");
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await ShowMessageAsync("执行结果", result ?? "无返回数据");
                });
            });

            await _socketIO.ConnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Socket.IO 连接失败: {ex.Message}");
            await ShowMessageAsync("连接失败", ex.Message);
        }
    }

    // ==================== 发送命令 ====================
    private async Task SendCommandToMaxAsync(object command)
    {
        if (_socketIO == null || !_isConnected)
        {
            await ShowMessageAsync("未连接", "请确保 server.js 已运行且 Max 已连接");
            return;
        }

        try
        {
            // EmitAsync expects an enumerable of objects, wrap the command
            await _socketIO.EmitAsync("command", new object[] { command });
            Console.WriteLine("命令已发送: " + JsonSerializer.Serialize(command));
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("发送失败", ex.Message);
        }
    }

    // ==================== 初始化按钮 ====================
    private async void OnInitializeClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var maxProcesses = Process.GetProcessesByName("3dsmax");
            if (maxProcesses.Length > 0)
            {
                var result = await ShowConfirmDialogAsync(
                    "3ds Max 已在运行",
                    "检测到 3ds Max 正在运行。\n\n" +
                    "需要复制 Python 文件到 Startup 文件夹。\n" +
                    "已打开的 Max 不会立即加载，需要重启。\n\n" +
                    "是否继续？");

                if (!result) return;
            }

            string sourcePy = await EnsureSourceAgentPathAsync("MaxAgent.py");
            string sourceMs = await EnsureSourceAgentPathAsync("LoadMaxAgent.ms");

            if (string.IsNullOrEmpty(sourcePy) || string.IsNullOrEmpty(sourceMs)) return;

            string maxRoot = await EnsureMaxRootPathAsync();
            if (string.IsNullOrEmpty(maxRoot)) return;

            string targetDir = Path.Combine(maxRoot, "scripts", "Startup");
            Directory.CreateDirectory(targetDir);

            File.Copy(sourcePy, Path.Combine(targetDir, "MaxAgent.py"), true);
            File.Copy(sourceMs, Path.Combine(targetDir, "LoadMaxAgent.ms"), true);

            await ShowMessageAsync("初始化完成",
                $"已成功复制两个文件到：\n{targetDir}\n\n" +
                "请**关闭并重新启动** 3ds Max 以加载监控。");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("初始化失败", ex.Message);
        }
    }

    // ==================== 测试按钮 ====================
    private async void OnTestClick(object? sender, RoutedEventArgs e)
    {
        var command = new Dictionary<string, object>
        {
            ["type"] = "qf_call",
            ["payload"] = new Dictionary<string, object>
            {
                ["method"] = "TestSuccess",
                ["args"] = new { message = "来自 C# 的实时 Socket.IO 调用！" }
            }
        };

        await SendCommandToMaxAsync(command);
        await ShowMessageAsync("测试已发送", "命令已通过 Socket.IO 发送到 Max\n查看 Max 是否弹出消息框");
    }

    // ==================== 辅助方法 ====================
    private async Task<string> EnsureSourceAgentPathAsync(string fileName)
    {
        string defaultPath = Path.Combine(AppContext.BaseDirectory, "QF", fileName);
        if (File.Exists(defaultPath))
            return defaultPath;

        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 5; i++)
        {
            dir = Path.GetDirectoryName(dir);
            if (string.IsNullOrEmpty(dir)) break;
            string testPath = Path.Combine(dir, "QF", fileName);
            if (File.Exists(testPath)) return testPath;
        }

        var dialog = new OpenFileDialog
        {
            Title = $"请选择 {fileName}",
            AllowMultiple = false,
            Filters = { new FileDialogFilter { Name = "File", Extensions = { Path.GetExtension(fileName).TrimStart('.') } } }
        };

        var result = await dialog.ShowAsync(this);
        if (result != null && result.Length > 0 && File.Exists(result[0]))
            return result[0];

        await ShowMessageAsync("错误", $"未选择 {fileName}，操作已取消。");
        return string.Empty;
    }

    private async Task<string> EnsureMaxRootPathAsync()
    {
        if (!string.IsNullOrEmpty(_settings.QF.Max路径) && Directory.Exists(_settings.QF.Max路径))
            return _settings.QF.Max路径;

        string[] defaultPaths = {
            @"C:\Program Files\Autodesk\3ds Max 2025",
            @"C:\Program Files\Autodesk\3ds Max 2024",
            @"C:\Program Files\Autodesk\3ds Max 2023",
            @"C:\Program Files\Autodesk\3ds Max 2022",
            @"C:\Program Files\Autodesk\3ds Max 2021"
        };

        foreach (var path in defaultPaths)
        {
            if (Directory.Exists(path))
            {
                _settings.QF.Max路径 = path.Replace('\\', '/');
                SaveWindowSettings();
                return path;
            }
        }

        var dialog = new OpenFileDialog
        {
            Title = "请选择 3dsmax.exe (2021或更高版本)",
            Filters = { new FileDialogFilter { Name = "Executable", Extensions = { "exe" } } }
        };

        var result = await dialog.ShowAsync(this);
        if (result != null && result.Length > 0 && File.Exists(result[0]))
        {
            string folder = Path.GetDirectoryName(result[0])!;
            _settings.QF.Max路径 = folder.Replace('\\', '/');
            SaveWindowSettings();
            return folder;
        }

        await ShowMessageAsync("错误", "未选择 3dsmax.exe，操作已取消。");
        return string.Empty;
    }

    private async Task<bool> ShowConfirmDialogAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new Grid
            {
                Margin = new Thickness(20),
                RowDefinitions = new RowDefinitions("*, Auto"),
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, [Grid.RowProperty] = 0 },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Spacing = 20,
                        Margin = new Thickness(0, 20, 0, 0),
                        [Grid.RowProperty] = 1,
                        Children =
                        {
                            new Button { Content = "是，继续", Width = 100 },
                            new Button { Content = "否，取消", Width = 100 }
                        }
                    }
                }
            }
        };

        var grid = (Grid)dialog.Content;
        var panel = (StackPanel)grid.Children[1];
        var btnYes = (Button)panel.Children[0];
        var btnNo = (Button)panel.Children[1];

        btnYes.Click += (_, _) => { tcs.TrySetResult(true); dialog.Close(); };
        btnNo.Click += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };
        dialog.Closed += (_, _) => tcs.TrySetResult(false);

        await dialog.ShowDialog(this);
        return await tcs.Task;
    }

    private Task ShowMessageAsync(string title, string content)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Grid
            {
                Margin = new Thickness(20),
                RowDefinitions = new RowDefinitions("*,Auto"),
                Children =
                {
                    new ScrollViewer { Content = new TextBox { Text = content, TextWrapping = TextWrapping.Wrap, IsReadOnly = true, BorderThickness = new Thickness(0), Background = Brushes.Transparent, Foreground = Brushes.Black } },
                    new Button { Content = "确定", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(0, 15, 0, 0), Width = 100, [Grid.RowProperty] = 1 }
                }
            }
        };

        var button = ((Grid)dialog.Content).Children[1] as Button;
        button!.Click += (_, _) => dialog.Close();

        return dialog.ShowDialog(this);
    }

    private void ToggleTopmost_Checked(object? sender, RoutedEventArgs e) => this.Topmost = true;

    private void ToggleTopmost_Unchecked(object? sender, RoutedEventArgs e) => this.Topmost = false;

    private void MaxPathTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (sender is TextBox tb)
        {
            _settings.QF.Max路径 = tb.Text;
            SaveWindowSettings();
        }
    }

    private async void MaxPathTextBox_Drop(object? sender, DragEventArgs e)
    {
        var files = e.Data?.GetFiles();
        if (files == null) return;

        foreach (var item in files)
        {
            if (item is Avalonia.Platform.Storage.IStorageFile sf)
            {
                string path = sf.Path.LocalPath;
                string? real = ResolveShortcut(path);
                if (!string.IsNullOrEmpty(real) && File.Exists(real)) path = real;

                if (File.Exists(path))
                {
                    string folder = Path.GetDirectoryName(path)!;
                    _settings.QF.Max路径 = folder.Replace('\\', '/');
                    SaveWindowSettings();
                    if (sender is TextBox tb) tb.Text = _settings.QF.Max路径;
                    return;
                }
            }
        }
    }

    private string? ResolveShortcut(string file)
    {
        if (Path.GetExtension(file).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell")!;
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic link = shell.CreateShortcut(file);
                return link.TargetPath;
            }
            catch { }
        }
        return null;
    }

    private void OnStartMaxClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_settings.QF.Max路径) || !Directory.Exists(_settings.QF.Max路径))
        {
            ShowMessageAsync("错误", "未设置 3ds Max 安装目录");
            return;
        }
        string exe = Path.Combine(_settings.QF.Max路径, "3dsmax.exe");
        try { Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true }); }
        catch (Exception ex) { ShowMessageAsync("启动失败", ex.Message); }
    }

    private void SaveWindowSettings()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            _settings.Width = Width;
            _settings.Height = Height;
            _settings.PositionX = Position.X;
            _settings.PositionY = Position.Y;
            _settings.WindowState = WindowState;

            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    private void LoadWindowSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                _settings = JsonSerializer.Deserialize<WindowSettings>(json) ?? new WindowSettings();
            }
            else
            {
                _settings = new WindowSettings();
            }

            Width = _settings.Width > 0 ? _settings.Width : Width;
            Height = _settings.Height > 0 ? _settings.Height : Height;
            if (_settings.PositionX >= 0 && _settings.PositionY >= 0)
                Position = new PixelPoint((int)_settings.PositionX, (int)_settings.PositionY);
            WindowState = _settings.WindowState;

            if (this.FindControl<TextBox>("MaxPathTextBox") is TextBox tb)
                tb.Text = _settings.QF.Max路径 ?? string.Empty;

            SaveWindowSettings();
        }
        catch { _settings = new WindowSettings(); }
    }
}

// 数据类
public class QF工具数据
{
    public string Max路径 { get; set; } = string.Empty;
    public string SourceAgentPath { get; set; } = string.Empty;
    public const string Max自启动路径 = "/scripts/Startup";
    public const string Max代理路径 = "/QF/MaxAgent.py";
}

public class WindowSettings
{
    public double Width { get; set; } = 420;
    public double Height { get; set; } = 280;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public WindowState WindowState { get; set; } = WindowState.Normal;
    public QF工具数据 QF { get; set; } = new QF工具数据();
}