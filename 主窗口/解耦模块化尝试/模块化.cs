using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ZW_PipelineTool
{
    public partial class 主窗口
    {
        private void OpenSettingsWindow_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new 设置窗口
                {
                    // 可选：让设置窗口和主窗口保持相同的置顶状态
                    Topmost = this.Topmost,
                    // 可选：窗口大小或位置微调
                    Width = 520,
                    Height = 780
                };

                // 非模态弹出（推荐，两个窗口可以同时操作）
                settingsWindow.Show(this);

                // 如果希望模态（阻塞主窗口），使用下面这行代替上面一行：
                // await settingsWindow.ShowDialog(this);
            }
            catch (Exception ex)
            {
                日志($"打开设置窗口失败：{ex.Message}");
            }
        }
    }
}