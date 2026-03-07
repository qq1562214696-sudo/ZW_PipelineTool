using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Linq;

namespace ZW_PipelineTool;

public partial class 主窗口 : Window
{
    private const string 设置文件名 = "主窗口数据.json";
    private static readonly string 存储路径;
    private 窗口数据 _窗口数据 = new 窗口数据();
    protected TextBox? 日志文本框;

    static 主窗口()
    {
        string appData目录 = Path.Combine(AppContext.BaseDirectory, "AppData");
        Directory.CreateDirectory(appData目录);
        存储路径 = Path.Combine(appData目录, 设置文件名);
    }

    public 主窗口()
    {
        InitializeComponent();

        // 查找日志控件
        日志文本框 = this.FindControl<TextBox>("LogBox");

        // ── 绑定所有事件 ──────────────────────────────────────────────────
        if (置顶CheckBox != null)
        {
            置顶CheckBox.Checked += 置顶切换_选中;
            置顶CheckBox.Unchecked += 置顶切换_取消选中;
        }

        if (开机自启CheckBox != null)
        {
            开机自启CheckBox.Checked += 开机自启切换_选中;
            开机自启CheckBox.Unchecked += 开机自启切换_取消选中;
        }

        // QF 项目 - 初始化
        if (QF_初始化按钮 != null) QF_初始化按钮.Click += 初始化按钮_点击;

        // QF 项目 - 3ds Max 工具按钮
        if (规范模型命名Btn != null) 规范模型命名Btn.Click += 脚本按钮_点击;
        if (导出当前选中为FBXBtn != null) 导出当前选中为FBXBtn.Click += 脚本按钮_点击;
        if (导出贴图加FBXBtn != null) 导出贴图加FBXBtn.Click += 脚本按钮_点击;
        if (选择多边面物体Btn != null) 选择多边面物体Btn.Click += 脚本按钮_点击;
        if (选择法线翻转的面Btn != null) 选择法线翻转的面Btn.Click += 脚本按钮_点击;
        if (快速面数检查Btn != null) 快速面数检查Btn.Click += 脚本按钮_点击;
        if (Mesh01批量重命名Btn != null) Mesh01批量重命名Btn.Click += 脚本按钮_点击;

        // Unity 工具按钮
        if (Unity初始化Btn != null) Unity初始化Btn.Click += 初始化按钮_点击;
        if (打开头像编辑器Btn != null) 打开头像编辑器Btn.Click += 打开头像编辑器按钮_点击;
        if (复制并导出Btn != null) 复制并导出Btn.Click += 复制并导出按钮_点击;
        if (粘贴Max名称Btn != null) 粘贴Max名称Btn.Click += 粘贴Max名称按钮_点击;
        if (文件标准化Btn != null) 文件标准化Btn.Click += 文件标准化按钮_点击;
        if (批量导出Btn != null) 批量导出Btn.Click += 批量导出按钮_点击;
        if (清空Max名称Btn != null) 清空Max名称Btn.Click += 清空Max名称按钮_点击;

        // 小岛植物 项目 - 3ds Max 工具
        if (小岛_规范模型命名Btn != null) 小岛_规范模型命名Btn.Click += 脚本按钮_点击;
        if (小岛_导出当前为FBXBtn != null) 小岛_导出当前为FBXBtn.Click += 脚本按钮_点击;
        if (小岛_导出贴图加FBXBtn != null) 小岛_导出贴图加FBXBtn.Click += 脚本按钮_点击;
        if (小岛_选择多边面Btn != null) 小岛_选择多边面Btn.Click += 脚本按钮_点击;
        if (小岛_选择法线翻转Btn != null) 小岛_选择法线翻转Btn.Click += 脚本按钮_点击;
        if (小岛_面数检查Btn != null) 小岛_面数检查Btn.Click += 脚本按钮_点击;
        if (小岛_Mesh01重命名Btn != null) 小岛_Mesh01重命名Btn.Click += 脚本按钮_点击;

        // 拖放支持
        DragDrop.SetAllowDrop(this, true);
        this.AddHandler(DragDrop.DragEnterEvent, 窗口_拖入);
        this.AddHandler(DragDrop.DragOverEvent, 窗口_拖拽中);
        this.AddHandler(DragDrop.DropEvent, 窗口_放下);

        // 窗口生命周期
        Opened += (s, e) =>
        {
            加载窗口设置();
            应用窗口设置();
            if (_窗口数据.X坐标 <= 0 && _窗口数据.Y坐标 <= 0)
            {
                自动定位到右下角();
            }
            else
            {
                确保窗口在可见区域内();
            }
        };

        Closing += (s, e) => 保存窗口设置();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // 置顶事件（示例，已在构造函数绑定）
    private void 置顶切换_选中(object? sender, RoutedEventArgs e) => Topmost = true;
    private void 置顶切换_取消选中(object? sender, RoutedEventArgs e) => Topmost = false;

    private void 开机自启切换_选中(object? sender, RoutedEventArgs e)
    {
        // 待实现
        记录日志("开机自启已勾选（功能待实现）");
    }

    private void 开机自启切换_取消选中(object? sender, RoutedEventArgs e)
    {
        // 待实现
        记录日志("开机自启已取消（功能待实现）");
    }

    // 初始化按钮（QF 和 Unity 共用，根据需要区分）
    private void 初始化按钮_点击(object? sender, RoutedEventArgs e)
    {
        记录日志("初始化按钮被点击（待实现具体逻辑）");
        // 可根据 sender 判断是 QF 还是 Unity 的按钮
    }

    #region 拖放支持（已修复 Data → DataTransfer）

    private void 窗口_拖入(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void 窗口_拖拽中(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

     /// <summary>
    /// 核心拖放处理：只接受单个文件夹，并交给 文件夹规范处理() 处理
    /// </summary>
    protected async void 窗口_放下(object? sender, DragEventArgs e)
    {
        记录日志("=== 拖放事件触发 ===");

        var 文件列表 = e.Data.GetFiles();
        if (文件列表 == null || !文件列表.Any())
        {
            记录日志("未检测到任何文件/文件夹");
            return;
        }

        // 只处理第一个拖入的项目（我们只接受文件夹）
        var 第一个项目 = 文件列表.First();
        var 路径 = 第一个项目.TryGetLocalPath();

        if (string.IsNullOrEmpty(路径))
        {
            记录日志("无法获取本地路径");
            return;
        }

        if (!Directory.Exists(路径))
        {
            记录日志($"拖入的不是文件夹：{路径}");
            return;
        }

        记录日志($"成功接收文件夹：{路径}");

        // 更新界面上的路径显示
        var 路径文本框 = this.FindControl<TextBox>("FolderPathTextBox");
        if (路径文本框 != null)
            路径文本框.Text = 路径;

        // 执行文件夹规范整理逻辑（核心业务）
        await 规范整理文件夹(路径);

        e.Handled = true;
    }

    #endregion

    #region 窗口位置与状态持久化（保持不变）

    private void 保存窗口设置()
    {
        try
        {
            if (WindowState == WindowState.Normal)
            {
                _窗口数据.宽度 = Width;
                _窗口数据.高度 = Height;
                _窗口数据.X坐标 = Position.X;
                _窗口数据.Y坐标 = Position.Y;
            }

            _窗口数据.窗口状态 = WindowState;
            _窗口数据.置顶 = Topmost;

            string json = JsonSerializer.Serialize(_窗口数据, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(存储路径, json);
            记录日志("窗口布局已保存");
        }
        catch (Exception ex)
        {
            记录日志($"保存布局失败：{ex.Message}");
        }
    }

    private void 加载窗口设置()
    {
        try
        {
            if (File.Exists(存储路径))
            {
                string json = File.ReadAllText(存储路径);
                _窗口数据 = JsonSerializer.Deserialize<窗口数据>(json) ?? new 窗口数据();
                记录日志("已加载上次窗口布局");
            }
        }
        catch (Exception ex)
        {
            记录日志($"加载布局失败：{ex.Message}");
            _窗口数据 = new 窗口数据();
        }
    }

    private void 应用窗口设置()
    {
        try
        {
            if (_窗口数据.窗口状态 == WindowState.Normal)
            {
                if (_窗口数据.宽度 > 0) Width = _窗口数据.宽度;
                if (_窗口数据.高度 > 0) Height = _窗口数据.高度;
                if (_窗口数据.X坐标 >= 0 && _窗口数据.Y坐标 >= 0)
                {
                    Position = new PixelPoint((int)_窗口数据.X坐标, (int)_窗口数据.Y坐标);
                }
            }

            WindowState = _窗口数据.窗口状态;
            Topmost = _窗口数据.置顶;
        }
        catch { }
    }

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

    #endregion

    protected virtual void 记录日志(string 消息)
    {
        if (日志文本框 == null) return;

        string 时间 = DateTime.Now.ToString("HH:mm:ss");
        string 一行 = $"[{时间}] {消息}\n";

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                if (日志文本框 != null && 日志文本框.IsVisible)
                {
                    日志文本框.Text += 一行;
                    日志文本框.CaretIndex = 日志文本框.Text.Length;
                }
            }
            catch { }
        });
    }
}

public class 窗口数据
{
    public double 宽度 { get; set; } = 300;
    public double 高度 { get; set; } = 600;
    public double X坐标 { get; set; }
    public double Y坐标 { get; set; }
    public WindowState 窗口状态 { get; set; } = WindowState.Normal;
    public bool 置顶 { get; set; } = true;
    public bool 开机自启 { get; set; } = true;
}