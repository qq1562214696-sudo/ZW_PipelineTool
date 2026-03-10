-- 导出FBX(当前).ms 精简修复版（适配硬编码路径）
fn main =
(
    local thisDir = getFilenamePath (getThisScriptFilename())
    fileIn (thisDir + "\\QF_Tools_Lib.ms")
    qf_log "=== 导出FBX(当前) 开始执行 ==="
    local assetsFolder = loadAssetsPath()
    if assetsFolder == "" or not (doesFileExist assetsFolder) do
    (
        messageBox ("Assets 路径无效或不存在：\n" + assetsFolder + "\n请检查 QF_Tools_Lib.ms 中的路径设置") title:"路径错误"
        return()
    )
    if maxFileName == "" do
    (
        messageBox "当前场景尚未保存，请先保存 .max 文件！"
        return()
    )
    FbxExporterSetParam "Animation" false
    FbxExporterSetParam "Cameras" false
    FbxExporterSetParam "Lights" false
    max unhide all
    local exportObjs = for o in objects where
        matchPattern o.name pattern:"Bip*" or
        matchPattern o.name pattern:"Mesh*" or
        matchPattern o.name pattern:"Root*" or
        matchPattern o.name pattern:"Node*" or
        matchPattern o.name pattern:"Bone*" collect o
    if exportObjs.count == 0 do
    (
        messageBox "没有找到任何名称以 Bip、Mesh、Root、Bone 开头的对象！" title:"警告"
        return()
    )
    selectionSets["Export"] = exportObjs
    local objNameArray = for o in exportObjs collect o.name
    clearSelection()
    for n in objNameArray do
    (
        local obj = getNodeByName n
        if obj != undefined do selectMore obj
    )
    local fileName = getFilenameFile maxFileName
    local nameArray = filterString fileName "_"
    local prefix = nameArray[1]
    local playerID = nameArray[2]
    local isPetNum = findItem prefixArray prefix
    local genderFolder = detectGender()
    local expLog = "导出FBX结果：\n"
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
            local fbxFileName = prefix + "_" + playerID
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
        messageBox "当前文件 FBX 导出完成" title:"完成"
    qf_log "=== 导出FBX(当前) 执行结束 ==="
)
main()