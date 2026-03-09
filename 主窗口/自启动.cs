using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Win32; // 新增：用于 SemaphoreSlim

namespace ZW_PipelineTool;

public partial class 主窗口 : Window//自启动区块
{
    // 新增常量（建议放在类开头附近）
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppStartupName = "ZW_PipelineTool";  // 可改成你的英文项目名，避免中文问题

    private void SetStartupEnabled(bool enabled)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                if (key == null) return;

                if (enabled)
                {
                    string exePath = GetExePath();
                    // 路径含空格时加引号
                    if (exePath.Contains(" ")) exePath = "\"" + exePath + "\"";
                    key.SetValue(AppStartupName, exePath);
                    日志("已启用开机自启（注册表）");
                }
                else
                {
                    key.DeleteValue(AppStartupName, false);
                    日志("已取消开机自启");
                }
            }
            catch (Exception ex)
            {
                日志($"设置开机自启失败：{ex.Message}");
            }
        }
        else
        {
            日志("开机自启功能仅支持 Windows 平台");
        }
    }

    private string GetExePath()
    {
        // 获取当前可执行文件的完整路径
        return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
    }

    private void 开机自启_Checked(object? sender, RoutedEventArgs e)
    {
        SetStartupEnabled(true);
    }

    private void 开机自启_Unchecked(object? sender, RoutedEventArgs e)
    {
        SetStartupEnabled(false);
    }
}