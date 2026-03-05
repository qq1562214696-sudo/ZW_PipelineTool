using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms; // for simple message boxes when Avalonia cannot start

namespace ZW_PipelineTool
{
    internal class Program
    {
        // Avalonia 配置入口点，不要删除或修改
        [STAThread]
        public static void Main(string[] args)
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.Error.WriteLine("This application only runs on Windows.");
                // optionally log as well
                try { File.AppendAllText("crash.log", $"[{DateTime.Now}] Non-Windows launch prevented\n"); } catch { }
                try { MessageBox.Show("This application only runs on Windows.", "Startup error", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
                // never exit automatically; keep process alive until user kills it
                Thread.Sleep(Timeout.Infinite);
            }

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (PlatformNotSupportedException ex)
            {
                var msg = "Unable to start UI. This build must be run on a Windows desktop environment.";
                Console.Error.WriteLine(msg + "\n" + ex.Message);
                try { File.AppendAllText("crash.log", $"[{DateTime.Now}] UI start failed: {ex}\n"); } catch { }
                try { MessageBox.Show(msg + "\n" + ex.Message, "Startup error", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
                // keep process alive indefinitely
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                // fallback catch to ensure we log unexpected problems
                Console.Error.WriteLine("Application failed to start: " + ex);
                try { File.AppendAllText("crash.log", $"[{DateTime.Now}] Startup exception: {ex}\n"); } catch { }
                try { MessageBox.Show("Application failed to start:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
                // keep process alive indefinitely
                Thread.Sleep(Timeout.Infinite);
            }
        }

        // Avalonia 配置，同样不要删除或修改
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
                // 注意：去掉了 .UseReactiveUI()，因为你项目没安装这个包

        static Program()
        {
            // 1. 非 UI 线程的未处理异常（AppDomain 级别）
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                if (ex == null) return;

                string errorMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] AppDomain 未捕获异常:\n{ex}\n\n";

                // 写到日志文件
                try
                {
                    File.AppendAllText("crash.log", errorMsg);
                }
                catch { }

                // 输出到控制台（调试时可见）
                Console.WriteLine(errorMsg);

                // AppDomain.CurrentDomain.UnhandledException 的事件无法设置 IsTerminating（只读）
                // 所以我们只能记录日志，程序会自动终止
            };

            // 2. UI 线程的未处理异常（Dispatcher 级别） - 最关键，防止按钮闪退
            Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (sender, e) =>
            {
                string errorMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UI线程未捕获异常:\n{e.Exception}\n\n";

                // 写到日志文件
                try
                {
                    File.AppendAllText("crash.log", errorMsg);
                }
                catch { }

                // 输出到控制台
                Console.WriteLine(errorMsg);

                // 重要：Handled = true 防止程序直接闪退
                e.Handled = true;

                // 可选：在这里弹出提示（需引入 Avalonia.Controls 和 MessageBox）
                // Avalonia.Controls.MessageBox.Show(
                //     "操作失败：" + e.Exception.Message + "\n已记录到 crash.log",
                //     "错误",
                //     icon: MessageBox.Avalonia.MessageBoxIcon.Error);
            };
        }
    }
}