using System;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Text;

namespace ZW_PipelineTool;

public partial class 主窗口 : Window//日志区块
{
    private Expander? 运行日志Expander;       
    private static readonly SemaphoreSlim _logFileSemaphore = new(1, 1);
    public ObservableCollection<日志数据> 日志列表 { get; } = new();

    // MaxScript 日志监控相关字段
    private FileSystemWatcher? _logWatcher;
    private string _logFilePath = string.Empty;

    protected void 日志(string 消息)
    {
        Dispatcher.UIThread.Post(() =>
        {
            日志列表.Add(new 日志数据
            {
                Message = $"[{DateTime.Now:HH:mm:ss}] {消息}",
                Color = "Black"
            });
            ScrollToEnd();
        });
    }

    protected void 报错(string 消息)
    {
        Dispatcher.UIThread.Post(() =>
        {
            日志列表.Add(new 日志数据
            {
                Message = $"[{DateTime.Now:HH:mm:ss}] {消息}",
                Color = "Red"
            });
            ScrollToEnd();
        });
    }

    private void ScrollToEnd()
    {
        if (LogListBox is null) return;

        var scrollViewer = LogListBox.FindDescendantOfType<ScrollViewer>();
        scrollViewer?.ScrollToEnd();
    }


    // ========== Max 日志监控相关方法 ==========
    private void StartMaxLogWatcher()
    {
        try
        {
            if (string.IsNullOrEmpty(_logFilePath))
            {
                日志("[警告] 日志文件路径为空，无法启动监控");
                return;
            }

            string? dir = Path.GetDirectoryName(_logFilePath);

            // null 安全处理（CS8604 修复）
            if (dir is null)
            {
                日志("[错误] 无法获取日志文件目录");
                return;
            }

            // 现在 dir 已被确认非 null
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);  // 使用 ! 告诉编译器已检查
            }

            _logWatcher = new FileSystemWatcher
            {
                Path = dir,
                Filter = "Max_Log.txt",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _logWatcher.Changed += OnMaxLogChanged;
            _logWatcher.Created += OnMaxLogChanged;

            日志($"日志记录激活（监控: {_logFilePath}）");
        }
        catch (Exception ex)
        {
            日志($"[系统] Max 日志监控启动失败：{ex.Message}");
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
                            日志("┌──── MaxScript 日志 ──────");
                            日志(content.Trim());
                            日志("└──────────────────────────");
                        }
                        break; // 成功则退出循环
                    }
                    catch (IOException ioEx) when (ioEx.Message.Contains("being used by another process") ||
                                                    ioEx.Message.Contains("正由另一进程使用"))
                    {
                        if (i == maxRetries - 1)
                        {
                            日志($"读取 Max 日志文件失败（重试 {maxRetries} 次后）：{ioEx.Message}");
                        }
                        else
                        {
                            await Task.Delay(retryDelay);
                        }
                    }
                    catch (Exception ex)
                    {
                        日志($"读取 Max 日志文件失败：{ex.Message}");
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
}

public class 日志数据
{
    public required string Message { get; set; }
    public string Color { get; set; } = "Black";
}