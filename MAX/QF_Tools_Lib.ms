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

-- 加载时记录
qf_log "QF_Tools_Lib.ms 已加载"

-- 前缀数组（公共常量）
global prefixArray = #("H","L","HHair","HFace","HTop","HBottom","HShoe","LHair","LFace","LTop","LBottom","LShoe","HPet","LPet",\
"Head","Wing","Hand","FlyDecoration","Tail","LeftBracelet","RightBracelet","LeftAnklet","RightAnklet","Earring","Mask","Glass",\
"LeftHandChain","RightHandRing")

-- 从配置文件读取 Assets 路径
fn loadAssetsPath = (
    try (
        local configFilePath = GetDir #plugcfg + "\\AvatorExpConfig.xml"
        if not (doesFileExist configFilePath) do return ""
        local doc = dotNetObject "system.xml.xmlDocument"
        doc.Load configFilePath
        local rootElem = doc.DocumentElement
        return rootElem.GetAttribute "UnityAssetsPath"
    ) catch (
        return ""
    )
)

-- 从路径猜测性别文件夹（Male / Female）
fn detectGender = (
    local genderFolder = ""
    case of (
        (matchPattern maxFilePath pattern:"*_boy*" ignoreCase:true): genderFolder = "Male"
        (matchPattern maxFilePath pattern:"*_girl*" ignoreCase:true): genderFolder = "Female"
    )
    genderFolder
)

-- 计算模型导出目录（Model 文件夹）
fn getExportFolder prefix playerID genderFolder isPetNum assetsBase = (
    local exportFolder = ""
    if isPetNum == 0 do return ""

    if isPetNum <= 12 then (
        local partName = substring prefix 2 -1
        local partFolder = case partName of (
            "Hair":   "01Hair"
            "Face":   "02Face"
            "Top":    "03Top"
            "Bottom": "04Bottom"
            "Shoe":   "05Shoe"
            default:  ""
        )
        if partFolder != "" do
            exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\" + partFolder + "\\" + partName + "_" + playerID + "\\Model"
    )
    else if isPetNum == 13 or isPetNum == 14 then
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
    else if isPetNum == 20 then (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\12LeftBracelet\\LeftBracelet_" + playerID + "\\Model"
    )
    else if isPetNum == 21 then (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\13RightBracelet\\RightBracelet_" + playerID + "\\Model"
    )
    else if isPetNum == 22 then (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\14LeftAnklet\\LeftAnklet_" + playerID + "\\Model"
    )
    else if isPetNum == 23 then (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\15RightAnklet\\RightAnklet_" + playerID + "\\Model"
    )
    else if isPetNum == 24 then (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\17Earring\\Earring_" + playerID + "\\Model"
    )
    else if isPetNum == 25 then (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\19Mask\\Mask_" + playerID + "\\Model"
    )
    else if isPetNum == 26 then (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\07Glass\\Glass_" + playerID + "\\Model"
    )
    else if isPetNum == 27 then
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\21LeftHandChain\\LeftHandChain_" + playerID + "\\Model"
    else if isPetNum == 28 then (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\20RightHandRing\\RightHandRing_" + playerID + "\\Model"
    )
    exportFolder
)

-- 计算贴图导出目录（Textures 文件夹）
fn getTexFolder prefix playerID genderFolder isPetNum assetsBase = (
    local texFolder = ""
    if isPetNum == 0 do return ""

    if isPetNum <= 12 then (
        local partName = substring prefix 2 -1
        texFolder = case partName of (
            "Hair":   assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\01Hair\\Hair_"   + playerID + "\\Textures"
            "Face":   assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\02Face\\Face_"   + playerID + "\\Textures"
            "Top":    assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\03Top\\Top_"     + playerID + "\\Textures"
            "Bottom": assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\04Bottom\\Bottom_" + playerID + "\\Textures"
            "Shoe":   assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\05Shoe\\Shoe_"   + playerID + "\\Textures"
            default:  ""
        )
    )
    else if isPetNum == 15 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\06Head\\Head_"               + playerID + "\\Textures"
    else if isPetNum == 16 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\08Wing\\Wing_"               + playerID + "\\Textures"
    else if isPetNum == 17 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\09Hand\\Hand_"               + playerID + "\\Textures"
    else if isPetNum == 18 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\11FlyDecoration\\FlyDecoration_" + playerID + "\\Textures"
    else if isPetNum == 19 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\18Tail\\Tail_"               + playerID + "\\Textures"
    else if isPetNum == 20 then (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\12LeftBracelet\\LeftBracelet_" + playerID + "\\Textures"
    )
    else if isPetNum == 21 then (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\13RightBracelet\\RightBracelet_" + playerID + "\\Textures"
    )
    else if isPetNum == 22 then (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\14LeftAnklet\\LeftAnklet_" + playerID + "\\Textures"
    )
    else if isPetNum == 23 then (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\15RightAnklet\\RightAnklet_" + playerID + "\\Textures"
    )
    else if isPetNum == 24 then (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\17Earring\\Earring_" + playerID + "\\Textures"
    )
    else if isPetNum == 25 then (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\19Mask\\Mask_" + playerID + "\\Textures"
    )
    else if isPetNum == 26 then (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\07Glass\\Glass_" + playerID + "\\Textures"
    )
    else if isPetNum == 27 then
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\21LeftHandChain\\LeftHandChain_" + playerID + "\\Textures"
    else if isPetNum == 28 then (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\20RightHandRing\\RightHandRing_" + playerID + "\\Textures"
    )
    texFolder
)