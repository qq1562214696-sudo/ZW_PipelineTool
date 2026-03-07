using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ZW_PipelineTool;

public partial class MainWindow
{
    // Unity 端命名管道名称（需与 Unity 脚本中一致）
    private const string UnityPipeName = "ZW_UnityPipelineTool";

    /// <summary>
    /// 通过命名管道向 Unity 发送简单 JSON 格式的命令
    /// </summary>
    /// <param name="command">命令名称</param>
    /// <param name="value">可选参数值</param>
    private async Task SendUnityCommand(string command, string? value = null)
    {
        try
        {
            // 连接到 Unity 打开的命名管道服务器
            await using var client = new NamedPipeClientStream(".", UnityPipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            // 最多等待 5 秒
            await client.ConnectAsync(5000);

            var payload = new Dictionary<string, string?>
            {
                ["command"] = command
            };
            if (!string.IsNullOrEmpty(value))
                payload["value"] = value;

            string json = JsonSerializer.Serialize(payload);
            byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");

            await client.WriteAsync(bytes, 0, bytes.Length);
            await client.FlushAsync();

            Log($"已发送命令到 Unity: {command}{(value != null ? $" 值: {value}" : "")}");
        }
        catch (TimeoutException)
        {
            Log("Unity 未响应（请确认 Unity 编辑器已打开，且 ZW_UnityPipelineTool.cs 已加载）");
        }
        catch (Exception ex)
        {
            Log($"发送命令失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从系统剪贴板异步读取文本（Avalonia 方式）
    /// </summary>
    private async Task<string?> GetClipboardTextAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard == null)
        {
            Log("无法访问剪贴板（窗口未激活或 Clipboard 服务不可用）");
            return null;
        }

        try
        {
            return await topLevel.Clipboard.GetTextAsync();
        }
        catch (Exception ex)
        {
            Log($"读取剪贴板失败: {ex.Message}");
            return null;
        }
    }

    #region Unity 相关按钮事件

    private async void Btn_OpenAvatarEditor_Click(object? sender, RoutedEventArgs e)
    {
        await SendUnityCommand("OpenAvatarEditor");
    }

    private async void Btn_CopyAndExport_Click(object? sender, RoutedEventArgs e)
    {
        await SendUnityCommand("CopyAndOpenExportWindow");
    }

    /// <summary>
    /// 从剪贴板读取内容并发送给 Unity（通常是前面 QF 整理得到的命名列表）
    /// </summary>
    private async void Btn_PasteMaxName_Click(object? sender, RoutedEventArgs e)
    {
        string? clipText = await GetClipboardTextAsync();
        if (string.IsNullOrWhiteSpace(clipText))
        {
            Log("剪贴板为空或读取失败");
            return;
        }

        string cleaned = clipText.Trim();
        await SendUnityCommand("AddMaxFileName", cleaned);
        Log($"已请求添加 Max 文件名: {cleaned}");
    }

    private async void Btn_StandardizeFiles_Click(object? sender, RoutedEventArgs e)
    {
        await SendUnityCommand("StandardizeFiles");
    }

    private async void Btn_ExportBatch_Click(object? sender, RoutedEventArgs e)
    {
        await SendUnityCommand("ExportBatch");
    }

    private async void Btn_ClearMaxNames_Click(object? sender, RoutedEventArgs e)
    {
        await SendUnityCommand("ClearMaxFileNames");
        Log("已请求清空 Unity 端的 Max 文件名列表");
    }

    #endregion
}