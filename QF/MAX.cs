using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ZW_PipelineTool;

public partial class MainWindow
{
    /// <summary>
    /// MaxScript 按钮点击事件：根据按钮的 Tag 属性找到对应的 .ms 文件并发送给 3ds Max
    /// </summary>
    private async void OnScriptButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        // Tag 属性存放脚本文件名（不含路径）
        string? scriptFileName = button.Tag?.ToString()?.Trim();

        if (string.IsNullOrWhiteSpace(scriptFileName))
        {
            Log($"按钮缺少 Tag 属性，无法确定脚本：{button.Content}");
            return;
        }

        try
        {
            // 脚本应放在程序目录下的 MAX 文件夹
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string scriptPath = Path.Combine(exeDir, "MAX", scriptFileName);

            if (!File.Exists(scriptPath))
            {
                Log($"脚本文件不存在：{scriptPath}");
                Log("请检查：");
                Log("1. MAX 文件夹是否在 EXE 同级目录");
                Log($"2. 是否包含文件：{scriptFileName}");
                Log("3. 项目 csproj 已设置 <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>");
                return;
            }

            Log($"准备发送脚本到 3ds Max：{scriptFileName} ({button.Content})");

            // 核心发送逻辑
            SendMsTo3dsMax(scriptPath);

            Log($"脚本已发送：{scriptFileName}（等待 3ds Max 执行）");
        }
        catch (Exception ex)
        {
            Log($"发送脚本失败：{ex.Message}");
        }
    }

    #region P/Invoke 定义 - 与 Windows 窗口和拖放消息交互

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    /// <summary>
    /// 用于模拟文件拖放的结构（WM_DROPFILES 消息需要）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct DROPFILES
    {
        public uint pFiles;     // 文件列表相对于结构开头的偏移
        public POINT pt;        // 拖放点（屏幕坐标）
        public int fNC;         // 是否非客户区（通常为0）
        public int fWide;       // 是否使用宽字符路径（我们用 Unicode）
    }

    private const uint WM_DROPFILES = 0x0233;

    #endregion

    /// <summary>
    /// 核心方法：通过模拟“拖放 .ms 文件到 3ds Max 窗口”来执行 MaxScript
    /// 这是目前最可靠的外部调用 MaxScript 的方式之一（无需 MaxScript Listener 开启）
    /// </summary>
    private void SendMsTo3dsMax(string scriptPath)
    {
        if (!File.Exists(scriptPath))
        {
            Log("脚本文件不存在（异常情况）");
            return;
        }

        // 尝试多种可能的窗口类名/标题来定位 3ds Max 主窗口
        IntPtr hMax = FindWindow("3DSMAX", null);
        if (hMax == IntPtr.Zero) hMax = FindWindow("3dsmax", null);
        if (hMax == IntPtr.Zero) hMax = FindWindow(null, "3ds Max");
        if (hMax == IntPtr.Zero) hMax = FindWindow(null, "Autodesk 3ds Max");

        if (hMax == IntPtr.Zero)
        {
            Log("未找到 3ds Max 主窗口（请确认 3ds Max 已打开）");
            return;
        }

        Log("找到 3ds Max 窗口，正在尝试发送脚本...");

        try
        {
            // 把 3ds Max 窗口置前（很重要，否则 WM_DROPFILES 可能失效）
            SetForegroundWindow(hMax);

            // 获取窗口矩形区域，用于计算中心点坐标
            if (!GetWindowRect(hMax, out RECT rect))
            {
                Log("无法获取 3ds Max 窗口矩形区域");
                return;
            }

            int centerX = rect.Left + (rect.Right - rect.Left) / 2;
            int centerY = rect.Top + (rect.Bottom - rect.Top) / 2;

            // 构造拖放路径（双空字符结尾，Unicode 编码）
            string pathWithDoubleNull = scriptPath + "\0\0";
            byte[] pathBytes = System.Text.Encoding.Unicode.GetBytes(pathWithDoubleNull);

            // 计算全局内存块大小
            int dfSize = Marshal.SizeOf<DROPFILES>();
            int totalSize = dfSize + pathBytes.Length;

            // 分配全局内存（PostMessage 需要 HGLOBAL）
            IntPtr hGlobal = Marshal.AllocHGlobal(totalSize);
            if (hGlobal == IntPtr.Zero)
            {
                Log("全局内存分配失败");
                return;
            }

            try
            {
                var df = new DROPFILES
                {
                    pFiles = (uint)dfSize,           // 文件列表从结构后开始
                    pt = new POINT { X = centerX, Y = centerY },
                    fNC = 0,                         // 客户区拖放
                    fWide = 1                        // 使用宽字符（Unicode）
                };

                // 把 DROPFILES 结构写入内存
                Marshal.StructureToPtr(df, hGlobal, false);

                // 把路径字符串（含双\0）紧跟在结构后面
                Marshal.Copy(pathBytes, 0, hGlobal + dfSize, pathBytes.Length);

                // 发送 WM_DROPFILES 消息给 3ds Max
                bool posted = PostMessage(hMax, WM_DROPFILES, hGlobal, IntPtr.Zero);

                Log(posted
                    ? "已成功发送 WM_DROPFILES 消息 → 3ds Max 应开始执行脚本"
                    : "PostMessage 返回 false（可能 3ds Max 未响应或被最小化）");
            }
            catch (Exception ex)
            {
                Log($"发送脚本时发生异常：{ex.Message}");
            }
            finally
            {
                // 必须释放全局内存，否则会内存泄漏
                Marshal.FreeHGlobal(hGlobal);
            }
        }
        catch (Exception ex)
        {
            Log($"与 3ds Max 交互时发生异常：{ex.Message}");
        }
    }
}