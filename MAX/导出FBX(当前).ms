-- 导出FBX(当前).ms 修复版

fn main =
(
    local thisDir = getFilenamePath (getThisScriptFilename())
    fileIn (thisDir + "\\QF_Tools_Lib.ms")
   
    qf_log "=== 导出FBX(当前) 开始执行 ==="
   
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
   
    fn copyToClipboard str =
    (
        try
        (
            local dotNetClass = dotNetClass "System.Windows.Forms.Clipboard"
            dotNetClass.SetText str
            return true
        )
        catch
        (
            try (setclipboardText str; return true)
            catch (return false)
        )
    )
   
    fn copyMaxFileNameToClipboard =
    (
        local fileName = ""
        if maxFileName != "" then
        (
            fileName = getFilenameFile maxFileName
            if copyToClipboard fileName then
                qf_log ("已复制文件名到剪切板: " + fileName)
            else
                qf_log ("复制文件名到剪切板失败: " + fileName)
        )
        else
            qf_log "当前文件未保存，无法复制文件名"
        fileName
    )
   
    fn showListenerWindow =
    (
        try (actionMan.executeAction 0 "40472")
        catch
        (
            try (listener.Open())
            catch
            (
                try (DOSCommand "max listener"; format "正在打开侦听器窗口...\n")
                catch (format "无法自动打开侦听器窗口，请手动按F11\n")
            )
        )
    )
   
    fn safeFormatToListener msgStr =
    (
        try
        (
            showListenerWindow()
            format msgStr
            forceCompleteRedraw()
        )
        catch (try (print msgStr) catch ())
    )
   
    fn detectGender =
    (
        local genderFolder = ""
        case of
        (
            (matchPattern maxFilePath pattern:"*_boy*"): genderFolder = "Male"
            (matchPattern maxFilePath pattern:"*_girl*"): genderFolder = "Female"
        )
        genderFolder
    )
   
    fn getExportFolder prefix playerID genderFolder isPetNum assetsBase =
    (
        local exportFolder = ""
        if isPetNum == 0 then return ""
        else if isPetNum <= 12 then
        (
            local partName = substring prefix 2 100
            local partFolder = case partName of
            (
                "Hair": "01Hair"
                "Face": "02Face"
                "Top": "03Top"
                "Bottom": "04Bottom"
                "Shoe": "05Shoe"
            )
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\" + partFolder + "\\" + partName + "_" + playerID + "\\Model"
        )
        else if isPetNum == 13 or isPetNum == 14 then
            exportFolder = assetsBase + "\\ArtWork\\Pet\\Main\\Pet_" + playerID + "\\Model"
        else if isPetNum == 15 then exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\06Head\\Head_" + playerID + "\\Model"
        else if isPetNum == 16 then exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\08Wing\\Wing_" + playerID + "\\Model"
        else if isPetNum == 17 then exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\09Hand\\Hand_" + playerID + "\\Model"
        else if isPetNum == 18 then exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\11FlyDecoration\\FlyDecoration_" + playerID + "\\Model"
        else if isPetNum == 19 then exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\18Tail\\Tail_" + playerID + "\\Model"
        else if isPetNum == 20 then
        (
            if genderFolder == "" then return ""
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\12LeftBracelet\\LeftBracelet_" + playerID + "\\Model"
        )
        else if isPetNum == 21 then
        (
            if genderFolder == "" then return ""
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\13RightBracelet\\RightBracelet_" + playerID + "\\Model"
        )
        else if isPetNum == 22 then
        (
            if genderFolder == "" then return ""
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\14LeftAnklet\\LeftAnklet_" + playerID + "\\Model"
        )
        else if isPetNum == 23 then
        (
            if genderFolder == "" then return ""
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\15RightAnklet\\RightAnklet_" + playerID + "\\Model"
        )
        else if isPetNum == 24 then
        (
            if genderFolder == "" then return ""
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\17Earring\\Earring_" + playerID + "\\Model"
        )
        else if isPetNum == 25 then
        (
            if genderFolder == "" then return ""
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\19Mask\\Mask_" + playerID + "\\Model"
        )
        else if isPetNum == 26 then
        (
            if genderFolder == "" then return ""
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\07Glass\\Glass_" + playerID + "\\Model"
        )
        else if isPetNum == 27 then
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\21LeftHandChain\\LeftHandChain_" + playerID + "\\Model"
        else if isPetNum == 28 then
        (
            if genderFolder == "" then return ""
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\20RightHandRing\\RightHandRing_" + playerID + "\\Model"
        )
        exportFolder
    )
   
    if maxFileName == "" do
    (
        qf_log "错误：当前场景未保存"
        messageBox "当前场景尚未保存，请先保存 .max 文件！"
        return()
    )
   
    FbxExporterSetParam "Animation" false
    FbxExporterSetParam "Cameras" false
    FbxExporterSetParam "Lights" false
    qf_log "FBX 参数设置完成"
   
    local expLog = "导出FBX结果：\n"
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
    qf_log ("找到可导出对象数量：" + (exportObjs.count as string))
   
    if exportObjs.count == 0 do
    (
        qf_log "错误：未找到可导出对象"
        messageBox "没有找到任何名称以 Bip、Mesh、Root、Bone 开头的对象！\n导出将无法继续。" title:"警告"
        return()
    )
   
    selectionSets["Export"] = exportObjs
    local objNameArray = #()
    if selectionSets["Export"] != undefined do
    (
        for obj in selectionSets["Export"] do append objNameArray obj.name
        qf_log ("选择集中对象数量：" + (objNameArray.count as string))
    )
   
    clearSelection()
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
    qf_log ("重新选择对象数量：" + (selectedCount as string))
   
    if selectedCount == 0 do
    (
        expLog += "错误：选择集中没有找到任何有效对象\n"
        qf_log "错误：选择集中没有有效对象"
    )
   
    local fileFullName = maxFilePath + maxFileName
    local fileName = getFilenameFile fileFullName
    local nameArray = filterString fileName "_"
    local prefix = nameArray[1]
    local playerID = nameArray[2]
    qf_log ("解析文件名：前缀=" + prefix + " ID=" + playerID)
   
    local isPetNum = findItem prefixArray prefix
    local genderFolder = detectGender()
   
    if isPetNum == 0 then
    (
        expLog += "错误：未知的前缀 " + prefix + "\n"
        qf_log ("错误：未知前缀 " + prefix)
    )
    else
    (
        local exportFolder = getExportFolder prefix playerID genderFolder isPetNum assetsFolder
        qf_log ("计算得到导出文件夹：" + exportFolder)
        if exportFolder == "" do
        (
            expLog += "错误：无法确定导出路径（可能缺少性别标识）\n"
            qf_log "错误：无法确定导出路径"
        )
        if exportFolder != "" do
        (
            if (doesFileExist exportFolder) == false do makeDir exportFolder
            local fbxFileName = prefix + "_" + playerID
            local fbxPath = exportFolder + "\\" + fbxFileName + ".fbx"
            qf_log ("准备导出 FBX：" + fbxPath)
            local success = exportFile fbxPath #noPrompt selectedOnly:true using:FBXEXP
            if success then
            (
                expLog += "FBX导出成功: " + fbxFileName + ".fbx\n"
                qf_log ("FBX导出成功: " + fbxPath)
            )
            else
            (
                expLog += "FBX导出失败：导出 " + fbxFileName + ".fbx 失败\n"
                qf_log ("FBX导出失败: " + fbxPath)
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
   
    if expLog != "导出FBX结果：\n" then
    (
        safeFormatToListener expLog
        qf_log "已通过 Listener 输出导出结果"
    )
    else
    (
        messageBox "当前文件 FBX 导出完成，无错误记录。" title:"完成"
        qf_log "导出完成，无错误记录"
    )
   
    qf_log "=== 导出FBX(当前) 执行结束 ==="
)

main()