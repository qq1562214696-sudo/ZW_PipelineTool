using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Linq;

namespace ZW_PipelineTool;

public partial class 主窗口 : Window//交互区块
{
    private void 初始化按钮_点击(object? sender, RoutedEventArgs e)
    {
        日志("初始化按钮被点击（待实现具体逻辑）");
    }

    private void 窗口_拖入(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void 窗口_拖拽中(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    protected async void 窗口_放下(object? sender, DragEventArgs e)
    {
        var 文件列表 = e.DataTransfer?.TryGetFiles();  // 使用 DataTransfer 并处理可能的 null

        if (文件列表 == null || !文件列表.Any())
        {
            日志("未检测到任何文件/文件夹");
            return;
        }
        var 第一个项目 = 文件列表.First();
        
        var 路径 = 第一个项目?.TryGetLocalPath(); 
        if (string.IsNullOrEmpty(路径))
        {
            日志("无法获取本地路径");
            return;
        }
        if (!Directory.Exists(路径))
        {
            日志($"拖入的不是文件夹：{路径}");
            return;
        }
        日志($"成功接收文件夹：{路径}");
        var 路径文本框 = this.FindControl<TextBox>("FolderPathTextBox");
        if (路径文本框 != null)
            路径文本框.Text = 路径;
        await 规范整理文件夹(路径);
        e.Handled = true;
    } 
}