-- ================== QF_Tools 共享库 ==================
-- 日志文件路径：固定放在脚本同级目录（即 MAX 文件夹下）
global MAX_LOG_PATH = (getFilenamePath (getThisScriptFilename())) + "Max_Log.txt"

-- 配置文件路径：与日志文件同级
global QF_CONFIG_PATH = (getFilenamePath (getThisScriptFilename())) + "QF_Config.txt"

-- 统一的日志写入函数（加强版：失败时输出到 Listener）
fn qf_log msg =
(
    try
    (
        local f = undefined
        
        -- 确保文件存在，如果不存在则创建（指定 UTF-8 编码）
        if not (doesFileExist MAX_LOG_PATH) then
        (
            f = createFile MAX_LOG_PATH encoding:#utf8
            if f != undefined then close f  -- 关闭刚刚创建的空文件
        )
        
        -- 以追加模式打开文件，指定 UTF-8 编码
        f = openFile MAX_LOG_PATH mode:"a" encoding:#utf8
        
        if f != undefined then
        (
            format "%\n" msg to:f
            close f
        )
        else
        (
            print ("[qf_log 警告] 无法打开文件: " + MAX_LOG_PATH)
            print ("[qf_log fallback] " + msg)
        )
    )
    catch
    (
        print ("[qf_log 异常] " + getCurrentException())
        print ("[qf_log fallback] " + msg)
        print ("日志路径尝试: " + MAX_LOG_PATH)
    )
)

-- 加载时记录
qf_log "公共方法库正确加载"

-- ================== 配置文件操作 ==================
-- 读取配置文件中的键值（如果不存在或没找到，返回 undefined）
fn loadQFConfig key =
(
    if not (doesFileExist QF_CONFIG_PATH) do
    (
        qf_log ("错误：配置文件不存在 -> " + QF_CONFIG_PATH)
        return undefined
    )
    
    local f = openFile QF_CONFIG_PATH mode:"r"
    if f == undefined do
    (
        qf_log ("错误：无法打开配置文件 -> " + QF_CONFIG_PATH)
        return undefined
    )
    
    local value = undefined
    seek f 0
    while not eof f do
    (
        local line = readline f
        if matchPattern line pattern:(key + "=*") do
        (
            value = substring line (key.count + 2) -1  -- 跳过 "key="
            exit
        )
    )
    close f
    
    if value == undefined do
        qf_log ("警告：配置文件中未找到键: " + key)
    
    value
)

-- 保存或更新配置文件中的键值（追加或覆盖）
fn saveQFConfig key value =
(
    local lines = #()
    local found = false
    
    if doesFileExist QF_CONFIG_PATH do
    (
        local f = openFile QF_CONFIG_PATH mode:"r"
        while not eof f do
        (
            local line = readline f
            if matchPattern line pattern:(key + "=*") then
            (
                append lines (key + "=" + value)
                found = true
            )
            else
                append lines line
        )
        close f
    )
    
    if not found do
        append lines (key + "=" + value)
    
    local f = openFile QF_CONFIG_PATH mode:"w"
    for l in lines do format "%\n" l to:f
    close f
    
    qf_log ("已保存配置: " + key + "=" + value)
)

-- ================== Assets 路径加载（强制从配置文件读取） ==================
fn loadAssetsPath =
(
    local cfgPath = loadQFConfig "AssetsPath"
    
    if cfgPath == undefined or cfgPath == "" do
    (
        qf_log "错误：配置文件中缺少 AssetsPath 或路径为空！请在工具界面设置 Unity Assets 路径并执行初始化。"
        messageBox "Assets 路径未设置！\n请在工具界面输入 Unity Assets 路径并点击【执行初始化】。" title:"路径错误"
        return ""  -- 返回空字符串，让调用方处理错误
    )
    
    if not (doesFileExist cfgPath) do
    (
        qf_log ("错误：配置中的 Assets 路径无效或不存在 -> " + cfgPath)
        messageBox ("配置中的 Assets 路径无效：\n" + cfgPath + "\n请检查路径并重新初始化。") title:"路径错误"
        return ""
    )
    
    qf_log ("当前使用的 Assets 路径: " + cfgPath)
    cfgPath
)

-- 前缀数组（公共常量）
global prefixArray = #("H","L","HHair","HFace","HTop","HBottom","HShoe","LHair","LFace","LTop","LBottom","LShoe","HPet","LPet",\
"Head","Wing","Hand","FlyDecoration","Tail","LeftBracelet","RightBracelet","LeftAnklet","RightAnklet","Earring","Mask","Glass",\
"LeftHandChain","RightHandRing")

