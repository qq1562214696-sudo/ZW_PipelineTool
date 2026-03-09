using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Win32; // 新增：用于 SemaphoreSlim

namespace ZW_PipelineTool;

public partial class 主窗口 : Window
{
    // 新增常量（建议放在类开头附近）
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppStartupName = "ZW_PipelineTool";  // 可改成你的英文项目名，避免中文问题

    private bool IsStartupEnabled()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
                if (key == null)
                {
                    记录日志("[自启] 注册表键不存在");
                    return false;
                }
                var valueObj = key.GetValue(AppStartupName);
                if (valueObj == null)
                {
                    记录日志("[自启] 注册表值不存在");
                    return false;
                }
                string registeredPath = valueObj.ToString()?.Trim('"').Trim() ?? "";
                string currentExePath = GetExePath().Trim('"').Trim();
                记录日志($"[自启] 注册表路径: {registeredPath}");
                记录日志($"[自启] 当前路径: {currentExePath}");
                bool match = string.Equals(registeredPath, currentExePath, StringComparison.OrdinalIgnoreCase);
                记录日志($"[自启] 是否匹配: {match}");
                return match;
            }
            catch (Exception ex)
            {
                记录日志($"[自启] 读取失败: {ex.Message}");
                return false;
            }
        }
        return false;
    }

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
                    记录日志("已启用开机自启（注册表）");
                }
                else
                {
                    key.DeleteValue(AppStartupName, false);
                    记录日志("已取消开机自启");
                }
            }
            catch (Exception ex)
            {
                记录日志($"设置开机自启失败：{ex.Message}");
            }
        }
        else
        {
            记录日志("开机自启功能仅支持 Windows 平台");
        }
    }

    private string GetExePath()
    {
        // 获取当前可执行文件的完整路径
        return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
    }

    private void 开机自启_Checked(object? sender, RoutedEventArgs e)
    {
                    记录日志("asf");
        SetStartupEnabled(true);
    }

    private void 开机自启_Unchecked(object? sender, RoutedEventArgs e)
    {
                            记录日志("asf");
        SetStartupEnabled(false);
    }
}