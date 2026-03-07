using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;  // ContentDialog 在这里
using System.Threading.Tasks;

namespace ZW_PipelineTool
{

    public partial class 主窗口//调试分块
    {
        private void 打开文件收集器_Click(object? sender, RoutedEventArgs e)
        {
            var 收集器窗口 = new FileCollectorWindow();
            收集器窗口.Show();          // 非模态打开
            // 或者使用 ShowDialog(this) 作为模态对话框
        }
    }

    public partial class FileCollectorWindow : Window
    {
        public ObservableCollection<FileItem> Items { get; } = new();
        public FileItem? SelectedItem { get; set; }

        public FileCollectorWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        // 拖拽进入
        private void OnDragEnter(object? sender, DragEventArgs e)
        {
#pragma warning disable CS0618
            e.DragEffects = e.Data.Contains(DataFormats.Files)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
#pragma warning restore CS0618
        }

        // 拖拽释放
        private async void OnDrop(object? sender, DragEventArgs e)
        {
#pragma warning disable CS0618
            if (!e.Data.Contains(DataFormats.Files))
                return;

            var storageItems = e.Data.GetFiles();
#pragma warning restore CS0618

            if (storageItems == null || !storageItems.Any())
                return;

            var paths = new List<string>();
            foreach (var item in storageItems)
            {
                var path = item.TryGetLocalPath();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    paths.Add(path);
                }
            }

            if (paths.Count == 0) return;

            foreach (var path in paths)
            {
                Items.Add(new FileItem { FullPath = path });
            }
        }

      private async void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (Items.Count == 0)
            {
                UpdateStatus("没有文件可复制");
                return;
            }

            var sb = new StringBuilder();
            int index = 1;
            foreach (var item in Items)
            {
                string content;
                try
                {
                    content = await File.ReadAllTextAsync(item.FullPath);
                }
                catch (Exception ex)
                {
                    content = $"[读取失败: {ex.Message}]";
                }

                sb.AppendLine($"{index}、{item.FileName}");
                sb.AppendLine(content);
                sb.AppendLine(); // 空行分隔
                index++;
            }

            var text = sb.ToString().TrimEnd();

            // 方案1：弹出消息框，让用户手动复制（最稳）
            var dialog = new ContentDialog
            {
                Title = "文件内容已生成",
                Content = new TextBox
                {
                    Text = text,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Height = 400,
                    IsReadOnly = false,
                    FontFamily = new FontFamily("Consolas"),
                    Padding = new Thickness(8)
                },
                PrimaryButtonText = "复制到剪贴板",
                CloseButtonText = "关闭",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync(this);

            if (result == ContentDialogResult.Primary)
            {
                // 用户点了“复制到剪贴板”，再尝试写入
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    try
                    {
                        await clipboard.SetTextAsync(text);
                        UpdateStatus("已复制到剪贴板");
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus($"复制失败：{ex.Message}");
                    }
                }
                else
                {
                    UpdateStatus("无法访问剪贴板");
                }
            }
            else
            {
                UpdateStatus("已关闭（可手动选中文本复制）");
            }
        }

        private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is FileItem item)
            {
                Items.Remove(item);
            }
        }

        private void OnListBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && FileListBox.SelectedItem is FileItem selected)
            {
                Items.Remove(selected);
                e.Handled = true;
            }
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }
    }

    public class FileItem
    {
        public required string FullPath { get; set; }
        public string FileName => Path.GetFileName(FullPath);
    }
}