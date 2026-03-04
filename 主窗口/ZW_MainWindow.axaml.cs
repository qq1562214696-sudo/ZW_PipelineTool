using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.IO;
using System.Text.Json;

namespace ZW_PipelineTool;

public partial class MainWindow : Window
{
    private const string SettingsFileName = "window_settings.json";
    private static readonly string SettingsPath;

    private WindowSettings _settings = new WindowSettings();

    static MainWindow()
    {
        string appDataDir = Path.Combine(AppContext.BaseDirectory, "AppData");
        Directory.CreateDirectory(appDataDir);
        SettingsPath = Path.Combine(appDataDir, SettingsFileName);
    }

    public MainWindow()
    {
        InitializeComponent();
        LoadWindowSettings();

        Closing += (s, e) => SaveWindowSettings();
        this.Topmost = true;  // 默认置顶
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // 置顶开关事件（需要在 XAML 中绑定）
    private void ToggleTopmost_Checked(object? sender, RoutedEventArgs e)
    {
        this.Topmost = true;
    }

    private void ToggleTopmost_Unchecked(object? sender, RoutedEventArgs e)
    {
        this.Topmost = false;
    }

    private void SaveWindowSettings()
    {
        try
        {
            _settings.Width = Width;
            _settings.Height = Height;
            _settings.PositionX = Position.X;
            _settings.PositionY = Position.Y;
            _settings.WindowState = WindowState;
            _settings.Topmost = Topmost;

            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* 静默失败 */ }
    }

    private void LoadWindowSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;

            string json = File.ReadAllText(SettingsPath);
            _settings = JsonSerializer.Deserialize<WindowSettings>(json) ?? new WindowSettings();

            Width = _settings.Width > 0 ? _settings.Width : Width;
            Height = _settings.Height > 0 ? _settings.Height : Height;

            if (_settings.PositionX >= 0 && _settings.PositionY >= 0)
            {
                Position = new PixelPoint((int)_settings.PositionX, (int)_settings.PositionY);
            }

            WindowState = _settings.WindowState;
            Topmost = _settings.Topmost;
        }
        catch
        {
            _settings = new WindowSettings();
        }
    }
}

public class WindowSettings
{
    public double Width { get; set; } = 400;
    public double Height { get; set; } = 400;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public WindowState WindowState { get; set; } = WindowState.Normal;
    public bool Topmost { get; set; } = true;
}