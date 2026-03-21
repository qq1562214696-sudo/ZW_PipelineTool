using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ZW_PipelineTool
{
    public partial class QF工具窗口 : Window
    {
        public QF工具窗口()
        {
            InitializeComponent();
        }

        // 下面这些事件处理方法可以：
        // 1. 保持和主窗口里的一样（如果逻辑相同）
        // 2. 或者把逻辑抽成共享类/服务
        // 3. 暂时先留空或复制过来
        private void 初始化按钮_点击(object? sender, RoutedEventArgs e)
        {
            // 原有逻辑（建议抽到单独的服务类，避免重复）
        }

        private void 脚本按钮_点击(object? sender, RoutedEventArgs e)
        {
            // 原有 Max 脚本执行逻辑
        }

        private void 打开头像编辑器按钮_点击(object? sender, RoutedEventArgs e) { /* ... */ }
        private void 复制并导出按钮_点击(object? sender, RoutedEventArgs e) { /* ... */ }
        private void 粘贴Max名称按钮_点击(object? sender, RoutedEventArgs e) { /* ... */ }
        private void 文件标准化按钮_点击(object? sender, RoutedEventArgs e) { /* ... */ }
        private void 批量导出按钮_点击(object? sender, RoutedEventArgs e) { /* ... */ }
        private void 清空Max名称按钮_点击(object? sender, RoutedEventArgs e) { /* ... */ }
    }
}