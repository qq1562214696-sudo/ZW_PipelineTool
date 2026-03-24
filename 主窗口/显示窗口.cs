using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Runtime.InteropServices;

namespace ZW_PipelineTool.工具;        // 中文命名空间

/// <summary>
/// 全局 F5 快捷键插件（已完全解耦）
/// </summary>
public class 快捷键 : UserControl, I工具
{
    public string 工具命名 { get; } = "快捷键设置" ;    // 显示名称
    public string 作者 { get; } = "周维" ;    // 显示名称
    public string 版本 { get; } = "v1.0" ;    // 显示名称

    public UserControl? 获取工具面板()
    {
        return this;//后续改进优化
    }

    public void 初始化()
    {
        
    }

    // 常量定义
    private const int 热键ID_F5 = 9001;
    private const uint 热键消息 = 0x0312;        // WM_HOTKEY
    private const uint 无修饰键 = 0x0000;
    private const uint F5虚拟键 = 0x74;

    private IntPtr? _窗口句柄;
    private Win32Properties.CustomWndProcHookCallback? _wndProc回调;

    /// <summary>
    /// 当全局 F5 被按下时触发（主窗口决定要做什么）
    /// </summary>
    public Action? 热键触发事件 { get; set; }

    /// <summary>
    /// 日志输出回调（用于把日志发送回主窗口）
    /// </summary>
    public Action<string>? 日志回调 { get; set; }

    /// <summary>
    /// 注册全局 F5 热键
    /// </summary>
    /// <param name="主窗口">主窗口实例</param>
    public void 注册全局热键(Window 主窗口)
    {
        if (!OperatingSystem.IsWindows())
        {
            输出日志("全局热键仅支持 Windows 系统");
            return;
        }

        _窗口句柄 = 主窗口.TryGetPlatformHandle()?.Handle;
        if (!_窗口句柄.HasValue)
        {
            输出日志("无法获取窗口句柄");
            return;
        }

        bool 注册成功 = RegisterHotKey(_窗口句柄.Value, 热键ID_F5, 无修饰键, F5虚拟键);

        if (注册成功)
        {
            _wndProc回调 = WndProc钩子;
            Win32Properties.AddWndProcHookCallback(主窗口, _wndProc回调);
            输出日志("全局 F5 热键注册成功");
        }
        else
        {
            输出日志("注册全局 F5 热键失败，可能已被其他程序占用");
        }
    }

    /// <summary>
    /// 注销全局 F5 热键
    /// </summary>
    /// <param name="主窗口">主窗口实例</param>
    public void 注销全局热键(Window 主窗口)
    {
        if (_窗口句柄.HasValue && _wndProc回调 != null)
        {
            UnregisterHotKey(_窗口句柄.Value, 热键ID_F5);
            Win32Properties.RemoveWndProcHookCallback(主窗口, _wndProc回调);
            输出日志("全局 F5 热键已注销");
        }
    }

    private IntPtr WndProc钩子(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 热键消息 && (int)wParam == 热键ID_F5)
        {
            Dispatcher.UIThread.Post(() => 热键触发事件?.Invoke());
            handled = true;
            return IntPtr.Zero;
        }
        return IntPtr.Zero;
    }

    private void 输出日志(string 消息)
    {
        日志回调?.Invoke($"[全局快捷键] {消息}");
    }

    // ==================== P/Invoke ====================
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}