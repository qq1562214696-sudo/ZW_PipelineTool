using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;

namespace ZW_PipelineTool;

public class FolderProcessor
{
    public event Action<string, string>? LogMessage;   // message, color
    public event Func<string, string, MessageBoxButtons, Task<bool>>? ShowMessage;

    private void Log(string msg, string color = "Black") => LogMessage?.Invoke(msg, color);
    private async Task<bool> Confirm(string msg, string title = "确认") => 
        ShowMessage != null && await ShowMessage(msg, title, MessageBoxButtons.OKCancel);

    public async Task Process(string targetFolder)
    {
        try
        {
            // 校验参数
            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                Log("传入的目标路径为空", "Red");
                return;
            }
            
            // 校验路径
            if (!Directory.Exists(targetFolder))
            {
                Log($"文件夹不存在: {targetFolder}", "Red");
                return;
            }

            // 检查总文件夹结构，必要时切换到“提交文件夹”
            string originalFolder = targetFolder;
            bool hasAssets = Directory.Exists(Path.Combine(targetFolder, "Assets"));
            bool hasScreenshot = Directory.Exists(Path.Combine(targetFolder, "截图"));

            if (!(hasAssets && hasScreenshot))
            {
                var submitFolder = Directory.GetDirectories(targetFolder, "提交文件夹").FirstOrDefault();
                if (submitFolder != null)
                {
                    targetFolder = submitFolder;
                    Log($"检测到提交文件夹，自动切换到: {targetFolder}", "Cyan");
                    hasAssets = Directory.Exists(Path.Combine(targetFolder, "Assets"));
                    hasScreenshot = Directory.Exists(Path.Combine(targetFolder, "截图"));
                }
            }

            if (!(hasAssets && hasScreenshot))
            {
                Log("选择的文件夹不符合总文件夹结构！应包含 Assets 和 截图 文件夹", "Red");
                return;
            }

            // 第一步：查找同级目录下的 .psd 文件并提取命名
            string? parentFolder = Path.GetDirectoryName(targetFolder);
            if (string.IsNullOrEmpty(parentFolder))
            {
                Log("无法确定父目录，停止处理", "Red");
                return;
            }
            var psdFiles = Directory.GetFiles(parentFolder, "*.psd", SearchOption.AllDirectories);
            var validNames = new List<string>();
            var regex = new Regex(@"^([A-Za-z]+)_(\d+)");
            foreach (var psd in psdFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(psd);
                var match = regex.Match(fileName);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    string number = match.Groups[2].Value;
                    validNames.Add($"{prefix}_{number}");
                }
            }
            validNames = validNames.Distinct().OrderBy(x => x).ToList();

            if (validNames.Count == 0)
            {
                Log("在同级目录下未找到符合格式的 .psd 文件！格式应为：前缀_数字（如 Hand_00001）", "Orange");
                return;
            }

            // 显示确认面板
            string confirmMsg = $"找到 {validNames.Count} 个道具：\n\n{string.Join("\n", validNames)}\n\n点确定执行所有操作，点取消退出。";
            if (!await Confirm(confirmMsg, "确认操作"))
                return;

            // 第二步：复制命名到剪贴板
            string textToCopy = string.Join("\n", validNames);
            await CopyToClipboard(textToCopy);
            Log("命名已复制到剪贴板", "Green");

            // 第三步：提取贴图到总文件夹
            Log("正在提取公共贴图到总文件夹...", "Blue");
            string[] texturePatterns = new[] { "*_A.png", "*_D.png", "*_D.psd" };

            // 检查是否已存在公共贴图
            bool hasCommonTextures = texturePatterns.All(pattern => 
                Directory.GetFiles(targetFolder, pattern).Length > 0);

