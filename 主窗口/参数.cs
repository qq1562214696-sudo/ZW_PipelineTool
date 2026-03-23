using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;

namespace ZW_PipelineTool
{
    public partial class 主窗口
    {
        // -------------------------自启动-------------------------
        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppStartupName = "ZW_PipelineTool";

        //-------------------------Unity-------------------------
        private const string Unity管道名称 = "ZW_PipelineTool";

        //-------------------------数据储存-------------------------
        private const string 设置文件名 = "主窗口数据.json";
        private static readonly string 存储路径;
        private 窗口数据 _窗口数据 = new 窗口数据();

        //-------------------------日志-------------------------
        private ListBox? 日志列表框;
        private Expander? 运行日志Expander;
        private Expander? 工具基本设置Expander;
        private static readonly SemaphoreSlim _logFileSemaphore = new(1, 1);
        public ObservableCollection<日志数据> 日志列表 { get; } = new();

        // MaxScript 日志监控相关字段
        private FileSystemWatcher? _logWatcher;
        private string _logFilePath = string.Empty;


        // 自启动属性（绑定用）
        private bool _开机自启;
        public bool 开机自启
        {
            get => _开机自启;
            set
            {
                if (_开机自启 != value)
                {
                    _开机自启 = value;
                    SetStartupEnabled(value);
                    OnPropertyChanged(nameof(开机自启));
                }
            }
        }

        // INotifyPropertyChanged 实现
        public new event PropertyChangedEventHandler? PropertyChanged;

        // -------------------------全局热键-------------------------
#if WINDOWS
        private IntPtr? _windowHandle;
        private const int HotKeyId_F5 = 9001;
        private const uint HotKeyMessage = 0x0312;     // WM_HOTKEY
        private const uint ModifierNone = 0x0000;
        private const uint VK_F5 = 0x74;

        // 保存 callback 以便移除
        private Win32Properties.CustomWndProcHookCallback? _wndProcCallback;

       [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
#endif

    
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
    /// <summary>
    /// 用于模拟文件拖放的结构（WM_DROPFILES 消息需要）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct DROPFILES
    {
        public uint 文件列表偏移; // 文件列表相对于结构开头的偏移
        public POINT 拖放点; // 拖放点（屏幕坐标）
        public int 是否非客户区; // 是否非客户区（通常为0）
        public int 是否宽字符; // 是否使用宽字符路径（我们用 Unicode）
    }
    private const uint WM_DROPFILES = 0x0233;

    #endregion
    }
}