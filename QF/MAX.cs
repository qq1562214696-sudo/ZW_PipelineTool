using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ZW_PipelineTool;

public partial class 主窗口
{
    /// <summary>
    /// MaxScript 按钮点击事件：根据按钮的 Tag 属性找到对应的 .ms 文件并发送给 3ds Max
    /// </summary>
    private async void 脚本按钮_点击(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        // Tag 属性存放脚本文件名（不含路径）
        string? 脚本文件名 = button.Tag?.ToString()?.Trim();

        if (string.IsNullOrWhiteSpace(脚本文件名))
        {
            记录日志($"按钮缺少 Tag 属性，无法确定脚本：{button.Content}");
            return;
        }

        try
        {
            // 脚本应放在程序目录下的 MAX 文件夹
            string 程序目录 = AppDomain.CurrentDomain.BaseDirectory;
            string 脚本路径 = Path.Combine(程序目录, "MAX", 脚本文件名);

            if (!File.Exists(脚本路径))
            {
                记录日志($"脚本文件不存在：{脚本路径}");
                记录日志("请检查以下几点：");
                记录日志("1. 是否在程序目录下创建了 MAX 文件夹");
                记录日志($"2. 是否包含文件：{脚本文件名}");
                记录日志("3. 项目中该文件属性已设置为“始终复制到输出目录”");
                return;
            }

            记录日志($"准备发送脚本到 3ds Max：{脚本文件名} ({button.Content})");

            // 核心发送逻辑
            发送脚本到3dsMax(脚本路径);

            记录日志($"脚本已发送：{脚本文件名}（等待 3ds Max 执行完成）");
        }
        catch (Exception ex)
        {
            记录日志($"发送脚本失败：{ex.Message}");
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
        public uint 文件列表偏移;     // 文件列表相对于结构开头的偏移
        public POINT 拖放点;           // 拖放点（屏幕坐标）
        public int 是否非客户区;       // 是否非客户区（通常为0）
        public int 是否宽字符;         // 是否使用宽字符路径（我们用 Unicode）
    }

    private const uint WM_DROPFILES = 0x0233;

    #endregion

    /// <summary>
    /// 核心方法：通过模拟“拖放 .ms 文件到 3ds Max 窗口”来执行 MaxScript
    /// 这是目前最可靠的外部调用 MaxScript 的方式之一（无需开启 MaxScript Listener）
    /// </summary>
    private void 发送脚本到3dsMax(string 脚本路径)
    {
        if (!File.Exists(脚本路径))
        {
            记录日志("脚本文件不存在（异常情况）");
            return;
        }

        // 尝试多种可能的窗口类名/标题来定位 3ds Max 主窗口
        IntPtr 窗口句柄 = FindWindow("3DSMAX", null);
        if (窗口句柄 == IntPtr.Zero) 窗口句柄 = FindWindow("3dsmax", null);
        if (窗口句柄 == IntPtr.Zero) 窗口句柄 = FindWindow(null, "3ds Max");
        if (窗口句柄 == IntPtr.Zero) 窗口句柄 = FindWindow(null, "Autodesk 3ds Max");
        if (窗口句柄 == IntPtr.Zero) 窗口句柄 = FindWindow(null, "Autodesk 3ds Max 2025"); // 可根据版本增加更多

        if (窗口句柄 == IntPtr.Zero)
        {
            记录日志("未找到 3ds Max 主窗口，请确认 3ds Max 已正常打开");
            return;
        }

        记录日志("已定位到 3ds Max 窗口，正在尝试发送脚本...");

        try
        {
            // 把 3ds Max 窗口置前（非常重要，否则拖放消息可能失效）
            SetForegroundWindow(窗口句柄);

            // 获取窗口矩形区域，用于计算中心点坐标
            if (!GetWindowRect(窗口句柄, out RECT 矩形区域))
            {
                记录日志("无法获取 3ds Max 窗口矩形区域");
                return;
            }

            int 中心X = 矩形区域.Left + (矩形区域.Right - 矩形区域.Left) / 2;
            int 中心Y = 矩形区域.Top + (矩形区域.Bottom - 矩形区域.Top) / 2;

            // 构造拖放路径（必须以双空字符结尾，Unicode 编码）
            string 路径带双空 = 脚本路径 + "\0\0";
            byte[] 路径字节 = System.Text.Encoding.Unicode.GetBytes(路径带双空);

            // 计算全局内存块大小
            int 结构大小 = Marshal.SizeOf<DROPFILES>();
            int 总内存大小 = 结构大小 + 路径字节.Length;

            // 分配全局内存（PostMessage 需要 HGLOBAL）
            IntPtr 全局内存句柄 = Marshal.AllocHGlobal(总内存大小);
            if (全局内存句柄 == IntPtr.Zero)
            {
                记录日志("全局内存分配失败");
                return;
            }

            try
            {
                var 拖放结构 = new DROPFILES
                {
                    文件列表偏移 = (uint)结构大小,           // 文件列表从结构后开始
                    拖放点 = new POINT { X = 中心X, Y = 中心Y },
                    是否非客户区 = 0,                         // 客户区拖放
                    是否宽字符 = 1                            // 使用宽字符（Unicode）
                };

                // 把 DROPFILES 结构写入内存
                Marshal.StructureToPtr(拖放结构, 全局内存句柄, false);

                // 把路径字符串（含双\0）紧跟在结构后面
                Marshal.Copy(路径字节, 0, 全局内存句柄 + 结构大小, 路径字节.Length);

                // 发送 WM_DROPFILES 消息给 3ds Max
                bool 发送成功 = PostMessage(窗口句柄, WM_DROPFILES, 全局内存句柄, IntPtr.Zero);

                记录日志(发送成功
                    ? "已成功发送拖放消息 → 3ds Max 应开始执行脚本"
                    : "发送消息失败（可能 3ds Max 未响应、被最小化或焦点问题）");
            }
            catch (Exception ex)
            {
                记录日志($"发送拖放消息时发生异常：{ex.Message}");
            }
            finally
            {
                // 必须释放全局内存，否则会造成内存泄漏
                Marshal.FreeHGlobal(全局内存句柄);
            }
        }
        catch (Exception ex)
        {
            记录日志($"与 3ds Max 交互时发生异常：{ex.Message}");
        }
    }
}