-- 选择多边面.ms 修复版（逻辑已封装，此处仅包一层main）

fn main =
(
    local thisDir = getFilenamePath (getThisScriptFilename())
    fileIn (thisDir + "\\QF_Tools_Lib.ms")
   
    qf_log "=== 选择多边面（>=5边） 开始 ==="
   
    fn processObject obj =
    (
        -- 原函数保持不变
        qf_log ("检查对象: " + obj.name + " 类: " + (classOf obj.baseObject as string))
        local canProcess = (classOf obj.baseObject == Editable_Poly) or (canConvertTo obj Editable_Poly)
        if not canProcess do
        (
            qf_log (obj.name + " 不支持 Editable_Poly 检查")
            return #(false, #())
        )
       
        if classOf obj.baseObject == Editable_Poly then
        (
            local numFaces = polyOp.getNumFaces obj
            qf_log (obj.name + " 是 Editable_Poly，总面数: " + numFaces as string)
            local n_face = for f = 1 to numFaces where (polyOp.getFaceVerts obj f).count > 4 collect f
            if n_face.count > 0 then
            (
                qf_log (obj.name + " 找到 " + n_face.count as string + " 个 >=5 边面")
                return #(true, n_face)
            )
            else
            (
                qf_log (obj.name + " 无 >=5 边面")
                return #(false, #())
            )
        )
        else
        (
            qf_log (obj.name + " 需要快照转换检查")
            local tmpObj = snapshot obj
            convertToPoly tmpObj
            local numFaces = polyOp.getNumFaces tmpObj
            local n_face = for f = 1 to numFaces where (polyOp.getFaceVerts tmpObj f).count > 4 collect f
            delete tmpObj
            if n_face.count > 0 then
            (
                qf_log ("快照检查: " + obj.name + " 找到 " + n_face.count as string + " 个 >=5 边面")
                return #(true, n_face)
            )
            else
            (
                qf_log ("快照检查: " + obj.name + " 无 >=5 边面")
                return #(false, #())
            )
        )
    )
   
    if selection.count == 1 then
    (
        local obj = selection[1]
        qf_log ("选中单个对象: " + obj.name)
        local canProcess = (classOf obj.baseObject == Editable_Poly) or (canConvertTo obj Editable_Poly)
        if canProcess then
        (
            local result = processObject obj
            if result[1] then
            (
                if classOf obj.baseObject != Editable_Poly do convertToPoly obj
                max modify mode
                modPanel.setCurrentObject obj
                subObjectLevel = 4
                polyOp.setFaceSelection obj result[2]
                update obj
                qf_log ("已选中 " + result[2].count as string + " 个 >=5 边面")
            )
            else
            (
                messagebox "当前对象没有五边及以上面" title:"提示"
                qf_log "当前对象无 >=5 边面"
            )
            return()
        )
    )
   
    qf_log "开始全场景扫描 >=5 边面"
    local found = false
    local foundObj = undefined
    local foundFaces = #()
    for obj in objects do
    (
        local canProcess = (classOf obj.baseObject == Editable_Poly) or (canConvertTo obj Editable_Poly)
        if canProcess do
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
    )
   
    if found then
    (
        qf_log (foundObj.name + " 找到 " + foundFaces.count as string + " 个 >=5 边面")
        if classOf foundObj.baseObject != Editable_Poly do convertToPoly foundObj
        select foundObj
        max modify mode
        modPanel.setCurrentObject foundObj
        subObjectLevel = 4
        polyOp.setFaceSelection foundObj foundFaces
        update foundObj
        qf_log "已自动选中并高亮第一个含 n-gon 的对象"
    )
    else
    (
        messagebox "场景中没有找到包含五边及以上面的对象" title:"提示"
        qf_log "全场景无 >=5 边面"
    )
   
    qf_log "=== 选择多边面 结束 ==="
)

main()