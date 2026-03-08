-- 规范模型.ms 修复版

fn main =
(
    local thisDir = getFilenamePath (getThisScriptFilename())
    fileIn (thisDir + "\\QF_Tools_Lib.ms")
   
    qf_log "=== 规范模型 开始执行 ==="
    qf_log ("当前 max 文件: " + maxFileName)
    qf_log ("路径: " + maxFilePath)
   
    fn setupBackgroundPlane =
    (
        -- 原函数内容保持不变
        try
        (
            local bgPlane = getNodeByName "原画" exact:true
            if bgPlane == undefined do (qf_log "未找到 '原画' 对象"; return false)
            if not (isKindOf bgPlane Plane) do (qf_log "'原画' 不是 Plane 类型"; return false)
           
            if maxFileName == "" do (qf_log "max 文件未保存"; return false)
            local maxFileDir = maxFilePath
            local maxFileNameOnly = getFilenameFile maxFileName
           
            local currentDir = maxFileDir
            local psdFile = undefined
            local levelsChecked = 0
            while levelsChecked < 5 and currentDir != "" do
            (
                currentDir = getFilenamePath (pathConfig.removePathLeaf currentDir)
                if currentDir == "" do exit
                local psdFiles = getFiles (currentDir + "\\*.psd")
                for file in psdFiles do
                (
                    local fileNameOnly = filenameFromPath file
                    if findString fileNameOnly maxFileNameOnly != undefined do
                    (
                        psdFile = file
                        qf_log ("找到匹配 PSD: " + file)
                        exit
                    )
                )
                if psdFile != undefined do exit
                levelsChecked += 1
            )
            if psdFile == undefined do (qf_log "未找到匹配 PSD"; return false)
           
            local bgMaterial = Standard()
            bgMaterial.name = "原画材质"
            local diffuseMap = Bitmaptexture()
            diffuseMap.filename = psdFile
            diffuseMap.name = "原画贴图"
            bgMaterial.diffuseMap = diffuseMap
            bgMaterial.diffuseMapEnable = true
            bgMaterial.showInViewport = true
            bgPlane.material = bgMaterial
           
            try
            (
                local bmp = openBitMap psdFile
                if bmp != undefined do
                (
                    local widthInPixels = bmp.width
                    local heightInPixels = bmp.height
                    close bmp
                    local widthInMeters = widthInPixels / 1000.0
                    local heightInMeters = heightInPixels / 1000.0
                    bgPlane.length = heightInMeters
                    bgPlane.width = widthInMeters
                    qf_log ("调整原画平面大小: " + widthInMeters as string + " x " + heightInMeters as string)
                )
            )
            catch (qf_log "PSD 尺寸读取失败，不调整大小")
           
            bgPlane.isFrozen = true
            qf_log "原画对象已冻结并应用材质"
            return true
        )
        catch (qf_log ("setupBackgroundPlane 异常: " + (getCurrentException())); return false)
    )
   
    fn ResetPivotFn Obj =
    (
        -- 原函数保持不变
        local target = matrix3 [1,0,0] [0,0,1] [0,-1,0] [0,0,0]
        local ChldTms = in coordSys target (for Chld in Obj.children collect Chld.transform)
        local TmScale = scaleMatrix Obj.objectOffsetScale
        local TmRot = Obj.objectOffsetRot as matrix3
        local TmPos = transMatrix Obj.objectOffsetPos
        local TmOffset = TmScale * TmRot * TmPos
        TmOffset *= Obj.transform * inverse target
        Obj.transform = target
        Obj.objectOffsetPos = TmOffset.translation
        Obj.objectOffsetRot = TmOffset.rotation
        Obj.objectOffsetScale = TmOffset.scale
        for i = 1 to Obj.children.count do
            Obj.children[i].transform = ChldTms[i] * inverse target * Obj.transform
    )
   
    local bgSuccess = setupBackgroundPlane()
    qf_log ("背景平面设置结果: " + (bgSuccess as string))
   
    local pureMaxFileName = getFilenameFile (maxFilePath + maxFileName)
    if matchPattern pureMaxFileName pattern:"*_fin" ignoreCase:true do
        pureMaxFileName = substring pureMaxFileName 1 (pureMaxFileName.count - 4)
   
    if maxFileName == "" do
    (
        qf_log "错误：max 文件未保存"
        messageBox "请先保存当前max文件！"
        return()
    )
   
    for i in 1 to 24 do meditMaterials[i] = Standard()
    qf_log "已清空 24 个材质球"
   
    local firstMat = Standard()
    firstMat.name = pureMaxFileName + "_D"
    firstMat.showInViewport = true
   
    local diffuseMapPath = maxFilePath + pureMaxFileName + "_D.png"
    local alphaMapPath = maxFilePath + pureMaxFileName + "_A.png"
   
    if doesFileExist diffuseMapPath then
    (
        local diffuseMap = Bitmaptexture()
        diffuseMap.filename = filenameFromPath diffuseMapPath
        diffuseMap.name = pureMaxFileName + "_D"
        firstMat.diffuseMap = diffuseMap
        firstMat.diffuseMapEnable = true
        qf_log ("漫反射贴图加载成功: " + diffuseMapPath)
    )
    else
    (
        qf_log ("未找到漫反射贴图: " + diffuseMapPath)
        messageBox ("未找到漫反射贴图: " + pureMaxFileName + "_D.png" + "\n请确保贴图文件与max文件在同一目录下。")
    )
   
    if doesFileExist alphaMapPath then
    (
        local alphaMap = Bitmaptexture()
        alphaMap.filename = filenameFromPath alphaMapPath
        alphaMap.name = pureMaxFileName + "_A"
        firstMat.opacityMap = alphaMap
        firstMat.opacityMapEnable = true
        qf_log ("透明贴图加载成功: " + alphaMapPath)
    )
    else
    (
        qf_log ("未找到透明贴图: " + alphaMapPath)
    )
   
    meditMaterials[1] = firstMat
    qf_log "主材质已赋给材质槽 1"
   
    undo on
    (
        local meshCount = 0
        for obj in objects where matchPattern obj.name pattern:"Mesh*" ignoreCase:true do
        (
            try
            (
                obj.material = firstMat
                meshCount += 1
            )
            catch ()
        )
        qf_log ("已为 " + meshCount as string + " 个 Mesh* 对象赋材质")
       
        for obj in objects where matchPattern obj.name pattern:"Mesh*" ignoreCase:true do
        (
            if obj.parent != undefined do obj.parent = undefined
        )
        qf_log "已断开所有 Mesh* 对象的父链接"
       
        local allPolyObjects = for obj in objects where classOf obj == Editable_Poly collect obj
        qf_log ("找到 Editable_Poly 对象: " + allPolyObjects.count as string + " 个")
       
        for obj in allPolyObjects do
        (
            polyop.setMapSupport obj 0 false
            polyop.setMapSupport obj 2 false
            polyop.setMapSupport obj 3 false
        )
        qf_log "已关闭所有 Editable_Poly 的贴图通道 0/2/3"
       
        if allPolyObjects.count > 0 do
        (
            local oldSelection = selection as array
            select allPolyObjects
            macros.run "Animation Tools" "DeleteSelectedAnimation"
            select oldSelection
            qf_log "已删除选中对象的动画键"
        )
       
        for obj in allPolyObjects do
        (
            ResetPivotFn obj
            obj.scale = [1.0,1.0,1.0]
        )
        qf_log "已对所有 Editable_Poly 执行轴心重置和缩放归一"
       
        local hasRoot = false; local hasBip = false
        local rootObj = undefined; local bipObj = undefined; local nodeObj = undefined
       
        for o in objects do
        (
            local n = o.name
            if matchPattern n pattern:"Root*" do (hasRoot = true; if rootObj == undefined do rootObj = o)
            if matchPattern n pattern:"Bip*" do (hasBip = true; if bipObj == undefined do bipObj = o)
            if matchPattern n pattern:"Node*" and nodeObj == undefined do nodeObj = o
        )
       
        local linkTarget = undefined
        if hasRoot and not hasBip and nodeObj == undefined then linkTarget = rootObj
        else if not hasRoot and nodeObj != undefined and not hasBip then linkTarget = nodeObj
        else if hasRoot and hasBip then linkTarget = bipObj
       
        if linkTarget != undefined do
        (
            local allMeshes = for o in objects where matchPattern o.name pattern:"Mesh*" collect o
            for m in allMeshes do
            (
                if m != linkTarget and m.parent != linkTarget do
                    m.parent = linkTarget
            )
            qf_log "所有 Mesh* 已链接到目标根节点"
        )
    )
   
    qf_log "=== 规范模型 执行结束 ==="
)

main()