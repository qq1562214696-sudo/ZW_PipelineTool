-- 选择法线翻转的面.ms 修复版

fn main =
(
    local thisDir = getFilenamePath (getThisScriptFilename())
    fileIn (thisDir + "\\QF_Tools_Lib.ms")
   
    qf_log "=== 选择法线翻转面 开始 ==="
   
    fn processObject obj =
    (
        -- 原函数保持不变
        qf_log ("检查对象: " + obj.name + " 类: " + (classOf obj as string))
        if not (iskindof obj PolyMeshObject or iskindof obj Editable_Poly) do
        (
            qf_log (obj.name + " 不支持法线检查")
            return #(false, #())
        )
       
        local n_face = #()
        local cp = snapshot obj
        local nf = #{1..cp.numFaces}
        qf_log ("快照创建，总面数: " + cp.numFaces as string)
       
        local g = for i in nf collect getFaceNormal cp i
        meshop.unifyNormals cp nf
        local x = for i in nf collect getFaceNormal cp i
       
        for i in nf do if g[i] != x[i] do append n_face i
       
        delete cp
       
        if n_face.count > 0 then
        (
            qf_log (obj.name + " 发现 " + n_face.count as string + " 个翻转法线面")
            return #(true, n_face)
        )
        else
        (
            qf_log (obj.name + " 无法线翻转面")
            return #(false, #())
        )
    )
   
    if selection.count == 1 then
    (
        local obj = selection[1]
        qf_log ("选中单个对象: " + obj.name)
        if (iskindof obj PolyMeshObject or iskindof obj Editable_Poly) then
        (
            local result = processObject obj
            if result[1] then
            (
                select obj
                max modify mode
                modPanel.setCurrentObject obj.baseObject
                subObjectLevel = 4
                polyop.setFaceSelection obj result[2]
                update obj
                qf_log ("已选中 " + result[2].count as string + " 个翻转法线面")
            )
            else
            (
                messagebox "当前对象没有法线翻转的面" title:"提示"
                qf_log "当前对象无法线翻转面"
            )
            return()
        )
    )
   
    qf_log "开始全场景扫描法线翻转面"
    local found = false
    local foundObj = undefined
    local foundFaces = #()
    for obj in objects where (iskindof obj PolyMeshObject or iskindof obj Editable_Poly) do
    (
        local result = processObject obj
        if result[1] do
        (
            found = true
            foundObj = obj
            foundFaces = result[2]
            exit
        )
    )
   
    if found then
    (
        qf_log (foundObj.name + " 有 " + foundFaces.count as string + " 个翻转法线面")
        select foundObj
        max modify mode
        modPanel.setCurrentObject foundObj.baseObject
        subObjectLevel = 4
        polyop.setFaceSelection foundObj foundFaces
        update foundObj
        qf_log "已自动选中并高亮翻转法线面"
    )
    else
    (
        messagebox "场景中没有找到包含法线翻转面的对象" title:"提示"
        qf_log "全场景无法线翻转面"
    )
   
    qf_log "=== 选择法线翻转面 结束 ==="
)

main()