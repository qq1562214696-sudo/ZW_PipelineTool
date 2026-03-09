using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ZW_PipelineTool;

public partial class 主窗口
{
    /// <summary>
    /// “浏览”按钮点击 → 打开文件夹选择对话框 → 执行规范整理
    /// </summary>
    private async void 浏览文件夹按钮_点击(object? sender, RoutedEventArgs e)
    {
        var options = new FolderPickerOpenOptions
        {
            Title = "请选择要规范整理的总文件夹",
            AllowMultiple = false
        };

        var folders = await StorageProvider.OpenFolderPickerAsync(options);
        if (folders.Count == 0) return;

        var 选择结果 = folders[0].TryGetLocalPath();
        if (string.IsNullOrEmpty(选择结果)) return;

        var 路径文本框 = this.FindControl<TextBox>("FolderPathTextBox");
        if (路径文本框 != null)
            路径文本框.Text = 选择结果;

        日志($"用户手动选择文件夹：{选择结果}");

        await 规范整理文件夹(选择结果);
    }

    /// <summary>
    /// 核心业务逻辑：规范 PSD 命名 → 提取公共贴图 → 清理旧模板 → 按命名批量创建规范文件夹
    /// </summary>
    private async Task 规范整理文件夹(string 目标文件夹)
    {
        日志("开始执行文件夹规范整理流程...");

        try
        {
            if (!Directory.Exists(目标文件夹))
            {
                日志($"无效路径：{目标文件夹}");
                return;
            }

            // 检查典型结构是否存在
            bool 有Assets文件夹 = Directory.Exists(Path.Combine(目标文件夹, "Assets"));
            bool 有截图文件夹 = Directory.Exists(Path.Combine(目标文件夹, "截图"));

            // 如果当前目录结构不对，尝试自动进入“提交文件夹”
            if (!有Assets文件夹 || !有截图文件夹)
            {
                var 提交文件夹数组 = Directory.GetDirectories(目标文件夹, "提交文件夹", SearchOption.TopDirectoryOnly);
                if (提交文件夹数组.Length > 0)
                {
                    目标文件夹 = 提交文件夹数组[0];
                    日志($"自动切换到提交文件夹：{目标文件夹}");
                    有Assets文件夹 = Directory.Exists(Path.Combine(目标文件夹, "Assets"));
                    有截图文件夹 = Directory.Exists(Path.Combine(目标文件夹, "截图"));
                }
            }

            if (!有Assets文件夹 || !有截图文件夹)
            {
                日志("文件夹结构不符合规范要求：缺少 Assets 或 截图 文件夹");
                return;
            }

            // 获取父目录（通常存放 PSD 文件的位置）
            string? 父目录 = Directory.GetParent(目标文件夹)?.FullName;
            if (string.IsNullOrEmpty(父目录))
            {
                日志("无法获取父目录");
                return;
            }

            // 查找所有 PSD 文件，并提取符合“前缀_数字”格式的命名
            var psd文件列表 = Directory.GetFiles(父目录, "*.psd", SearchOption.AllDirectories);
            日志($"在父目录中找到 {psd文件列表.Length} 个 PSD 文件");

            var 有效命名列表 = new System.Collections.Generic.List<string>();
            foreach (var psd文件 in psd文件列表)
            {
                string 文件基名 = Path.GetFileNameWithoutExtension(psd文件);
                var 匹配结果 = Regex.Match(文件基名, @"^([A-Za-z]+)_(\d+)");
                if (匹配结果.Success)
                {
                    string 提取命名 = 匹配结果.Value;
                    有效命名列表.Add(提取命名);
                    日志($"  提取有效命名：{提取命名} （来自 {文件基名}）");
                }
                else
                {
                    日志($"  跳过不符合格式：{文件基名}");
                }
            }

            有效命名列表 = 有效命名列表.Distinct().OrderBy(n => n).ToList();

            if (有效命名列表.Count == 0)
            {
                日志("未找到任何符合 前缀_数字.psd 格式的文件");
                return;
            }

            日志($"共提取到 {有效命名列表.Count} 个有效命名：");
            foreach (var 命名 in 有效命名列表) 日志("  " + 命名);

            // 将提取的命名列表复制到剪贴板（方便后续粘贴到 Unity 等工具）
            try
            {
                string 要复制的文本 = string.Join("\r\n", 有效命名列表);
                // 使用 cmd echo | clip 的方式（Windows 下最稳定可靠）
                string 安全文本 = 要复制的文本.Replace("%", "%%").Replace("\"", "\"\"");
                var 进程启动信息 = new System.Diagnostics.ProcessStartInfo("cmd", "/c echo " + 安全文本 + " | clip")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(进程启动信息);
                日志("已成功将所有有效命名复制到剪贴板");
            }
            catch (Exception ex)
            {
                日志($"复制到剪贴板失败：{ex.Message}");
            }

            // ── 公共贴图提取处理 ───────────────────────────────────────
            日志("开始提取公共贴图到总文件夹...");

            string[] 贴图模式 = new[] { "*_A.png", "*_D.png", "*_D.psd" };

            bool 已有公共贴图 = true;
            var 公共贴图列表 = new System.Collections.Generic.List<string>();

            // 先检查总文件夹是否已经齐全公共贴图
            foreach (var 模式 in 贴图模式)
            {
                var 匹配文件 = Directory.GetFiles(目标文件夹, 模式, SearchOption.TopDirectoryOnly);
                if (匹配文件.Length == 0)
                {
                    已有公共贴图 = false;
                    break;
                }
            }

            // 如果缺少，则从第一个模板文件夹中提取
            if (!已有公共贴图)
            {
                var 模板文件夹列表 = Directory.GetDirectories(目标文件夹)
                    .Where(d => !string.Equals(Path.GetFileName(d), "Assets", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(Path.GetFileName(d), "截图", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (模板文件夹列表.Length > 0)
                {
                    var 第一个模板 = 模板文件夹列表[0];
                    日志($"从模板文件夹提取公共贴图：{Path.GetFileName(第一个模板)}");

                    foreach (var 模式 in 贴图模式)
                    {
                        var 贴图文件列表 = Directory.GetFiles(第一个模板, 模式, SearchOption.TopDirectoryOnly);
                        foreach (var 贴图文件 in 贴图文件列表)
                        {
                            string 目标路径 = Path.Combine(目标文件夹, Path.GetFileName(贴图文件));
                            if (!File.Exists(目标路径))
                            {
                                File.Copy(贴图文件, 目标路径, true);
                                日志($"  已提取：{Path.GetFileName(贴图文件)}");
                            }
                        }
                    }
                }
                else
                {
                    日志("未找到任何模板文件夹，无法提取公共贴图");
                }
            }
            else
            {
                日志("公共贴图已完整存在于总文件夹，无需提取");
            }

            // 收集最终公共贴图路径
            foreach (var 模式 in 贴图模式)
            {
                公共贴图列表.AddRange(Directory.GetFiles(目标文件夹, 模式, SearchOption.TopDirectoryOnly));
            }

            // ── 标记并清理旧模板 ────────────────────────────────────────
            日志("开始标记旧模板并清理重复贴图...");

            var 已标记项目 = new System.Collections.Generic.List<(string 原路径, string 新路径)>();

            var 所有子文件夹 = Directory.GetDirectories(目标文件夹)
                .Where(d => Path.GetFileName(d).Contains("_")
                            && !string.Equals(Path.GetFileName(d), "Assets", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(Path.GetFileName(d), "截图", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var 子文件夹 in 所有子文件夹)
            {
                string 文件夹名 = Path.GetFileName(子文件夹);
                var 匹配 = Regex.Match(文件夹名, "^(.+?)_(.+)$");
                if (匹配.Success)
                {
                    string 后缀 = 匹配.Groups[2].Value;
                    // 如果后缀不是纯数字，则视为旧模板，需要标记删除
                    if (!Regex.IsMatch(后缀, "^\\d+$"))
                    {
                        if (!文件夹名.EndsWith("_删"))
                        {
                            string 新文件夹名 = 文件夹名 + "_删";
                            string 新路径 = Path.Combine(目标文件夹, 新文件夹名);
                            try
                            {
                                if (Directory.Exists(新路径))
                                    Directory.Delete(新路径, true);

                                // 删除模板内的公共贴图（防止重复）
                                foreach (var 模式 in 贴图模式)
                                {
                                    var 模板贴图 = Directory.GetFiles(子文件夹, 模式, SearchOption.TopDirectoryOnly);
                                    foreach (var 贴图 in 模板贴图)
                                    {
                                        File.Delete(贴图);
                                        日志($"  删除模板内重复贴图：{文件夹名}/{Path.GetFileName(贴图)}");
                                    }
                                }

                                Directory.Move(子文件夹, 新路径);
                                已标记项目.Add((子文件夹, 新路径));
                                日志($"已标记为删除：{文件夹名} → {新文件夹名}");
                            }
                            catch (Exception ex)
                            {
                                日志($"标记失败：{文件夹名} - {ex.Message}");
                            }
                        }
                    }
                }
            }

            // ── 根据 PSD 命名批量创建规范文件夹 ──────────────────────────
            日志("开始按命名批量创建规范文件夹...");

            var 已标记模板 = Directory.GetDirectories(目标文件夹)
                .Where(d => Path.GetFileName(d).EndsWith("_删", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            int 成功数量 = 0, 失败数量 = 0;

            foreach (var 文件命名 in 有效命名列表)
            {
                var 命名匹配 = Regex.Match(文件命名, "^(.+?)_(.+)$");
                if (!命名匹配.Success)
                {
                    日志($"命名格式异常：{文件命名}");
                    失败数量++;
                    continue;
                }

                string 前缀 = 命名匹配.Groups[1].Value;

                string? boy模板 = null, girl模板 = null, 通用模板 = null;

                // 匹配模板（优先级：boy > girl > 通用）
                foreach (var 模板 in 已标记模板)
                {
                    string 基名 = Path.GetFileName(模板).Replace("_删", "");
                    if (基名.StartsWith(前缀 + "_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (基名.EndsWith("_boy", StringComparison.OrdinalIgnoreCase))
                            boy模板 = 模板;
                        else if (基名.EndsWith("_girl", StringComparison.OrdinalIgnoreCase))
                            girl模板 = 模板;
                        else if (!Regex.IsMatch(基名, "_(boy|girl)$", RegexOptions.IgnoreCase))
                            通用模板 = 模板;
                    }
                }

                var 要处理的模板列表 = new System.Collections.Generic.List<(string 模板路径, string 后缀)>();

                if (boy模板 != null && girl模板 != null)
                {
                    要处理的模板列表.Add((boy模板, "_boy"));
                    要处理的模板列表.Add((girl模板, "_girl"));
                }
                else if (通用模板 != null)
                {
                    要处理的模板列表.Add((通用模板, ""));
                }
                else if (boy模板 != null || girl模板 != null)
                {
                    if (boy模板 != null) 要处理的模板列表.Add((boy模板, "_boy"));
                    else 要处理的模板列表.Add((girl模板!, "_girl"));
                }
                else
                {
                    日志($"未找到匹配模板：{文件命名}");
                    失败数量++;
                    continue;
                }

                foreach (var tpl in 要处理的模板列表)
                {
                    string 新文件夹名 = 文件命名 + tpl.后缀;
                    string 来源路径 = tpl.模板路径;
                    string 目标路径 = Path.Combine(目标文件夹, 新文件夹名);

                    try
                    {
                        // 先删除可能存在的同名旧文件夹
                        if (Directory.Exists(目标路径))
                            Directory.Delete(目标路径, true);

                        Directory.CreateDirectory(目标路径);
                        日志($"成功创建文件夹：{新文件夹名}");

                        // 复制 .max 文件并重命名
                        var max文件 = Directory.GetFiles(来源路径, "*.max", SearchOption.TopDirectoryOnly).FirstOrDefault();
                        if (max文件 != null)
                        {
                            string 新Max文件名 = 文件命名 + ".max";
                            File.Copy(max文件, Path.Combine(目标路径, 新Max文件名), true);
                            日志($"  已复制 .max 文件：{新Max文件名}");
                        }
                        else
                        {
                            日志("  警告：未在模板中找到 .max 文件");
                        }

                        // 复制公共贴图，并按规范重命名
                        foreach (var 贴图文件 in 公共贴图列表)
                        {
                            string 贴图基名 = Path.GetFileNameWithoutExtension(贴图文件);
                            string 扩展名 = Path.GetExtension(贴图文件);
                            var m2 = Regex.Match(贴图基名, "_(A|D)$");
                            if (m2.Success)
                            {
                                string 后缀 = m2.Value;
                                string 新贴图名 = 文件命名 + 后缀 + 扩展名;
                                File.Copy(贴图文件, Path.Combine(目标路径, 新贴图名), true);
                                日志($"  已复制贴图：{新贴图名}");
                            }
                            else
                            {
                                日志($"  跳过非标准贴图：{Path.GetFileName(贴图文件)}");
                            }
                        }

                        成功数量++;
                    }
                    catch (Exception ex)
                    {
                        日志($"创建文件夹失败：{新文件夹名} - {ex.Message}");
                        失败数量++;
                    }
                }
            }

            // ── 最终清理阶段 ─────────────────────────────────────────────
            日志("开始清理原模板和公共贴图...");

            foreach (var 项目 in 已标记项目)
            {
                try
                {
                    if (Directory.Exists(项目.新路径))
                        Directory.Delete(项目.新路径, true);
                    日志($"已彻底删除模板：{Path.GetFileName(项目.新路径)}");
                }
                catch { /* 忽略无法删除的情况 */ }
            }

            日志("正在删除总文件夹中的原始公共贴图...");
            foreach (var 贴图 in 公共贴图列表)
            {
                try
                {
                    if (File.Exists(贴图))
                        File.Delete(贴图);
                    日志($"已删除原始公共贴图：{Path.GetFileName(贴图)}");
                }
                catch { }
            }

            日志($"规范整理完成：成功 {成功数量} 个，失败 {失败数量} 个");
            日志("整个文件夹规范整理流程已结束");
        }
        catch (Exception ex)
        {
            日志($"处理过程中发生严重异常：{ex.Message}");
        }
    }
}