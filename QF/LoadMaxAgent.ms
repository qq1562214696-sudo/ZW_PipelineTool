try
(
    python.ExecuteFile (getDir #scripts + "\\Startup\\MaxAgent.py")
    format "=== 无网络命令文件监控已加载 ===\n" to:listener
)
catch err
(
    format "加载失败: %\n" (err as string) to:listener
)