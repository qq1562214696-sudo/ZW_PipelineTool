-- Mesh01_重命名.ms 修复版

fn main =
(
    local thisDir = getFilenamePath (getThisScriptFilename())
    
    -- 尝试加载共享库，并检查是否成功
    local libPath = thisDir + "\\QF_Tools_Lib.ms"
    if doesFileExist libPath then
    (
        fileIn libPath
    )
    else
    (
        messageBox ("找不到共享库文件:\n" + libPath + "\n请确保 QF_Tools_Lib.ms 与本脚本在同一目录") title:"加载失败"
        return()
    )
    
    -- 再次确认 qf_log 是否可用
    if qf_log == undefined do
    (
        messageBox "qf_log 函数未定义，脚本库加载异常" title:"错误"
        return()
    )
    
    undo "重命名所有Poly为Mesh01_" on
    (
        max unhide all
        
        local allPolys = for o in objects where classOf o == Editable_Poly collect o

        if allPolys.count == 0 do
        (
            qf_log "错误：未找到 Editable_Poly"
            messageBox "场景中没有找到任何 可编辑多边形 对象！" title:"提示"
            return()
        )
        
        for i = 1 to allPolys.count do
        (
            local newName = "Mesh" + (formattedPrint i format:"02d") + "_"
            local oldName = allPolys[i].name
            allPolys[i].name = newName
            qf_log ("重命名: " + oldName + " → " + newName)
        )
        
        qf_log ("重命名完成，共 " + allPolys.count as string + " 个对象")
    )
)

-- 执行主函数
main()