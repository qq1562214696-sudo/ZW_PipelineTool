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
using System.Threading.Tasks;
using System.Text;
using System.Threading; // 新增：用于 SemaphoreSlim

namespace ZW_PipelineTool;

public partial class 主窗口 : Window
{
    private const string 设置文件名 = "主窗口数据.json";
    private static readonly string 存储路径;
    private 窗口数据 _窗口数据 = new 窗口数据();
    private Expander? 工具基本设置Expander;


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
            _窗口数据.开机自启 = 开机自启CheckBox?.IsChecked ?? false; // 保存复选框状态
            if (工具基本设置Expander != null)
                _窗口数据.工具基本设置展开 = 工具基本设置Expander.IsExpanded;
            if (运行日志Expander != null)
                _窗口数据.运行日志展开 = 运行日志Expander.IsExpanded;

            string json = JsonSerializer.Serialize(_窗口数据, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(存储路径, json);
        }
        catch (Exception ex)
        {
            日志($"窗口保存布局失败：{ex.Message}");
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
            }
        }
        catch (Exception ex)
        {
            日志($"加载窗口布局失败：{ex.Message}");
            _窗口数据 = new 窗口数据();
        }
    }
}

public class 窗口数据
{
    public double 宽度 { get; set; } = 500;
    public double 高度 { get; set; } = 750;
    public double X坐标 { get; set; }
    public double Y坐标 { get; set; }
    public WindowState 窗口状态 { get; set; } = WindowState.Normal;
    public bool 置顶 { get; set; } = true;
    public bool 开机自启 { get; set; } = false; // 默认不开启
    public bool 工具基本设置展开 { get; set; } = true;
    public bool 运行日志展开 { get; set; } = false;
}