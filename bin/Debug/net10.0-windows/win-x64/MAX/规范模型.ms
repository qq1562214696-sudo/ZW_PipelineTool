-- 规范模型.ms （作用域修复版）

fileIn (getFilenamePath (getThisScriptFilename()) + "\\QF_Tools_Lib.ms")

qf_log "=== 规范模型.ms 开始加载 ==="

-- ──────────────────────────────────────────────
--  移到顶层定义的函数
-- ──────────────────────────────────────────────

fn setupBackgroundPlane = 
(
    try
    (
        local bgPlane = getNodeByName "原画" exact:true
        if bgPlane == undefined or not (isKindOf bgPlane Plane) do 
        (
            qf_log "'原画' 对象不存在或不是 Plane 类型"
            return false
        )

        if maxFileName == "" do 
        (
            qf_log "max 文件尚未保存"
            return false
        )

        local maxFileNameOnly = getFilenameFile maxFileName
        local currentDir = maxFilePath
        local psdFile = undefined
        local levels = 0

        while levels < 5 and currentDir != "" do
        (
            currentDir = getFilenamePath (pathConfig.removePathLeaf currentDir)
            local psdFiles = getFiles (currentDir + "*.psd")
            for f in psdFiles while psdFile == undefined do
            (
                if findString (filenameFromPath f) maxFileNameOnly != undefined do
                    psdFile = f
            )
            if psdFile != undefined do exit
            levels += 1
        )

        if psdFile == undefined do 
        (
            qf_log "向上 5 级目录未找到匹配的 PSD 文件"
            return false
        )

        qf_log ("找到原画 PSD: " + psdFile)

        local mat = Standard name:"原画材质"
        local bmpTex = Bitmaptexture filename:psdFile name:"原画贴图"
        mat.diffuseMap = bmpTex
        mat.diffuseMapEnable = true
        mat.showInViewport = true
        bgPlane.material = mat

        try
        (
            local b = openBitmap psdFile
            if b != undefined do
            (
                bgPlane.length = b.height / 1000.0
                bgPlane.width  = b.width  / 1000.0
                close b
            )
        )
        catch
        (
            qf_log "读取 PSD 尺寸失败，不调整平面大小"
        )

        bgPlane.isFrozen = true
        qf_log "原画平面已设置完成并冻结"
        return true
    )
    catch
    (
        qf_log ("setupBackgroundPlane 异常: " + getCurrentException())
        return false
    )
)

fn ResetPivotFn Obj =
(
    local target = matrix3 [1,0,0] [0,0,1] [0,-1,0] [0,0,0]
    local ChldTms = in coordsys target (for c in Obj.children collect c.transform)

    local TmScale   = scaleMatrix   Obj.objectOffsetScale
    local TmRot     = Obj.objectOffsetRot as matrix3
    local TmPos     = transMatrix    Obj.objectOffsetPos
    local TmOffset  = TmScale * TmRot * TmPos

    TmOffset *= Obj.transform * inverse target

    Obj.transform = target
    Obj.objectOffsetPos   = TmOffset.translationpart
    Obj.objectOffsetRot   = TmOffset.rotationpart
    Obj.objectOffsetScale = TmOffset.scalepart

    for i = 1 to Obj.children.count do
        Obj.children[i].transform = ChldTms[i] * inverse target * Obj.transform
)

-- ──────────────────────────────────────────────
--  主逻辑
-- ──────────────────────────────────────────────

fn main =
(
    qf_log "=== 规范模型 开始执行 ==="

    if maxFileName == "" do
    (
        messageBox "请先保存当前max文件！"
        return()
    )

    local bgSuccess = setupBackgroundPlane()
    qf_log ("背景平面设置结果: " + bgSuccess as string)

    local pureName = getFilenameFile maxFileName
    if matchPattern pureName pattern:"*_fin" ignoreCase:true do
        pureName = substring pureName 1 (pureName.count - 4)

    for i = 1 to 24 do meditMaterials[i] = Standard()

    local mat = Standard name:(pureName + "_D") showInViewport:true

    local diffPath = maxFilePath + pureName + "_D.png"
    local opacPath = maxFilePath + pureName + "_A.png"

    if doesFileExist diffPath then
    (
        mat.diffuseMap = Bitmaptexture filename:(filenameFromPath diffPath)
        mat.diffuseMapEnable = true
        qf_log "漫反射贴图已加载"
    )
    else
    (
        messageBox ("未找到漫反射贴图:\n" + diffPath)
        qf_log "未找到漫反射贴图"
    )

    if doesFileExist opacPath then
    (
        mat.opacityMap = Bitmaptexture filename:(filenameFromPath opacPath)
        mat.opacityMapEnable = true
        qf_log "透明贴图已加载"
    )

    meditMaterials[1] = mat

    undo "规范模型" on
    (
        local meshCount = 0
        for o in objects where matchPattern o.name pattern:"Mesh*" ignoreCase:true do
        (
            try (o.material = mat; meshCount += 1) catch ()
        )
        qf_log ("已为 " + meshCount as string + " 个 Mesh 对象赋材质")

        for o in objects where matchPattern o.name pattern:"Mesh*" ignoreCase:true do
            if o.parent != undefined do o.parent = undefined

        local polys = for o in objects where classOf o == Editable_Poly collect o

        for p in polys do
        (
            polyop.setMapSupport p 0 off
            polyop.setMapSupport p 2 off
            polyop.setMapSupport p 3 off
        )

        if polys.count > 0 do
        (
            local oldSel = selection as array
            select polys
            macros.run "Animation Tools" "DeleteSelectedAnimation"
            select oldSel
        )

        for p in polys do
        (
            ResetPivotFn p
            p.scale = [1,1,1]
        )

        local root, bip, nodeObj
        for o in objects do
        (
            if matchPattern o.name pattern:"Root*" do root = o
            if matchPattern o.name pattern:"Bip*"  do bip = o
            if matchPattern o.name pattern:"Node*" and nodeObj == undefined do nodeObj = o
        )

        local linkTarget = if bip != undefined then bip \
                          else if root != undefined then root \
                          else nodeObj

        if linkTarget != undefined do
        (
            local meshes = for o in objects where matchPattern o.name pattern:"Mesh*" collect o
            for m in meshes where m != linkTarget and m.parent != linkTarget do
                m.parent = linkTarget
        )
    )

    qf_log "=== 规范模型 执行结束 ==="
)

-- 执行主函数
main()