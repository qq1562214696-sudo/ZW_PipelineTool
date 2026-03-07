using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace ZW_PipelineTool
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // 捕获 AppDomain 级别的未处理异常（后台线程异常）
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                string log = $"UnhandledException: {e.ExceptionObject}";
                File.AppendAllText("crash.log", log + Environment.NewLine);
            };

            // 捕获 UI 线程上的未处理异常
            Dispatcher.UIThread.UnhandledException += (sender, e) =>
            {
                string log = $"Dispatcher UnhandledException: {e.Exception}";
                File.AppendAllText("crash_dispatcher.log", log + Environment.NewLine);
                e.Handled = true; // 阻止程序崩溃
            };

            // 处理桌面应用程序生命周期
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new 主窗口();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}