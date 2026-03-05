using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace ZW_PipelineTool
{
    public partial class MainWindow
    {
        // ==================== 浏览按钮 ====================
        private async void QF_BrowseFolder_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择要规范整理的总文件夹"
            };

            var result = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result)) return;

            var textBox = this.FindControl<TextBox>("FolderPathTextBox");
            if (textBox != null)
                textBox.Text = result;

            Log($"手动选择文件夹：{result}");
            await QF_ProcessFolderNorm(result);
        }

        // ==================== 核心处理逻辑 ====================
        private async Task QF_ProcessFolderNorm(string targetFolder)
        {
            Log("开始文件夹规范整理...");

            try
            {
                if (!Directory.Exists(targetFolder))
                {
                    Log("无效路径：" + targetFolder);
                    return;
                }

                // 改进：支持直接拖入提交文件夹或总文件夹
                bool hasAssets = Directory.Exists(Path.Combine(targetFolder, "Assets"));
                bool hasScreenshot = Directory.Exists(Path.Combine(targetFolder, "截图"));

                if (!hasAssets || !hasScreenshot)
                {
                    // 查找子目录中的“提交文件夹”
                    var submitDirs = Directory.GetDirectories(targetFolder, "提交文件夹", SearchOption.TopDirectoryOnly);
                    if (submitDirs.Length > 0)
                    {
                        targetFolder = submitDirs[0];
                        Log("自动切换到提交文件夹：" + targetFolder);
                        hasAssets = Directory.Exists(Path.Combine(targetFolder, "Assets"));
                        hasScreenshot = Directory.Exists(Path.Combine(targetFolder, "截图"));
                    }
                }

                if (!hasAssets || !hasScreenshot)
                {
                    Log("文件夹结构不符合要求：缺少 Assets 或 截图 文件夹");
                    return;
                }

                // 从父目录寻找 psd 文件
                string parentFolder = Directory.GetParent(targetFolder)?.FullName ?? "";
                if (string.IsNullOrEmpty(parentFolder))
                {
                    Log("无法获取父目录");
                    return;
                }

                var psdFiles = Directory.GetFiles(parentFolder, "*.psd", SearchOption.AllDirectories);
                Log($"在父目录中找到 {psdFiles.Length} 个PSD文件");

                var validNames = new System.Collections.Generic.List<string>();
                foreach (var psd in psdFiles)
                {
                    string baseName = Path.GetFileNameWithoutExtension(psd);
                    // 修改正则：仅匹配开头的“前缀_数字”，不再要求结尾
                    var match = Regex.Match(baseName, @"^([A-Za-z]+)_(\d+)");
                    if (match.Success)
                    {
                        string extracted = match.Value; // 例如 "Hand_01987"
                        validNames.Add(extracted);
                        Log($"  提取命名: {extracted} (来自 {baseName})");
                    }
                    else
                    {
                        Log($"  跳过: {baseName} (格式不符)");
                    }
                }

                validNames = validNames.Distinct().OrderBy(n => n).ToList();

                if (validNames.Count == 0)
                {
                    Log("未找到符合 前缀_数字.psd 格式的文件");
                    return;
                }

                Log($"找到 {validNames.Count} 个有效命名：");
                foreach (var name in validNames)
                {
                    Log("  " + name);
                }

                // 复制到剪贴板（通过 cmd clip 工具）
                try
                {
                    string textToCopy = string.Join("\r\n", validNames);
                    string safe = textToCopy.Replace("%", "%%").Replace("\"", "\"\"");
                    var psi = new System.Diagnostics.ProcessStartInfo("cmd", "/c " +
                        "echo " + safe + " | clip")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    System.Diagnostics.Process.Start(psi);
                    Log("已将命名复制到剪贴板");
                }
                catch (Exception ex)
                {
                    Log("复制剪贴板失败：" + ex.Message);
                }

                // 提取公共贴图
                Log("正在提取公共贴图到总文件夹...");
                string[] texturePatterns = new[] { "*_A.png", "*_D.png", "*_D.psd" };

                bool hasCommonTextures = true;
                var commonTextures = new System.Collections.Generic.List<string>();
                foreach (var pattern in texturePatterns)
                {
                    var matches = Directory.GetFiles(targetFolder, pattern, SearchOption.TopDirectoryOnly);
                    if (matches.Length == 0)
                    {
                        hasCommonTextures = false;
                        break;
                    }
                }

                if (!hasCommonTextures)
                {
                    // 查找第一个模板文件夹（排除Assets和截图）
                    var templateFolders = Directory.GetDirectories(targetFolder)
                        .Where(d => !string.Equals(Path.GetFileName(d), "Assets", StringComparison.OrdinalIgnoreCase)
                                 && !string.Equals(Path.GetFileName(d), "截图", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (templateFolders.Length > 0)
                    {
                        var firstTemplate = templateFolders[0];
                        Log("从模板文件夹提取公共贴图: " + Path.GetFileName(firstTemplate));

                        foreach (var pattern in texturePatterns)
                        {
                            var textureFiles = Directory.GetFiles(firstTemplate, pattern, SearchOption.TopDirectoryOnly);
                            foreach (var textureFile in textureFiles)
                            {
                                string destPath = Path.Combine(targetFolder, Path.GetFileName(textureFile));
                                if (!File.Exists(destPath))
                                {
                                    File.Copy(textureFile, destPath, true);
                                    Log("  提取: " + Path.GetFileName(textureFile));
                                }
                            }
                        }
                    }
                    else
                    {
                        Log("未找到模板文件夹，无法提取公共贴图");
                    }
                }
                else
                {
                    Log("公共贴图已存在于总文件夹");
                }

                // 重新收集公共贴图列表
                foreach (var pattern in texturePatterns)
                {
                    commonTextures.AddRange(Directory.GetFiles(targetFolder, pattern, SearchOption.TopDirectoryOnly));
                }

                // 标记并清理模板
                Log("正在标记源文件并清理重复贴图...");
                var markedItems = new System.Collections.Generic.List<(string OriginalPath, string MarkedPath)>();

                var allFolders = Directory.GetDirectories(targetFolder)
                    .Where(d => Path.GetFileName(d).Contains("_")
                                && !string.Equals(Path.GetFileName(d), "Assets", StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(Path.GetFileName(d), "截图", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                foreach (var folder in allFolders)
                {
                    string folderName = Path.GetFileName(folder);
                    var m = Regex.Match(folderName, "^(.+?)_(.+)$");
                    if (m.Success)
                    {
                        string suffix = m.Groups[2].Value;
                        if (!Regex.IsMatch(suffix, "^\\d+$"))
                        {
                            if (!folderName.EndsWith("_删"))
                            {
                                string newName = folderName + "_删";
                                string newPath = Path.Combine(targetFolder, newName);
                                try
                                {
                                    if (Directory.Exists(newPath))
                                        Directory.Delete(newPath, true);

                                    foreach (var pattern in texturePatterns)
                                    {
                                        var texFiles = Directory.GetFiles(folder, pattern, SearchOption.TopDirectoryOnly);
                                        foreach (var tex in texFiles)
                                        {
                                            File.Delete(tex);
                                            Log("  删除模板贴图: " + folderName + "/" + Path.GetFileName(tex));
                                        }
                                    }

                                    Directory.Move(folder, newPath);
                                    markedItems.Add((folder, newPath));
                                    Log("已标记: " + folderName + " -> " + newName);
                                }
                                catch (Exception ex)
                                {
                                    Log("标记失败: " + folderName + " / " + ex.Message);
                                }
                            }
                        }
                    }
                }

                // 创建新文件夹
                Log("正在创建新文件夹...");
                var markedTemplates = Directory.GetDirectories(targetFolder)
                    .Where(d => Path.GetFileName(d).EndsWith("_删", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                int successCount = 0, errorCount = 0;

                foreach (var fileName in validNames)
                {
                    var nameMatch = Regex.Match(fileName, "^(.+?)_(.+)$");
                    if (!nameMatch.Success)
                    {
                        Log("命名格式错误: " + fileName);
                        errorCount++;
                        continue;
                    }

                    string prefix = nameMatch.Groups[1].Value;

                    string? boyTemplate = null, girlTemplate = null, genericTemplate = null;
                    foreach (var template in markedTemplates)
                    {
                        string baseName = Path.GetFileName(template).Replace("_删", "");
                        if (baseName.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase))
                        {
                            if (baseName.EndsWith("_boy", StringComparison.OrdinalIgnoreCase))
                                boyTemplate = template;
                            else if (baseName.EndsWith("_girl", StringComparison.OrdinalIgnoreCase))
                                girlTemplate = template;
                            else if (!Regex.IsMatch(baseName, "_(boy|girl)$", RegexOptions.IgnoreCase))
                                genericTemplate = template;
                        }
                    }

                    var templatesToProcess = new System.Collections.Generic.List<(string Template, string Suffix)>();
                    if (boyTemplate != null && girlTemplate != null)
                    {
                        templatesToProcess.Add((boyTemplate, "_boy"));
                        templatesToProcess.Add((girlTemplate, "_girl"));
                    }
                    else if (genericTemplate != null)
                    {
                        templatesToProcess.Add((genericTemplate, ""));
                    }
                    else if (boyTemplate != null || girlTemplate != null)
                    {
                        if (boyTemplate != null) templatesToProcess.Add((boyTemplate, "_boy"));
                        else templatesToProcess.Add((girlTemplate!, "_girl"));
                    }
                    else
                    {
                        Log("未找到模板: " + fileName);
                        errorCount++;
                        continue;
                    }

                    foreach (var tpl in templatesToProcess)
                    {
                        string newFolderName = fileName + tpl.Suffix;
                        string sourcePath = tpl.Template;
                        string destPath = Path.Combine(targetFolder, newFolderName);
                        try
                        {
                            if (Directory.Exists(destPath))
                                Directory.Delete(destPath, true);

                            Directory.CreateDirectory(destPath);
                            Log("创建文件夹: " + newFolderName);

                            var maxFile = Directory.GetFiles(sourcePath, "*.max", SearchOption.TopDirectoryOnly).FirstOrDefault();
                            if (maxFile != null)
                            {
                                string newMaxName = fileName + ".max";
                                File.Copy(maxFile, Path.Combine(destPath, newMaxName), true);
                                Log("  复制.max文件: " + newMaxName);
                            }
                            else
                            {
                                Log("  警告: 未找到.max文件");
                            }

                            foreach (var textureFile in commonTextures)
                            {
                                string textureBase = Path.GetFileNameWithoutExtension(textureFile);
                                string textureExt = Path.GetExtension(textureFile);
                                var m2 = Regex.Match(textureBase, "_(A|D)$");
                                if (m2.Success)
                                {
                                    string suffix = m2.Value;
                                    string newTexName = fileName + suffix + textureExt;
                                    File.Copy(textureFile, Path.Combine(destPath, newTexName), true);
                                    Log("  复制贴图: " + newTexName);
                                }
                                else
                                {
                                    Log("  跳过非标准贴图: " + Path.GetFileName(textureFile));
                                }
                            }

                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            Log("创建失败: " + newFolderName + " - " + ex.Message);
                            errorCount++;
                        }
                    }
                }

                // 删除标记文件夹
                Log("正在删除原模板文件夹...");
                foreach (var item in markedItems)
                {
                    try
                    {
                        if (Directory.Exists(item.MarkedPath))
                            Directory.Delete(item.MarkedPath, true);
                        Log("已删除: " + Path.GetFileName(item.MarkedPath));
                    }
                    catch { }
                }

                // 删除总文件夹中的原贴图
                Log("正在删除总文件夹中的原贴图...");
                foreach (var tex in commonTextures)
                {
                    try
                    {
                        if (File.Exists(tex))
                            File.Delete(tex);
                        Log("已删除原贴图: " + Path.GetFileName(tex));
                    }
                    catch { }
                }

                Log($"创建完成：{successCount} 成功，{errorCount} 失败");
                Log("文件夹规范整理完成");
            }
            catch (Exception ex)
            {
                Log("处理过程中异常：" + ex.Message);
            }
        }
    }
}