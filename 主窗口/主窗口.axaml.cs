using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace ZW_PipelineTool;

// 1.Unity待测试优化

// 3.将max unity main的QF和主要使用部分分离出来
// 4.除了Mesh01其他ms脚本日志待优化
// 5.后续还需要观察是否功能和QF原工具有出入
// 6.UI优化

// 8.面数检查等检查项可以合做一个
// 9.检查面数检查是否为将满足命名条件的对象全部三角面算到一起
// 10.拖入文件功能后续还需优化，区分拖入区块或者窗口
// 11.交互需要进一步优化，现在只有QF模板文件夹，后续很多东西都需要拖入处理
// 12.其他暂时记不起来，想起来再补充
public partial class 主窗口 : Window
{
    private Expander? 工具基本设置Expander;

    public 主窗口()
    {
        InitializeComponent();
        DataContext = this;

        日志列表框 = this.FindControl<ListBox>("LogListBox");
        工具基本设置Expander = this.FindControl<Expander>("工具基本设置");
        运行日志Expander = this.FindControl<Expander>("运行日志");

        // 拖放支持
        DragDrop.SetAllowDrop(this, true);
        this.AddHandler(DragDrop.DragEnterEvent, 窗口_拖入);
        this.AddHandler(DragDrop.DragOverEvent, 窗口_拖拽中);
        this.AddHandler(DragDrop.DropEvent, 窗口_放下);

        Opened += async (s, e) =>
        {
            加载窗口设置();          // 先加载数据
            应用窗口设置();          // 应用窗口大小/位置等

            // 关键：手动同步自启动状态（确保 UI 反映 JSON 值）
            开机自启 = _窗口数据.开机自启;
            OnPropertyChanged(nameof(开机自启));  // 强制通知绑定刷新 CheckBox

            if (工具基本设置Expander != null)
                工具基本设置Expander.IsExpanded = _窗口数据.工具基本设置展开;
            if (运行日志Expander != null)
                运行日志Expander.IsExpanded = _窗口数据.运行日志展开;

            if (_窗口数据.X坐标 <= 0 && _窗口数据.Y坐标 <= 0)
                自动定位到右下角();
            else
                确保窗口在可见区域内();

            // ====== 新增：自动加载上次保存的 Unity 路径到输入框 ======
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MAX", "QF_Config.txt");
                if (File.Exists(configPath))
                {
                    var lines = await File.ReadAllLinesAsync(configPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("AssetsPath=", StringComparison.OrdinalIgnoreCase))
                        {
                            string savedPath = line.Substring("AssetsPath=".Length).Trim();
                            if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                            {
                                var unityBox = this.FindControl<TextBox>("QF_UnityPathInput");
                                if (unityBox != null)
                                {
                                    unityBox.Text = savedPath;
                                    日志($"已自动加载上次保存的 Unity 路径：{savedPath}");
                                }
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                日志($"自动加载 Unity 路径失败：{ex.Message}");
            }
            // ====================================================

            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            _logFilePath = Path.Combine(exeDir, "MAX", "Max_Log.txt");
            StartMaxLogWatcher();
        };

        Closing += (s, e) =>
        {
            保存窗口设置();
            StopMaxLogWatcher();
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // 窗口位置/大小相关方法（保持原样）
    private void 确保窗口在可见区域内()
    {
        var 主屏幕 = Screens.Primary;
        if (主屏幕 == null) return;
        var 工作区 = 主屏幕.WorkingArea;
        int x = Position.X, y = Position.Y;
        int 宽 = (int)Width, 高 = (int)Height;
        if (x + 宽 > 工作区.X + 工作区.Width) x = 工作区.X + 工作区.Width - 宽;
        if (x < 工作区.X) x = 工作区.X;
        if (y + 高 > 工作区.Y + 工作区.Height) y = 工作区.Y + 工作区.Height - 高;
        if (y < 工作区.Y) y = 工作区.Y;
        Position = new PixelPoint(x, y);
    }

    private void 自动定位到右下角()
    {
        var 主屏幕 = Screens.Primary;
        if (主屏幕 == null) return;
        var 缩放 = 主屏幕.Scaling;
        var 窗口像素尺寸 = new PixelSize(
            (int)(ClientSize.Width * 缩放),
            (int)(ClientSize.Height * 缩放));
        var 工作区 = 主屏幕.WorkingArea;
        int 边距 = (int)(15 * 缩放);
        int x = 工作区.X + 工作区.Width - 窗口像素尺寸.Width - 边距;
        int y = 工作区.Y + 工作区.Height - 窗口像素尺寸.Height - 边距;
        x = Math.Max(工作区.X, x);
        y = Math.Max(工作区.Y, y);
        Position = new PixelPoint(x, y);
        WindowStartupLocation = WindowStartupLocation.Manual;
    }
}