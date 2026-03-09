using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace ZW_PipelineTool;

public partial class 主窗口
{
    // Unity 端命名管道名称（必须与 Unity 脚本中完全一致）
    private const string Unity管道名称 = "ZW_UnityPipelineTool";

    /// <summary>
    /// 通过命名管道向 Unity 发送简单的 JSON 格式命令
    /// </summary>
    /// <param name="命令">命令名称</param>
    /// <param name="参数值">可选的参数值</param>
    private async Task 发送Unity命令(string 命令, string? 参数值 = null)
    {
        try
        {
            // 连接到 Unity 打开的命名管道服务器
            await using var 客户端 = new NamedPipeClientStream(".", Unity管道名称, PipeDirection.Out, PipeOptions.Asynchronous);
            
            // 最多等待 5 秒连接成功
            await 客户端.ConnectAsync(5000);

            var 数据包 = new Dictionary<string, string?>
            {
                ["command"] = 命令
            };
            
            if (!string.IsNullOrEmpty(参数值))
            {
                数据包["value"] = 参数值;
            }

            string json = JsonSerializer.Serialize(数据包);
            byte[] 字节数据 = Encoding.UTF8.GetBytes(json + "\n");

            await 客户端.WriteAsync(字节数据, 0, 字节数据.Length);
            await 客户端.FlushAsync();

            日志($"已向 Unity 发送命令：{命令}{(参数值 != null ? $"（值：{参数值}）" : "")}");
        }
        catch (TimeoutException)
        {
            日志("Unity 未响应，请确认：");
            日志("1. Unity 编辑器已打开");
            日志("2. ZW_UnityPipelineTool.cs 脚本已正确挂载并运行");
            日志("3. 命名管道名称完全一致");
        }
        catch (Exception ex)
        {
            日志($"发送命令到 Unity 失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 从系统剪贴板异步读取文本（Avalonia 方式）
    /// </summary>
    private async Task<string?> 获取剪贴板文本Async()
    {
        var 顶层窗口 = TopLevel.GetTopLevel(this);
        if (顶层窗口?.Clipboard == null)
        {
            日志("无法访问系统剪贴板（窗口未激活或剪贴板服务不可用）");
            return null;
        }

        try
        {
            return await 顶层窗口.Clipboard.TryGetTextAsync();  // ← 改成 TryGetTextAsync
        }
        catch (Exception ex)
        {
            日志($"读取剪贴板失败：{ex.Message}");
            return null;
        }
    }

    #region Unity 相关按钮点击事件

    private async void 打开头像编辑器按钮_点击(object? sender, RoutedEventArgs e)
    {
        await 发送Unity命令("OpenAvatarEditor");
    }

    private async void 复制并导出按钮_点击(object? sender, RoutedEventArgs e)
    {
        await 发送Unity命令("CopyAndOpenExportWindow");
    }

    /// <summary>
    /// 从剪贴板读取内容（通常是前面 QF 整理得到的命名列表），然后发送给 Unity
    /// </summary>
    private async void 粘贴Max名称按钮_点击(object? sender, RoutedEventArgs e)
    {
        string? 剪贴板文本 = await 获取剪贴板文本Async();
        if (string.IsNullOrWhiteSpace(剪贴板文本))
        {
            日志("剪贴板为空或读取失败");
            return;
        }

        string 清理后文本 = 剪贴板文本.Trim();
        await 发送Unity命令("AddMaxFileName", 清理后文本);
        日志($"已请求 Unity 添加 Max 文件名：{清理后文本}");
    }

    private async void 文件标准化按钮_点击(object? sender, RoutedEventArgs e)
    {
        await 发送Unity命令("StandardizeFiles");
    }

    private async void 批量导出按钮_点击(object? sender, RoutedEventArgs e)
    {
        await 发送Unity命令("ExportBatch");
    }

    private async void 清空Max名称按钮_点击(object? sender, RoutedEventArgs e)
    {
        await 发送Unity命令("ClearMaxFileNames");
        日志("已请求 Unity 清空 Max 文件名列表");
    }

    #endregion
}