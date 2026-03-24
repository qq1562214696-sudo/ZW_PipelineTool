using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.ComponentModel;

namespace ZW_PipelineTool;

public partial class 设置窗口 : Window, INotifyPropertyChanged
{
    // 饮水提醒相关字段（复用主窗口的逻辑）
    private DispatcherTimer? 饮水提醒定时器;
    private 饮水提醒? 饮水提醒弹窗;   // 假设你已有 饮水提醒 类

    private bool _开启饮水提醒;
    public bool 开启饮水提醒
    {
        get => _开启饮水提醒;
        set
        {
            if (_开启饮水提醒 != value)
            {
                _开启饮水提醒 = value;
                OnPropertyChanged(nameof(开启饮水提醒));
                // 可以在这里同步回主窗口的 _窗口数据，如果需要共享
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public 设置窗口()
    {
        InitializeComponent();
        DataContext = this;   // 让绑定生效

        // 查找控件（和主窗口一样）
        var targetNumeric = this.FindControl<NumericUpDown>("DrinkTargetNumeric");
        var intervalNumeric = this.FindControl<NumericUpDown>("ReminderIntervalNumeric");
        var progressBar = this.FindControl<ProgressBar>("DrinkProgressBar");
        var progressText = this.FindControl<TextBlock>("DrinkProgressText");

        // 加载已保存的数值（从主窗口数据或单独存储，这里示例从主窗口共享）
        // 如果你想让设置窗口独立保存，可以再加一个 json 文件

        SetupDrinkReminder();   // 复用你已有的饮水提醒初始化方法（如果在主窗口有，就复制或提取成公共方法）
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // 应用设置按钮事件（你 XAML 里已绑定）
    private void ApplyDrinkSettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var targetNumeric = this.FindControl<NumericUpDown>("DrinkTargetNumeric");
        var intervalNumeric = this.FindControl<NumericUpDown>("ReminderIntervalNumeric");

        if (targetNumeric?.Value is decimal target && intervalNumeric?.Value is decimal interval)
        {
            // 更新主窗口的数据（推荐通过事件或静态/单例传递）
            // 这里简单示例：你可以把主窗口实例传进来，或者用 Messenger
            日志($"饮水设置已应用 → 目标: {target}ml, 间隔: {interval}分钟");

            // 刷新进度条（示例）
            UpdateDrinkProgressUI((double)target);
        }
    }

    // 复用你主窗口的饮水提醒方法（复制或提取到公共类）
    private void SetupDrinkReminder()
    {
        // ... 你原有的饮水提醒定时器初始化代码 ...
        // OnDrinkReminderCheckChanged 等事件也可以在这里处理
    }

    private void UpdateDrinkProgressUI(double max = 2000)
    {
        var progressBar = this.FindControl<ProgressBar>("DrinkProgressBar");
        var progressText = this.FindControl<TextBlock>("DrinkProgressText");

        if (progressBar != null) progressBar.Maximum = max;
        if (progressText != null) progressText.Text = $"0 / {max} ml (0%)";
    }

    private void OnDrinkReminderCheckChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // 处理开关变化
        日志($"饮水提醒已{(开启饮水提醒 ? "开启" : "关闭")}");
    }

    protected void 日志(string 消息)
    {
        // 可以调用主窗口的日志，或者在这里简单 Console.WriteLine
        Console.WriteLine($"[设置窗口] {DateTime.Now:HH:mm:ss} {消息}");
        // 推荐：把主窗口的日志方法提取成静态或通过事件传递
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}