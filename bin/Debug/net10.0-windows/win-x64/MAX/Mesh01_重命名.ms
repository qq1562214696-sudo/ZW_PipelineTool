-- Mesh01_重命名.ms 修复版

fn main =
(
    local thisDir = getFilenamePath (getThisScriptFilename())
    fileIn (thisDir + "\\QF_Tools_Lib.ms")
   
    qf_log "=== Mesh01_ 重命名 开始 ==="
   
    undo "重命名所有Poly为Mesh01_" on
    (
        max unhide all
        qf_log "已执行 max unhide all"
       
        local allPolys = for o in objects where classOf o == Editable_Poly collect o
        qf_log ("找到 Editable_Poly 对象: " + allPolys.count as string + " 个")
       
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
   
    qf_log "=== Mesh01_ 重命名 结束 ==="
)

main()