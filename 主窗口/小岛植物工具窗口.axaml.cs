using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ZW_PipelineTool
{
    public partial class 小岛植物工具窗口 : Window
    {
        public 小岛植物工具窗口()
        {
            InitializeComponent();
        }

        // 这些按钮事件目前都是“待实现”，你可以在这里放逻辑
        // 或者统一抽到共享类（如 MaxScriptExecutor）里调用，避免重复
        private void 脚本按钮_点击(object? sender, RoutedEventArgs e)
        {
            // 示例：根据按钮名字执行不同 Max 脚本
            if (sender is Button btn)
            {
                string buttonName = btn.Name ?? "";
                // 调用你的 Max 脚本执行方法
                // e.g. ExecuteMaxScriptForIsland(buttonName);
            }
        }
    }
}