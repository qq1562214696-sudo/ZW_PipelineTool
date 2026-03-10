using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.ComponentModel;
using Microsoft.Win32;

namespace ZW_PipelineTool;

public partial class 主窗口 : INotifyPropertyChanged
{
    // 自启动常量
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppStartupName = "ZW_PipelineTool";

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

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // 自启动注册表操作
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
        return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
    }
}