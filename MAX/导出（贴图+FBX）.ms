-- 导出（贴图+FBX）.ms（完整版，包含所有依赖）
fn main = 
(
    local thisDir = getFilenamePath (getThisScriptFilename())
    fileIn (thisDir + "\\QF_Tools_Lib.ms")
    
    local assetsFolder = loadAssetsPath()
    if assetsFolder == "" or (doesFileExist assetsFolder) == false do
    (
        messageBox "请先在 QF工具精简版 中【浏览】设置 Unity Assets 目录！" title:"路径错误"
        return()
    )
    
    -- 全局变量
    prefixArray = #("H","L","HHair","HFace","HTop","HBottom","HShoe","LHair","LFace","LTop","LBottom","LShoe","HPet","LPet",\
    "Head","Wing","Hand","FlyDecoration","Tail","LeftBracelet","RightBracelet","LeftAnklet","RightAnklet","Earring","Mask","Glass",\
    "LeftHandChain","RightHandRing")
    
    -- 辅助函数 - 全部完整定义
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
            try
            (
                setclipboardText str
                return true
            )
            catch
            (
                return false
            )
        )
    )
    
    fn copyMaxFileNameToClipboard =
    (
        local fileName = ""
        if maxFileName != "" then
        (
            fileName = getFilenameFile maxFileName
            if copyToClipboard fileName then
            (
                print ("\n\n\n" + "已复制文件名到剪切板: " + fileName)
            )
            else
            (
                print ("\n\n\n" + "复制文件名到剪切板失败: " + fileName)
            )
        )
        else
        (
            print "当前文件未保存，无法复制文件名"
        )
        return fileName
    )
    
    fn showListenerWindow =
    (
        try
        (
            actionMan.executeAction 0 "40472"  -- 侦听器窗口
        )
        catch
        (
            try
            (
                listener.Open()
            )
            catch
            (
                try
                (
                    DOSCommand "max listener"
                    format "正在打开侦听器窗口...\n"
                )
                catch
                (
                    format "无法自动打开侦听器窗口，请手动按F11\n"
                )
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
        catch
        (
            try
            (
                print msgStr
            )
            catch()
        )
    )
    
    fn detectGender =
    (
        local genderFolder = ""
        case of
        (
            (matchPattern maxFilePath pattern:"*_boy*"): genderFolder = "Male"
            (matchPattern maxFilePath pattern:"*_girl*"): genderFolder = "Female"
        )
        return genderFolder
    )
    
    fn getTexFolder prefix playerID genderFolder isPetNum assetsBase =
    (
        local texFolder = ""
        if isPetNum == 0 then return ""
        else if isPetNum <= 12 then
        (
            local partName = substring prefix 2 100
            case partName of
            (
                "Hair": texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\01Hair\\Hair_" + playerID + "\\Textures"
                "Face": texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\02Face\\Face_" + playerID + "\\Textures"
                "Top": texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\03Top\\Top_" + playerID + "\\Textures"
                "Bottom": texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\04Bottom\\Bottom_" + playerID + "\\Textures"
                "Shoe": texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\05Shoe\\Shoe_" + playerID + "\\Textures"
            )
        )
        else if isPetNum == 15 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\06Head\\Head_" + playerID + "\\Textures"
        else if isPetNum == 16 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\08Wing\\Wing_" + playerID + "\\Textures"
        else if isPetNum == 17 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\09Hand\\Hand_" + playerID + "\\Textures"
        else if isPetNum == 18 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\11FlyDecoration\\FlyDecoration_" + playerID + "\\Textures"
        else if isPetNum == 19 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\18Tail\\Tail_" + playerID + "\\Textures"
        else if isPetNum == 20 then
        (
            if genderFolder == "" then return ""
            texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\12LeftBracelet\\LeftBracelet_" + playerID + "\\Textures"
        )
        else if isPetNum == 21 then
        (
            if genderFolder == "" then return ""
            texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\13RightBracelet\\RightBracelet_" + playerID + "\\Textures"
        )
        else if isPetNum == 22 then
        (
            if genderFolder == "" then return ""
            texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\14LeftAnklet\\LeftAnklet_" + playerID + "\\Textures"
        )
        else if isPetNum == 23 then
        (
            if genderFolder == "" then return ""
            texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\15RightAnklet\\RightAnklet_" + playerID + "\\Textures"
        )
        else if isPetNum == 24 then
        (
            if genderFolder == "" then return ""
            texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\17Earring\\Earring_" + playerID + "\\Textures"
        )
        else if isPetNum == 25 then
        (
            if genderFolder == "" then return ""
            texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\19Mask\\Mask_" + playerID + "\\Textures"
        )
        else if isPetNum == 26 then
        (
            if genderFolder == "" then return ""
            texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\07Glass\\Glass_" + playerID + "\\Textures"
        )
        else if isPetNum == 27 then
            texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\21LeftHandChain\\LeftHandChain_" + playerID + "\\Textures"
        else if isPetNum == 28 then
        (
            if genderFolder == "" then return ""
            texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\20RightHandRing\\RightHandRing_" + playerID + "\\Textures"
        )
        texFolder
    )
    
    fn getExportFolder prefix playerID genderFolder isPetNum assetsBase =
    (
        local exportFolder = ""
        if isPetNum == 0 then return ""
        else if isPetNum <= 12 then
        (
            local partName = substring prefix 2 100
            local partFolder = ""
            case partName of
            (
                "Hair": partFolder = "01Hair"
                "Face": partFolder = "02Face"
                "Top": partFolder = "03Top"
                "Bottom": partFolder = "04Bottom"
                "Shoe": partFolder = "05Shoe"
            )
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\" + partFolder + "\\" + partName + "_" + playerID + "\\Model"
        )
        else if (isPetNum == 13 or isPetNum == 14) then
            exportFolder = assetsBase + "\\ArtWork\\Pet\\Main\\Pet_" + playerID + "\\Model"
        else if isPetNum == 15 then
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\06Head\\Head_" + playerID + "\\Model"
        else if isPetNum == 16 then
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\08Wing\\Wing_" + playerID + "\\Model"
        else if isPetNum == 17 then
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\09Hand\\Hand_" + playerID + "\\Model"
        else if isPetNum == 18 then
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\11FlyDecoration\\FlyDecoration_" + playerID + "\\Model"
        else if isPetNum == 19 then
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\18Tail\\Tail_" + playerID + "\\Model"
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
    
    -- 主逻辑开始
    if (doesFileExist assetsFolder) == false do
    (
        Messagebox "请在地址栏指定正确的Unity工程Assets目录"
        return()
    )
    if maxFileName == "" do
    (
        messagebox "当前场景未保存，请先保存文件"
        return()
    )
    
    max unhide all
    local exportObjs = #()
    for obj in objects do
    (
        local objName = obj.name
        if matchPattern objName pattern:"Bip*" or matchPattern objName pattern:"Mesh*" or
           matchPattern objName pattern:"Root*" or matchPattern objName pattern:"Node*" or
           matchPattern objName pattern:"Bone*" do append exportObjs obj
    )
    if exportObjs.count == 0 do
    (
        messageBox "没有找到任何名称以 Bip、Mesh、Root、Bone 开头的对象！\n导出将无法继续。" title:"警告"
        return()
    )
    
    selectionSets["Export"] = exportObjs
    local objNameArray = #()
    if selectionSets["Export"] != undefined do
        for obj in selectionSets["Export"] do append objNameArray obj.name
    
    FbxExporterSetParam "Animation" false
    FbxExporterSetParam "Cameras" false
    FbxExporterSetParam "Lights" false
    
    local expLog = "导出结果：\n"
    
    -- 导出贴图
    local fileFullName = maxFilePath + maxFileName
    local fileName = getFilenameFile fileFullName
    local nameArray = filterString fileName "_"
    local prefix = nameArray[1]
    local playerID = nameArray[2]
    local imageArray = getFiles (maxFilePath + "\\*.png")
    if imageArray.count == 0 then
        expLog += "该目录下没有PNG贴图文件\n"
    else
    (
        local isPetNum = findItem prefixArray prefix
        local genderFolder = detectGender()
        if isPetNum != 0 then
        (
            local texFolder = getTexFolder prefix playerID genderFolder isPetNum assetsFolder
            if texFolder != "" do
            (
                if not (doesFileExist texFolder) do makeDir texFolder
                for imgFile in imageArray do
                (
                    local imgFileName = filenameFromPath imgFile
                    local targetPath = texFolder + "\\" + imgFileName
                    try
                    (
                        copyFile imgFile targetPath
                        expLog += "贴图导出成功: " + imgFileName + "\n"
                    )
                    catch
                    (
                        expLog += "贴图导出失败: 导出 " + imgFileName + " 失败\n"
                    )
                )
            )
        )
    )
    
    -- 导出 FBX
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
    if selectedCount == 0 then
        expLog += "错误：选择集中没有找到任何有效对象\n"
    else
    (
        local isPetNum = findItem prefixArray prefix
        local genderFolder = detectGender()
        local fbxFileName = prefix + "_" + playerID
        if isPetNum == 0 then
            expLog += "错误：未知的前缀 " + prefix + "\n"
        else
        (
            local exportFolder = getExportFolder prefix playerID genderFolder isPetNum assetsFolder
            if exportFolder == "" do
                expLog += "错误：无法确定导出路径（可能缺少性别标识）\n"
            if exportFolder != "" do
            (
                if not (doesFileExist exportFolder) do makeDir exportFolder
                local fbxPath = exportFolder + "\\" + fbxFileName + ".fbx"
                local success = exportFile fbxPath #noPrompt selectedOnly:true using:FBXEXP
                if success then
                    expLog += "FBX导出成功: " + fbxFileName + ".fbx\n"
                else
                    expLog += "FBX导出失败：导出 " + fbxFileName + ".fbx 失败\n"
            )
        )
    )
    
    if selectionSets["Export"] != undefined do select selectionSets["Export"]
    
    copyMaxFileNameToClipboard()
    
    if expLog != "导出结果：\n" then
        safeFormatToListener expLog
    else
        messagebox "导出完成！"
)

main()