            if (!hasCommonTextures)
            {
                var templateFolders = Directory.GetDirectories(targetFolder)
                    .Where(d => !Path.GetFileName(d).Equals("Assets", StringComparison.OrdinalIgnoreCase) &&
                                !Path.GetFileName(d).Equals("截图", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (templateFolders.Length > 0)
                {
                    string firstTemplate = templateFolders[0];
                    Log($"从模板文件夹提取公共贴图: {Path.GetFileName(firstTemplate)}", "Cyan");
                    foreach (string pattern in texturePatterns)
                    {
                        var textureFiles = Directory.GetFiles(firstTemplate, pattern);
                        foreach (string tex in textureFiles)
                        {
                            string dest = Path.Combine(targetFolder, Path.GetFileName(tex));
                            if (!File.Exists(dest))
                            {
                                File.Copy(tex, dest);
                                Log($"  提取: {Path.GetFileName(tex)}", "Green");
                            }
                        }
                    }
                }
                else
                {
                    Log("未找到模板文件夹，无法提取公共贴图", "Yellow");
                }
            }
            else
            {
                Log("公共贴图已存在于总文件夹", "Cyan");
            }

            // 第四步：标记源文件夹并删除模板文件夹中的贴图
            Log("正在标记源文件并清理重复贴图...", "Blue");
            var markedItems = new List<(string OriginalPath, string MarkedPath, string MarkedName)>();

            var allFolders = Directory.GetDirectories(targetFolder)
                .Select(d => new DirectoryInfo(d))
                .Where(di => di.Name.Contains("_") && 
                             !di.Name.Equals("Assets", StringComparison.OrdinalIgnoreCase) && 
                             !di.Name.Equals("截图", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var folder in allFolders)
            {
                string folderName = folder.Name;
                var match = Regex.Match(folderName, @"^(.+?)_(.+)$");
                if (match.Success)
                {
                    string suffix = match.Groups[2].Value;
                    if (!Regex.IsMatch(suffix, @"^\d+$")) // 不是纯数字
                    {
                        if (!folderName.EndsWith("_删"))
                        {
                            string newName = folderName + "_删";
                            string newPath = Path.Combine(targetFolder, newName);

                            // 如果已存在同名标记文件夹，先删除
                            if (Directory.Exists(newPath))
                                Directory.Delete(newPath, true);

                            // 删除模板文件夹中的贴图文件
                            foreach (string pattern in texturePatterns)
                            {
                                var texFiles = Directory.GetFiles(folder.FullName, pattern);
                                foreach (string tex in texFiles)
                                {
                                    File.Delete(tex);
                                    Log($"  删除模板贴图: {folderName}\\{Path.GetFileName(tex)}", "DarkGray");
                                }
                            }

                            // 重命名文件夹
                            Directory.Move(folder.FullName, newPath);
                            markedItems.Add((folder.FullName, newPath, newName));
                            Log($"已标记: {folderName} -> {newName}", "Yellow");
                        }
                    }
                }
            }

            // 第五步：使用标记后的模板文件夹创建新文件夹
            Log("正在创建新文件夹...", "Blue");
            var markedTemplates = Directory.GetDirectories(targetFolder)
                .Where(d => d.EndsWith("_删"))
                .Select(d => new DirectoryInfo(d))
                .ToList();

            // 获取公共贴图文件
            var commonTextures = texturePatterns
                .SelectMany(pattern => Directory.GetFiles(targetFolder, pattern))
                .Select(f => new FileInfo(f))
                .ToList();
            Log($"找到 {commonTextures.Count} 个公共贴图文件", "Cyan");

            int successCount = 0, errorCount = 0;
            foreach (string fileName in validNames)
            {
                var match = Regex.Match(fileName, @"^(.+?)_(.+)$");
                if (!match.Success)
                {
                    Log($"命名格式错误: {fileName}", "Red");
                    errorCount++;
                    continue;
                }
                string prefix = match.Groups[1].Value;

                // 查找模板
                DirectoryInfo? boyTemplate = null, girlTemplate = null, genericTemplate = null;
                foreach (var template in markedTemplates)
                {
                    string baseName = template.Name.Replace("_删", "");
                    if (!baseName.StartsWith(prefix + "_")) continue;

                    if (baseName.EndsWith("_boy"))
                        boyTemplate = template;
                    else if (baseName.EndsWith("_girl"))
                        girlTemplate = template;
                    else if (!baseName.Contains("_boy") && !baseName.Contains("_girl"))
                        genericTemplate = template;
                }

                var templatesToProcess = new List<(DirectoryInfo Template, string Suffix)>();

                if (boyTemplate != null && girlTemplate != null)
                {
                    templatesToProcess.Add((boyTemplate, "_boy"));
                    templatesToProcess.Add((girlTemplate, "_girl"));
                }
                else if (genericTemplate != null)
                {
                    templatesToProcess.Add((genericTemplate, ""));
                }
                else if (boyTemplate != null)
                {
                    templatesToProcess.Add((boyTemplate, "_boy"));
                }
                else if (girlTemplate != null)
                {
                    templatesToProcess.Add((girlTemplate, "_girl"));
                }
                else
                {
                    Log($"未找到模板: {fileName}", "Red");
                    errorCount++;
                    continue;
                }

                foreach (var (template, suffix) in templatesToProcess)
                {
                    string newFolderName = fileName + suffix;
                    string targetPath = Path.Combine(targetFolder, newFolderName);

                    try
                    {
                        // 自动覆盖已存在的文件夹
                        if (Directory.Exists(targetPath))
                            Directory.Delete(targetPath, true);

                        Directory.CreateDirectory(targetPath);
                        Log($"创建文件夹: {newFolderName}", "Green");

                        // 复制 .max 文件
                        var maxFile = Directory.GetFiles(template.FullName, "*.max").FirstOrDefault();
                        if (maxFile != null)
                        {
                            string newMaxName = fileName + ".max";
                            File.Copy(maxFile, Path.Combine(targetPath, newMaxName), true);
                            Log($"  复制 .max 文件: {newMaxName}", "Cyan");
                        }
                        else
                        {
                            Log($"  警告: 未找到 .max 文件", "Yellow");
                        }

                        // 复制公共贴图并重命名
                        foreach (var tex in commonTextures)
                        {
                            string texName = tex.Name; // 例如 Hand_A.png
                            string baseTex = Path.GetFileNameWithoutExtension(texName); // Hand_A
                            string extension = tex.Extension; // .png
                            var texMatch = Regex.Match(baseTex, @"_(A|D)$");
                            if (texMatch.Success)
                            {
                                string texSuffix = texMatch.Value; // _A 或 _D
                                string newTexName = fileName + texSuffix + extension;
                                File.Copy(tex.FullName, Path.Combine(targetPath, newTexName), true);
                                Log($"  复制贴图: {newTexName}", "Cyan");
                            }
                            else
                            {
                                Log($"  跳过非标准贴图: {tex.Name}", "Yellow");
                            }
                        }
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Log($"创建失败: {newFolderName} - {ex.Message}", "Red");
                        errorCount++;
                    }
                }
            }
            Log($"创建完成，成功: {successCount}, 失败: {errorCount}", successCount > 0 ? "Green" : "Red");

            // 第六步：删除所有标记的项目
            Log("正在删除原模板文件夹...", "Blue");
            foreach (var item in markedItems)
            {
                try
                {
                    if (Directory.Exists(item.MarkedPath))
                        Directory.Delete(item.MarkedPath, true);
                    Log($"已删除: {item.MarkedName}", "DarkGray");
                }
                catch { /* 忽略错误 */ }
            }

            // 第七步：自动删除总文件夹中的原贴图
            Log("正在删除总文件夹中的原贴图...", "Blue");
            foreach (var tex in commonTextures)
            {
                try
                {
                    File.Delete(tex.FullName);
                    Log($"已删除原贴图: {tex.Name}", "DarkGray");
                }
                catch { }
            }

            Log("所有操作完成！", "Green");
        }
        catch (Exception ex)
        {
            // 输出完整异常字符串以便调试
            Log($"发生未处理异常:\n{ex}", "Red");
        }
    }

    private async Task CopyToClipboard(string text)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(text);
        }
    }
}