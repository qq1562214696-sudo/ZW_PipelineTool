using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Text.Json;

namespace ZW_PipelineTool;

public partial class MainWindow : Window
{
    // 不再手动声明 statusText 和 pathDisplay（XAML 已生成）
    // 如果需要访问，直接用 this.statusText 或 FindControl

    private const string SettingsFileName = "window_settings.json";

    // 相对路径：当前工作目录下的 AppData 文件夹
    private static readonly string SettingsDirectory;
    private static readonly string SettingsPath;

    static MainWindow()
    {
        string baseDirectory = Environment.CurrentDirectory;

        string? exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(exePath))
        {
            string? exeDir = Path.GetDirectoryName(exePath);
            if (!string.IsNullOrEmpty(exeDir) && Directory.Exists(exeDir))
            {
                baseDirectory = exeDir;
            }
        }

        SettingsDirectory = Path.Combine(baseDirectory, "AppData");
        SettingsPath = Path.Combine(SettingsDirectory, SettingsFileName);
    }

    public MainWindow()
    {
        InitializeComponent();
        LoadWindowSettings();

        Closing += (s, e) => SaveWindowSettings();

        // 查找 XAML 里定义的控件（可 null 安全）
        var statusTextCtrl = this.FindControl<TextBlock>("statusText");
        var pathDisplayCtrl = this.FindControl<TextBlock>("pathDisplay");

        // 调试显示路径（如果控件存在）
        if (pathDisplayCtrl != null)
        {
            pathDisplayCtrl.Text = $"数据保存路径：{SettingsPath}";
        }
    }

    private void OnTestButtonClick(object? sender, RoutedEventArgs e)
    {
        var statusTextCtrl = this.FindControl<TextBlock>("statusText");
        if (statusTextCtrl != null)
        {
            statusTextCtrl.Text = $"你点击了按钮！当前时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }
    }

    private void SaveWindowSettings()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);

            var settings = new WindowSettings
            {
                Width = Width,
                Height = Height,
                PositionX = Position.X,
                PositionY = Position.Y,
                WindowState = WindowState
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);

            var status = this.FindControl<TextBlock>("statusText");
            if (status != null)
                status.Text = "设置已保存！";
        }
        catch (Exception ex)
        {
            var status = this.FindControl<TextBlock>("statusText");
            if (status != null)
                status.Text = $"保存失败：{ex.Message}";
        }
    }

    private void LoadWindowSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<WindowSettings>(json);
            if (settings == null) return;

            Width = settings.Width > 0 ? settings.Width : Width;
            Height = settings.Height > 0 ? settings.Height : Height;

            if (settings.PositionX >= 0 && settings.PositionY >= 0)
            {
                Position = new PixelPoint((int)settings.PositionX, (int)settings.PositionY);
            }

            WindowState = settings.WindowState;
        }
        catch (Exception ex)
        {
            var status = this.FindControl<TextBlock>("statusText");
            if (status != null)
                status.Text = $"加载失败：{ex.Message}";
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

public class WindowSettings
{
    public double Width { get; set; } = 600;
    public double Height { get; set; } = 400;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public WindowState WindowState { get; set; } = WindowState.Normal;
}