-- 从路径猜测性别文件夹（Male / Female）
fn detectGender =
(
    local genderFolder = ""
    case of
    (
        (matchPattern maxFilePath pattern:"*_boy*" ignoreCase:true): genderFolder = "Male"
        (matchPattern maxFilePath pattern:"*_girl*" ignoreCase:true): genderFolder = "Female"
    )
    genderFolder
)

-- 计算模型导出目录（Model 文件夹）
fn getExportFolder prefix playerID genderFolder isPetNum assetsBase =
(
    local exportFolder = ""
    if isPetNum == 0 do return ""
    if isPetNum <= 12 then
    (
        local partName = substring prefix 2 -1
        local partFolder = case partName of
        (
            "Hair": "01Hair"
            "Face": "02Face"
            "Top": "03Top"
            "Bottom": "04Bottom"
            "Shoe": "05Shoe"
            default: ""
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
    else if isPetNum == 20 then
    (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\12LeftBracelet\\LeftBracelet_" + playerID + "\\Model"
    )
    else if isPetNum == 21 then
    (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\13RightBracelet\\RightBracelet_" + playerID + "\\Model"
    )
    else if isPetNum == 22 then
    (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\14LeftAnklet\\LeftAnklet_" + playerID + "\\Model"
    )
    else if isPetNum == 23 then
    (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\15RightAnklet\\RightAnklet_" + playerID + "\\Model"
    )
    else if isPetNum == 24 then
    (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\17Earring\\Earring_" + playerID + "\\Model"
    )
    else if isPetNum == 25 then
    (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\19Mask\\Mask_" + playerID + "\\Model"
    )
    else if isPetNum == 26 then
    (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\07Glass\\Glass_" + playerID + "\\Model"
    )
    else if isPetNum == 27 then
    (
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\21LeftHandChain\\LeftHandChain_" + playerID + "\\Model"
    )
    else if isPetNum == 28 then
    (
        if genderFolder == "" do return ""
        exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\20RightHandRing\\RightHandRing_" + playerID + "\\Model"
    )
    exportFolder
)

-- 计算贴图导出目录（Textures 文件夹）
fn getTexFolder prefix playerID genderFolder isPetNum assetsBase =
(
    local texFolder = ""
    if isPetNum == 0 do return ""
    if isPetNum <= 12 then
    (
        local partName = substring prefix 2 -1
        texFolder = case partName of
        (
            "Hair": assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\01Hair\\Hair_" + playerID + "\\Textures"
            "Face": assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\02Face\\Face_" + playerID + "\\Textures"
            "Top": assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\03Top\\Top_" + playerID + "\\Textures"
            "Bottom": assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\04Bottom\\Bottom_" + playerID + "\\Textures"
            "Shoe": assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\05Shoe\\Shoe_" + playerID + "\\Textures"
            default: ""
        )
    )
    else if isPetNum == 15 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\06Head\\Head_" + playerID + "\\Textures"
    else if isPetNum == 16 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\08Wing\\Wing_" + playerID + "\\Textures"
    else if isPetNum == 17 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\09Hand\\Hand_" + playerID + "\\Textures"
    else if isPetNum == 18 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\11FlyDecoration\\FlyDecoration_" + playerID + "\\Textures"
    else if isPetNum == 19 then texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\18Tail\\Tail_" + playerID + "\\Textures"
    else if isPetNum == 20 then
    (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\12LeftBracelet\\LeftBracelet_" + playerID + "\\Textures"
    )
    else if isPetNum == 21 then
    (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\13RightBracelet\\RightBracelet_" + playerID + "\\Textures"
    )
    else if isPetNum == 22 then
    (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\14LeftAnklet\\LeftAnklet_" + playerID + "\\Textures"
    )
    else if isPetNum == 23 then
    (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\15RightAnklet\\RightAnklet_" + playerID + "\\Textures"
    )
    else if isPetNum == 24 then
    (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\17Earring\\Earring_" + playerID + "\\Textures"
    )
    else if isPetNum == 25 then
    (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\19Mask\\Mask_" + playerID + "\\Textures"
    )
    else if isPetNum == 26 then
    (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\07Glass\\Glass_" + playerID + "\\Textures"
    )
    else if isPetNum == 27 then
    (
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\21LeftHandChain\\LeftHandChain_" + playerID + "\\Textures"
    )
    else if isPetNum == 28 then
    (
        if genderFolder == "" do return ""
        texFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\20RightHandRing\\RightHandRing_" + playerID + "\\Textures"
    )
    texFolder
)

-- 脚本加载时自动尝试加载路径（如果失败会记录日志）
loadAssetsPath()