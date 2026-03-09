using System;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using System.Threading;
using System.IO;

namespace ZW_PipelineTool;

public partial class 主窗口 : Window
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
}

public class 日志数据
{
    public required string Message { get; set; }
    public string Color { get; set; } = "Black";
}