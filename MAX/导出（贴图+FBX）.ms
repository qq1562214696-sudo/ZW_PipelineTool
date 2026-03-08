-- 导出（贴图+FBX）.ms 修复版

fn main =
(
    local thisDir = getFilenamePath (getThisScriptFilename())
    fileIn (thisDir + "\\QF_Tools_Lib.ms")
   
    qf_log "=== 导出（贴图+FBX） 开始执行 ==="
   
    local assetsFolder = loadAssetsPath()
    qf_log ("Assets 路径: " + assetsFolder)
   
    if assetsFolder == "" or (doesFileExist assetsFolder) == false do
    (
        qf_log "错误：Unity Assets 路径无效或不存在"
        messageBox "请先在 QF工具精简版 中【浏览】设置 Unity Assets 目录！" title:"路径错误"
        return()
    )
   
    local prefixArray = #("H","L","HHair","HFace","HTop","HBottom","HShoe","LHair","LFace","LTop","LBottom","LShoe","HPet","LPet",\
    "Head","Wing","Hand","FlyDecoration","Tail","LeftBracelet","RightBracelet","LeftAnklet","RightAnklet","Earring","Mask","Glass",\
    "LeftHandChain","RightHandRing")
   
    fn copyToClipboard str = (...) -- 保持原函数（你没贴完整内容，这里保持占位）
   
    fn copyMaxFileNameToClipboard = (...) -- 保持原函数
   
    fn showListenerWindow = (...) -- 保持原函数
   
    fn safeFormatToListener msgStr = (...) -- 保持原函数
   
    fn detectGender = (...) -- 保持原函数
   
    fn getTexFolder prefix playerID genderFolder isPetNum assetsBase = (...) -- 保持原函数
   
    fn getExportFolder prefix playerID genderFolder isPetNum assetsBase = (...) -- 保持原函数
   
    if maxFileName == "" do
    (
        qf_log "错误：当前场景未保存"
        messagebox "当前场景未保存，请先保存文件"
        return()
    )
   
    qf_log ("当前 max 文件: " + maxFileName + " 路径: " + maxFilePath)
   
    max unhide all
    qf_log "已执行 max unhide all"
   
    local exportObjs = #()
    for obj in objects do
    (
        local objName = obj.name
        if matchPattern objName pattern:"Bip*" or matchPattern objName pattern:"Mesh*" or
           matchPattern objName pattern:"Root*" or matchPattern objName pattern:"Node*" or
           matchPattern objName pattern:"Bone*" do append exportObjs obj
    )
    qf_log ("找到可导出对象: " + exportObjs.count as string + " 个")
   
    if exportObjs.count == 0 do
    (
        qf_log "错误：未找到可导出对象"
        messageBox "没有找到任何名称以 Bip、Mesh、Root、Bone 开头的对象！\n导出将无法继续。" title:"警告"
        return()
    )
   
    selectionSets["Export"] = exportObjs
    qf_log "已创建 Export 选择集"
   
    local objNameArray = #()
    if selectionSets["Export"] != undefined do
    (
        for obj in selectionSets["Export"] do append objNameArray obj.name
        qf_log ("Export 选择集中对象: " + objNameArray.count as string + " 个")
    )
   
    FbxExporterSetParam "Animation" false
    FbxExporterSetParam "Cameras" false
    FbxExporterSetParam "Lights" false
    qf_log "FBX 参数设置完成（无动画/灯光/摄像机）"
   
    local expLog = "导出结果：\n"
   
    local fileFullName = maxFilePath + maxFileName
    local fileName = getFilenameFile fileFullName
    local nameArray = filterString fileName "_"
    local prefix = nameArray[1]
    local playerID = nameArray[2]
    qf_log ("解析文件名 -> 前缀: " + prefix + " ID: " + playerID)
   
    local imageArray = getFiles (maxFilePath + "\\*.png")
    qf_log ("当前目录找到 PNG 文件: " + imageArray.count as string + " 个")
   
    if imageArray.count == 0 then
    (
        expLog += "该目录下没有PNG贴图文件\n"
        qf_log "提示：没有找到 PNG 贴图"
    )
    else
    (
        local isPetNum = findItem prefixArray prefix
        local genderFolder = detectGender()
        qf_log ("性别文件夹: " + genderFolder + " 前缀编号: " + isPetNum as string)
       
        if isPetNum != 0 then
        (
            local texFolder = getTexFolder prefix playerID genderFolder isPetNum assetsFolder
            qf_log ("贴图目标文件夹: " + texFolder)
            if texFolder != "" do
            (
                if not (doesFileExist texFolder) do
                (
                    makeDir texFolder
                    qf_log ("已创建贴图文件夹: " + texFolder)
                )
                for imgFile in imageArray do
                (
                    local imgFileName = filenameFromPath imgFile
                    local targetPath = texFolder + "\\" + imgFileName
                    try
                    (
                        copyFile imgFile targetPath
                        expLog += "贴图导出成功: " + imgFileName + "\n"
                        qf_log ("贴图拷贝成功: " + imgFileName + " → " + targetPath)
                    )
                    catch
                    (
                        expLog += "贴图导出失败: " + imgFileName + "\n"
                        qf_log ("贴图拷贝失败: " + imgFileName + " → " + targetPath)
                    )
                )
            )
        )
    )
   
    clearSelection()
    qf_log "已清空当前选择"
   
    local selectedCount = 0
    for objName in objNameArray do
    (
        local obj = getNodeByName objName
        if obj != undefined do
        (
            selectMore obj
            selectedCount += 1
        )
    )
    qf_log ("重新选择导出对象: " + selectedCount as string + " 个")
   
    if selectedCount == 0 then
    (
        expLog += "错误：选择集中没有找到任何有效对象\n"
        qf_log "错误：重新选择后仍无有效对象"
    )
    else
    (
        local isPetNum = findItem prefixArray prefix
        local genderFolder = detectGender()
        local fbxFileName = prefix + "_" + playerID
        qf_log ("FBX 文件名: " + fbxFileName + ".fbx")
       
        if isPetNum == 0 then
        (
            expLog += "错误：未知的前缀 " + prefix + "\n"
            qf_log ("错误：未知前缀 " + prefix)
        )
        else
        (
            local exportFolder = getExportFolder prefix playerID genderFolder isPetNum assetsFolder
            qf_log ("FBX 导出目标文件夹: " + exportFolder)
            if exportFolder == "" do
            (
                expLog += "错误：无法确定导出路径（可能缺少性别标识）\n"
                qf_log "错误：导出路径为空（性别或路径问题）"
            )
            if exportFolder != "" do
            (
                if not (doesFileExist exportFolder) do
                (
                    makeDir exportFolder
                    qf_log ("已创建 FBX 导出文件夹: " + exportFolder)
                )
                local fbxPath = exportFolder + "\\" + fbxFileName + ".fbx"
                qf_log ("准备导出 FBX 到: " + fbxPath)
                local success = exportFile fbxPath #noPrompt selectedOnly:true using:FBXEXP
                if success then
                (
                    expLog += "FBX导出成功: " + fbxFileName + ".fbx\n"
                    qf_log ("FBX 导出成功: " + fbxPath)
                )
                else
                (
                    expLog += "FBX导出失败：导出 " + fbxFileName + ".fbx 失败\n"
                    qf_log ("FBX 导出失败: " + fbxPath)
                )
            )
        )
    )
   
    if selectionSets["Export"] != undefined do
    (
        select selectionSets["Export"]
        qf_log "已恢复 Export 选择集"
    )
   
    copyMaxFileNameToClipboard()
    qf_log "已尝试复制 max 文件名到剪贴板"
   
    if expLog != "导出结果：\n" then
    (
        safeFormatToListener expLog
        qf_log "已通过 Listener 输出完整导出日志"
    )
    else
    (
        messagebox "导出完成！"
        qf_log "导出完成，无特殊日志记录"
    )
   
    qf_log "=== 导出（贴图+FBX） 执行结束 ==="
)

main()