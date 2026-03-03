using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Avalonia.Platform.Storage;

namespace ZW_PipelineTool;

public partial class MainWindow//QF工具PS1部分
{
    private async void OnProcessButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        button.IsEnabled = false;
        try
        {
            await ProcessFolderAsync();
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private async Task ProcessFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "请选择总文件夹（包含 Assets 和 截图）",
            AllowMultiple = false
        });

        if (folders.Count == 0)
            return;

        var selectedFolder = folders[0].TryGetLocalPath();
        if (string.IsNullOrEmpty(selectedFolder))
            return;

        // 实例化处理器并订阅事件
        var processor = new FolderProcessor();
        processor.LogMessage += (msg, color) => AddLog(msg, color);
        processor.ShowMessage += (msg, title, buttons) => ShowMessageBox(msg, title, buttons);

        // 在后台线程执行耗时操作
        await Task.Run(() => processor.Process(selectedFolder));
    }

    private void AddLog(string message, string color = "Black")
    {
        Dispatcher.UIThread.Post(() =>
        {
            var logList = this.FindControl<ListBox>("logList");
            if (logList is null) return;

            logList.Items?.Add($"[{DateTime.Now:HH:mm:ss}] {message}");

            if (logList.Items?.Count > 0)
            {
                logList.ScrollIntoView(logList.Items[^1]);
            }
        });
    }

private async Task<bool> ShowMessageBox(string message, string title, MessageBoxButtons buttons)
{
    var enumButtons = buttons switch
    {
        MessageBoxButtons.OK => ButtonEnum.Ok,
        MessageBoxButtons.OKCancel => ButtonEnum.OkCancel,
        _ => ButtonEnum.Ok
    };

    var msgBox = MessageBoxManager
        .GetMessageBoxStandard(
            title,
            message,
            enumButtons,
            MsBox.Avalonia.Enums.Icon.Success   // ← 用全限定名，避免歧义
        );

    var result = await msgBox.ShowWindowDialogAsync(this);

    return result is ButtonResult.Ok or ButtonResult.Yes;
}
}

public enum MessageBoxButtons
{
    OK,
    OKCancel
}