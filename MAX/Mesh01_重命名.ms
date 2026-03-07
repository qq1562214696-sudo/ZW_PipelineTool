-- Mesh01_重命名.ms（完整版，无额外依赖）
fn main = 
(
    undo "重命名所有Poly为Mesh01_" on
    (
        max unhide all
        
        local allPolys = for o in objects where classOf o == Editable_Poly collect o
        
        if allPolys.count == 0 do
        (
            messageBox "场景中没有找到任何 可编辑多边形 对象！" title:"提示"
            return()
        )
        
        for i = 1 to allPolys.count do
        (
            local newName = "Mesh" + (formattedPrint i format:"02d") + "_"
            allPolys[i].name = newName
        )
    )
)

main()