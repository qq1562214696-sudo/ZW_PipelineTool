using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ZW_PipelineTool;

public partial class MainWindow
{
    // ── MaxScript 按钮执行 ────────────────────────────────────────

    private async void OnScriptButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        string? scriptFileName = button.Tag?.ToString()?.Trim();

        if (string.IsNullOrWhiteSpace(scriptFileName))
        {
            Log($"按钮缺少 Tag 属性，无法确定脚本：{button.Content}");
            return;
        }

        try
        {
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
            SendMsTo3dsMax(scriptPath);
            Log($"脚本已发送：{scriptFileName}（等待 3ds Max 执行）");
        }
        catch (Exception ex)
        {
            Log($"发送脚本失败：{ex.Message}");
        }
    }

    // P/Invoke 相关定义
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

    [StructLayout(LayoutKind.Sequential)]
    private struct DROPFILES
    {
        public uint pFiles;
        public POINT pt;
        public int fNC;   // BOOL -> int
        public int fWide; // BOOL -> int
    }

    private const uint WM_DROPFILES = 0x0233;

    private void SendMsTo3dsMax(string scriptPath)
    {
        if (!File.Exists(scriptPath))
        {
            Log("脚本文件不存在（异常情况）");
            return;
        }

        IntPtr hMax = FindWindow("3DSMAX", null);
        if (hMax == IntPtr.Zero) hMax = FindWindow("3dsmax", null);
        if (hMax == IntPtr.Zero) hMax = FindWindow(null, "3ds Max");
        if (hMax == IntPtr.Zero) hMax = FindWindow(null, "Autodesk 3ds Max");

        if (hMax == IntPtr.Zero)
        {
            Log("未找到 3ds Max 主窗口");
            return;
        }

        Log("找到 3ds Max 窗口，正在尝试发送脚本...");

        try
        {
            SetForegroundWindow(hMax);

            if (!GetWindowRect(hMax, out RECT rect))
            {
                Log("无法获取 3ds Max 窗口矩形区域");
                return;
            }

            int centerX = rect.Left + (rect.Right - rect.Left) / 2;
            int centerY = rect.Top + (rect.Bottom - rect.Top) / 2;

            string pathWithDoubleNull = scriptPath + "\0\0";
            byte[] pathBytes = System.Text.Encoding.Unicode.GetBytes(pathWithDoubleNull);

            int dfSize = Marshal.SizeOf<DROPFILES>();
            int totalSize = dfSize + pathBytes.Length;

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
                    pFiles = (uint)dfSize,
                    pt = new POINT { X = centerX, Y = centerY },
                    fNC = 0,
                    fWide = 1
                };

                Marshal.StructureToPtr(df, hGlobal, false);
                Marshal.Copy(pathBytes, 0, hGlobal + dfSize, pathBytes.Length);

                bool posted = PostMessage(hMax, WM_DROPFILES, hGlobal, IntPtr.Zero);

                Log(posted
                    ? "已成功发送 WM_DROPFILES 消息 → 3ds Max 应开始执行脚本"
                    : "PostMessage 返回 false（可能 3ds Max 未响应）");
            }
            catch (Exception ex)
            {
                Log($"发送脚本时发生异常：{ex.Message}");
                Marshal.FreeHGlobal(hGlobal);
            }
        }
        catch (Exception ex)
        {
            Log($"与 3ds Max 交互时发生异常：{ex.Message}");
        }
    }
}