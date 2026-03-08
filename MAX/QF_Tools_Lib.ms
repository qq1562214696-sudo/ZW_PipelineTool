-- ================== QF_Tools 共享库 ==================

-- 全局日志文件路径（所有脚本共用）
global MAX_LOG_PATH = (getDir #temp) + "\\Max_Log.txt"

-- 统一的日志写入函数
fn qf_log msg = (
    try (
        local f = openFile MAX_LOG_PATH mode:"a"
        if f != undefined do (
            local t = getLocalTime()
            local timeStr = (t[2] as string) + ":" + (formattedPrint t[4] format:"02d") + ":" + (formattedPrint t[5] format:"02d") + "." + (formattedPrint t[6] format:"03d")
            format "[%] %\n" timeStr msg to:f
            close f
        )
    ) catch()
)

-- 加载时记录（便于确认加载成功）
qf_log "QF_Tools_Lib.ms 已加载"

prefixArray = #("H","L","HHair","HFace","HTop","HBottom","HShoe","LHair","LFace","LTop","LBottom","LShoe","HPet","LPet",\
"Head","Wing","Hand","FlyDecoration","Tail","LeftBracelet","RightBracelet","LeftAnklet","RightAnklet","Earring","Mask","Glass",\
"LeftHandChain","RightHandRing")

fn loadAssetsPath =
(
    try
    (
        local configFilePath = GetDir #plugcfg + "\\AvatorExpConfig.xml"
        if not (doesFileExist configFilePath) do return ""
        local doc = dotNetObject "system.xml.xmlDocument"
        doc.Load configFilePath
        local rootElem = doc.DocumentElement
        return rootElem.GetAttribute "UnityAssetsPath"
    )
    catch
    (
        return ""
    )
)

-- 以下是你原有的其他函数（setupBackgroundPlane、copyToClipboard 等），保持不变
-- 如果你有更多函数，直接接在下面即可