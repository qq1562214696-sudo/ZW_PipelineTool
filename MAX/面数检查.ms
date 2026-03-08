-- 面数检查.ms 修复版

fn main =
(
    local thisDir = getFilenamePath (getThisScriptFilename())
    local libPath = thisDir + "QF_Tools_Lib.ms"
    if doesFileExist libPath then (
        fileIn libPath
        qf_log "成功加载共享库 QF_Tools_Lib.ms"
    ) else (
        messageBox ("找不到共享库文件:\n" + libPath) title:"错误"
        return()
    )
   
    qf_log "=== 面数检查 开始执行 ==="
   
    local limitTable = #(
        #("Head", 1000),
        #("Wing", 2000),
        #("Hand", 1500),
        #("Tail", 1000),
        #("Mask", 100),
        #("Glass", 400),
        #("FlyDecoration", 2000),
        #("FloatDecoration", 2000),
        #("Earring", 300),
        #("LeftBracelet", 400),
        #("RightBracelet", 400),
        #("RightHandRing", 400),
        #("LeftHandChain", 500),
        #("LightRing", 2000)
    )
   
    local fileName = getFilenameFile maxFileName
    qf_log ("当前文件名: " + fileName)
   
    local nameParts = filterString fileName "_"
    local prefix = ""
    local prefixNum = 0
   
    if nameParts.count >= 1 then
    (
        prefix = nameParts[1]
        if (prefix == "H" or prefix == "L") and nameParts.count >= 2 then
            prefix = nameParts[2]
        prefixNum = findItem (for item in limitTable collect item[1]) prefix
        qf_log ("解析前缀: " + prefix + " 类型编号: " + prefixNum as string)
    )
   
    local meshObjects = for o in objects where matchPattern o.name pattern:"Mesh*" ignoreCase:true collect o
    qf_log ("找到 Mesh* 对象: " + meshObjects.count as string + " 个")
   
    if meshObjects.count == 0 do
    (
        qf_log "错误：未找到 Mesh* 对象"
        messageBox "场景中没有找到任何名称以 'Mesh' 开头的对象！" title:"提示"
        return()
    )
   
    local totalTris = 0
    local msgDetails = ""
    local objCount = meshObjects.count
   
    for obj in meshObjects do
    (
        local triCount = 0
        if isKindOf obj.baseObject Editable_Poly then
            triCount = polyOp.getNumFaces obj.baseObject
        else if isKindOf obj.baseObject Editable_Mesh then
            triCount = meshOp.getNumFaces obj.baseObject
        else if canConvertTo obj Editable_Poly then
        (
            local snap = snapshotAsMesh obj
            triCount = getNumFaces snap
            delete snap
        )
        else continue
       
        totalTris += triCount
        msgDetails += obj.name + "：" + (triCount as string) + " 面\n"
        qf_log (obj.name + " → " + triCount as string + " 面")
    )
   
    qf_log ("总三角面数: " + totalTris as string)
   
    local formattedTotal = ""
    local strTotal = totalTris as string
    local len = strTotal.count
    if len > 3 then
    (
        local groups = #()
        local i = len
        while i > 0 do
        (
            local start = i - 2
            if start < 1 do start = 1
            local group = substring strTotal start (i - start + 1)
            append groups group
            i = start - 1
        )
        for i = groups.count to 1 by -1 do
        (
            formattedTotal += groups[i]
            if i > 1 do formattedTotal += ","
        )
    )
    else formattedTotal = strTotal
   
    local modelType = "未知类型"
    local faceLimit = 0
    local isOverLimit = false
    if prefixNum > 0 then
    (
        modelType = limitTable[prefixNum][1]
        faceLimit = limitTable[prefixNum][2]
        isOverLimit = totalTris > faceLimit
        qf_log ("类型: " + modelType + " 上限: " + faceLimit as string + " 当前: " + totalTris as string)
    )
    else
        qf_log "未匹配类型，面数上限未知"
   
    local finalMsg = ""
    finalMsg += "道具类型：" + modelType + "\n"
    finalMsg += "Mesh数量：" + (objCount as string) + " 个\n"
    finalMsg += "总三角面数：" + formattedTotal + "\n"
    if prefixNum > 0 then
        finalMsg += "面数上限：" + (faceLimit as string) + "\n\n"
    else
        finalMsg += "面数上限：未知（未匹配到类型）\n\n"
   
    if isOverLimit then
    (
        finalMsg += "（超出 " + ((totalTris - faceLimit) as string) + " 面）\n\n"
        qf_log ("面数超限，超出: " + ((totalTris - faceLimit) as string) + " 面")
    )
    else
        qf_log "面数在限制范围内"
   
    finalMsg += "---------\n" + msgDetails
   
    if prefixNum == 0 then
        finalMsg += "\n提示：未识别到道具类型，请检查max文件名格式"
   
    messageBox finalMsg title:"面数检查完成" beep:isOverLimit
   
    qf_log "=== 面数检查 执行结束 ==="
)

main()