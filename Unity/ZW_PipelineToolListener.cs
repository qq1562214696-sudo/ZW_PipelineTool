using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity 管道监听服务 - 编辑器启动后自动运行，持续接收桌面工具命令
/// </summary>
[InitializeOnLoad]
public static class ZW_PipelineToolListener
{
    private const string PipeName = "ZW_PipelineTool";
    private static CancellationTokenSource _cts;
    private static Task _listenTask;

    // 静态构造函数：编辑器加载时自动执行
    static ZW_PipelineToolListener()
    {
        StartListening();
        EditorApplication.quitting += StopListening;
        AssemblyReloadEvents.beforeAssemblyReload += StopListening; // 编译前停止
        AssemblyReloadEvents.afterAssemblyReload += StartListening; // 编译后重启
    }

    private static void StartListening()
    {
        if (_cts != null) return; // 已在运行

        _cts = new CancellationTokenSource();
        _listenTask = Task.Run(ListenLoop, _cts.Token);
        Debug.Log("[ZW管道] 监听服务已启动");
    }

    private static void StopListening()
    {
        if (_cts == null) return;

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
        try
        {
            _listenTask?.Wait(1000);
        }
        catch (AggregateException) { }
        _listenTask = null;
        Debug.Log("[ZW管道] 监听服务已停止");
    }

    private static async Task ListenLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                Debug.Log("[ZW管道] 等待桌面工具连接...");
                await server.WaitForConnectionAsync(_cts.Token);

                if (_cts.IsCancellationRequested) break;

                Debug.Log("[ZW管道] 已连接，读取命令");
                using (var reader = new StreamReader(server, Encoding.UTF8, true, 1024, false))
                {
                    string? line = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                    {
                        // 将命令调度到 Unity 主线程执行
                        EditorApplication.delayCall += () => ProcessCommand(line);
                    }
                }
                server.Disconnect();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ZW管道] 监听异常：{ex.Message}");
                await Task.Delay(1000); // 避免死循环
            }
        }
    }

    private static void ProcessCommand(string json)
    {
        try
        {
            var cmd = JsonUtility.FromJson<PipeCommand>(json);
            if (cmd == null) return;

            Debug.Log($"[ZW管道] 执行命令：{cmd.command} 值：{cmd.value}");

            switch (cmd.command)
            {
                case "UnityInitialize":
                    Debug.Log("Unity 初始化：可在此执行资源路径校验等操作");
                    break;

                case "OpenAvatarEditor":
                    // 打开自定义头像编辑器窗口，例如：
                    // AvatarEditorWindow.OpenWindow();
                    Debug.Log("打开 Avatar 编辑器");
                    break;

                case "CopyAndOpenExportWindow":
                    // 复制选中资源并打开导出窗口
                    // CopySelectedAndOpenExport();
                    Debug.Log("复制选中并打开导出窗口");
                    break;

                case "AddMaxFileName":
                    // 接收从桌面工具粘贴的 Max 文件名
                    Debug.Log($"收到 Max 文件名：{cmd.value}");
                    // 将文件名存入列表或临时文件，供后续导出使用
                    // MaxFileNameList.Add(cmd.value);
                    break;

                case "StandardizeFiles":
                    Debug.Log("执行文件标准化");
                    break;

                case "ExportBatch":
                    Debug.Log("执行批量导出");
                    break;

                case "ClearMaxFileNames":
                    Debug.Log("清空 Max 文件名列表");
                    // MaxFileNameList.Clear();
                    break;

                // 可添加“小岛植物”项目专用命令
                case "IslandOpenAvatarEditor":
                    Debug.Log("小岛项目 - 打开头像编辑器");
                    break;

                default:
                    Debug.LogWarning($"[ZW管道] 未知命令：{cmd.command}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ZW管道] 命令处理异常：{ex.Message}\n原始JSON：{json}");
        }
    }

    [Serializable]
    private class PipeCommand
    {
        public string command;
        public string value;
    }
}