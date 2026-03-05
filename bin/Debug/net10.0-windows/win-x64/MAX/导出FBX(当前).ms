-- 导出FBX(当前).ms（完整版，包含所有依赖）
(
local thisDir = getFilenamePath (getThisScriptFilename())
fileIn (thisDir + "\\QF_Tools_Lib.ms")

local assetsFolder = loadAssetsPath()
if assetsFolder == "" or (doesFileExist assetsFolder) == false do
(
	messageBox "请先在 QF工具精简版 中【浏览】设置 Unity Assets 目录！" title:"路径错误"
	return()
)

-- 全局变量和函数
prefixArray = #("H","L","HHair","HFace","HTop","HBottom","HShoe","LHair","LFace","LTop","LBottom","LShoe","HPet","LPet",\
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
		actionMan.executeAction 0 "40472"  -- 侦听器窗口ID
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

fn getExportFolder prefix playerID genderFolder isPetNum assetsBase =
(
	local exportFolder = ""
	
	if isPetNum == 0 then
	(
		return ""
	)
	else if isPetNum <= 12 then
	(
		-- 普通角色部件 (H, L, HHair, LHair等)
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
	(
		-- 宠物
		exportFolder = assetsBase + "\\ArtWork\\Pet\\Main\\Pet_" + playerID + "\\Model"
	)
	else if isPetNum == 15 then
	(
		-- Head（不分性别）
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\06Head\\Head_" + playerID + "\\Model"
	)
	else if isPetNum == 16 then
	(
		-- Wing（不分性别）
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\08Wing\\Wing_" + playerID + "\\Model"
	)
	else if isPetNum == 17 then
	(
		-- Hand（不分性别）
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\09Hand\\Hand_" + playerID + "\\Model"
	)
	else if isPetNum == 18 then
	(
		-- FlyDecoration（不分性别）
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\11FlyDecoration\\FlyDecoration_" + playerID + "\\Model"
	)
	else if isPetNum == 19 then
	(
		-- Tail（不分性别）
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\18Tail\\Tail_" + playerID + "\\Model"
	)
	else if isPetNum == 20 then
	(
		-- LeftBracelet（分性别）
		if genderFolder == "" then return "" else
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\12LeftBracelet\\LeftBracelet_" + playerID + "\\Model"
	)
	else if isPetNum == 21 then
	(
		-- RightBracelet（分性别）
		if genderFolder == "" then return "" else
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\13RightBracelet\\RightBracelet_" + playerID + "\\Model"
	)
	else if isPetNum == 22 then
	(
		-- LeftAnklet（分性别）
		if genderFolder == "" then return "" else
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\14LeftAnklet\\LeftAnklet_" + playerID + "\\Model"
	)
	else if isPetNum == 23 then
	(
		-- RightAnklet（分性别）
		if genderFolder == "" then return "" else
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\15RightAnklet\\RightAnklet_" + playerID + "\\Model"
	)
	else if isPetNum == 24 then
	(
		-- Earring（分性别）
		if genderFolder == "" then return "" else
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\17Earring\\Earring_" + playerID + "\\Model"
	)
	else if isPetNum == 25 then
	(
		-- Mask（分性别）
		if genderFolder == "" then return "" else
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\19Mask\\Mask_" + playerID + "\\Model"
	)
	else if isPetNum == 26 then
	(
		-- Glass（分性别）
		if genderFolder == "" then return "" else
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\07Glass\\Glass_" + playerID + "\\Model"
	)
	else if isPetNum == 27 then
	(
		-- LeftHandChain（不分性别）
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Hang\\21LeftHandChain\\LeftHandChain_" + playerID + "\\Model"
	)
	else if isPetNum == 28 then
	(
		-- RightHandRing（分性别）
		if genderFolder == "" then return "" else
		exportFolder = assetsBase + "\\ArtWork\\Avatar\\Character\\" + genderFolder + "\\20RightHandRing\\RightHandRing_" + playerID + "\\Model"
	)
	
	return exportFolder
)

-- 主逻辑（原 exportFbx）
if (doesFileExist assetsFolder) == false do
(
	Messagebox "请在地址栏指定正确的Unity工程Assets目录"
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

local expLog = "导出FBX结果：\n"

max unhide all

local exportObjs = #()
for obj in objects do
(
	local objName = obj.name
	if matchPattern objName pattern:"Bip*" or
	matchPattern objName pattern:"Mesh*" or
	matchPattern objName pattern:"Root*" or
	matchPattern objName pattern:"Node*" or
	matchPattern objName pattern:"Bone*" do
	(
		append exportObjs obj
	)
)

if exportObjs.count == 0 do
(
	messageBox "没有找到任何名称以 Bip、Mesh、Root、Bone 开头的对象！\n导出将无法继续。" title:"警告"
	return()
)

selectionSets["Export"] = exportObjs

local objNameArray = #()
if selectionSets["Export"] != undefined do
(
	for obj in selectionSets["Export"] do append objNameArray obj.name
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

if selectedCount == 0 do
(
	expLog += "错误：选择集中没有找到任何有效对象\n"
	return()
)

local fileFullName = maxFilePath + maxFileName
local fileName = getFilenameFile fileFullName
local nameArray = filterString fileName "_"
local prefix = nameArray[1]
local playerID = nameArray[2]

local isPetNum = findItem prefixArray prefix
local genderFolder = detectGender()
local fbxFileName = prefix + "_" + playerID

if isPetNum == 0 then
(
	expLog += "错误：未知的前缀 " + prefix + "\n"
	return()
)

local exportFolder = getExportFolder prefix playerID genderFolder isPetNum assetsFolder

if exportFolder == "" do
(
	expLog += "错误：无法确定导出路径（可能缺少性别标识）\n"
	return()
)

if (doesFileExist exportFolder) == false do makeDir exportFolder

local fbxPath = exportFolder + "\\" + fbxFileName + ".fbx"
local success = exportFile fbxPath #noPrompt selectedOnly:true using:FBXEXP

if success == true then
(
	expLog += "FBX导出成功: " + fbxFileName + ".fbx\n"
)
else
(
	expLog += "FBX导出失败：导出 " + fbxFileName + ".fbx 失败\n"
)

if selectionSets["Export"] != undefined do
(
	select selectionSets["Export"]
)

-- 新增：导出结束后复制max文件名到剪切板
copyMaxFileNameToClipboard()

-- 修改：使用新的安全格式化函数
if expLog != "导出FBX结果：\n" then
(
	safeFormatToListener expLog
)
else
(
	messageBox "当前文件 FBX 导出完成，无错误记录。" title:"完成"
)
)