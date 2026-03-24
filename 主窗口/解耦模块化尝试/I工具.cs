// ZW_PipelineTool.工具 / I工具.cs
using Avalonia.Controls;

namespace ZW_PipelineTool.工具;

public interface I工具
{
    string 工具命名 { get; }     // 显示名称
    string 作者 { get; }         //作者
    string 版本 { get; }        //工具版本

    // 工具面板
    UserControl? 获取工具面板();

    // 初始化
    void 初始化();
}