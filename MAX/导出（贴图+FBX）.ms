-- 导出（贴图+FBX）.ms 精简修复版（适配硬编码路径）
fn main =
(
    local thisDir = getFilenamePath (getThisScriptFilename())
    fileIn (thisDir + "\\QF_Tools_Lib.ms")
    qf_log "=== 导出（贴图+FBX） 开始执行 ==="
    local assetsFolder = loadAssetsPath()
    qf_log ("Assets 路径: " + assetsFolder)
    if assetsFolder == "" or not (doesFileExist assetsFolder) do
    (
        qf_log "错误：Unity Assets 路径无效或不存在"
        messageBox ("Assets 路径无效或不存在：\n" + assetsFolder + "\n请检查 QF_Tools_Lib.ms 中的路径设置") title:"路径错误"
        return()
    )
    if maxFileName == "" do
    (
        qf_log "错误：当前场景未保存"
        messageBox "当前场景未保存，请先保存文件"
        return()
    )
    max unhide all
    qf_log "已执行 max unhide all"
    local exportObjs = for o in objects where
        matchPattern o.name pattern:"Bip*" or
        matchPattern o.name pattern:"Mesh*" or
        matchPattern o.name pattern:"Root*" or
        matchPattern o.name pattern:"Node*" or
        matchPattern o.name pattern:"Bone*" collect o
    qf_log ("找到可导出对象: " + exportObjs.count as string + " 个")
    if exportObjs.count == 0 do
    (
        messageBox "没有找到任何名称以 Bip、Mesh、Root、Bone 开头的对象！\n导出将无法继续。" title:"警告"
        return()
    )
    selectionSets["Export"] = exportObjs
    local objNameArray = for o in exportObjs collect o.name
    FbxExporterSetParam "Animation" false
    FbxExporterSetParam "Cameras" false
    FbxExporterSetParam "Lights" false
    local expLog = "导出结果：\n"
    local fileName = getFilenameFile maxFileName
    local nameArray = filterString fileName "_"
    local prefix = nameArray[1]
    local playerID = nameArray[2]
    local imageArray = getFiles (maxFilePath + "*.png")
    if imageArray.count > 0 then
    (
        local isPetNum = findItem prefixArray prefix
        local genderFolder = detectGender()
        if isPetNum != 0 then
        (
            local texFolder = getTexFolder prefix playerID genderFolder isPetNum assetsFolder
            if texFolder != "" do
            (
                makeDir texFolder all:true
                for imgFile in imageArray do
                (
                    local imgFileName = filenameFromPath imgFile
                    local targetPath = texFolder + "\\" + imgFileName
                    if copyFile imgFile targetPath then
                        expLog += "贴图导出成功: " + imgFileName + "\n"
                    else
                        expLog += "贴图导出失败: " + imgFileName + "\n"
                )
            )
        )
    )
    else
    (
        expLog += "当前目录下没有找到任何 .png 贴图文件\n"
    )
    -- 导出 FBX
    clearSelection()
    for n in objNameArray do
    (
        local obj = getNodeByName n
        if obj != undefined do selectMore obj
    )
    local isPetNum = findItem prefixArray prefix
    local genderFolder = detectGender()
    local fbxFileName = prefix + "_" + playerID
    if isPetNum == 0 then
    (
        expLog += "错误：未知的前缀 " + prefix + "\n"
    )
    else
    (
        local exportFolder = getExportFolder prefix playerID genderFolder isPetNum assetsFolder
        if exportFolder != "" then
        (
            makeDir exportFolder all:true
            local fbxPath = exportFolder + "\\" + fbxFileName + ".fbx"
            local success = exportFile fbxPath #noPrompt selectedOnly:true using:FBXEXP
            if success then
                expLog += "FBX 导出成功: " + fbxFileName + ".fbx\n"
            else
                expLog += "FBX 导出失败: " + fbxFileName + ".fbx\n"
        )
        else
        (
            expLog += "错误：无法确定导出路径（可能缺少性别标识）\n"
        )
    )
    if selectionSets["Export"] != undefined do select selectionSets["Export"]
    format "%\n" expLog
    if findString expLog "错误" != undefined then
        messageBox expLog title:"导出有错误"
    else
        messageBox "导出完成（贴图 + FBX）" title:"完成"
    qf_log "=== 导出（贴图+FBX） 执行结束 ==="
)
main()