-- 面数检查.ms（完整版，包含所有依赖）

-- 预设的面数限制表：根据文件名前缀确定限制
local limitTable = #(
	#("Head",              1000),
	#("Wing",              2000),
	#("Hand",              1500),
	#("Tail",              1000),
	#("Mask",               100),
	#("Glass",              400),
	#("FlyDecoration",     2000),
	#("FloatDecoration",   2000),
	#("Earring",            300),
	#("LeftBracelet",       400),
	#("RightBracelet",      400),
	#("RightHandRing",      400),
	#("LeftHandChain",      500),
	#("LightRing",         2000)
)

-- 主逻辑（原 btnCheckFaceCount）
-- 从当前max文件名中提取前缀
local fileName = getFilenameFile maxFileName
local nameParts = filterString fileName "_"
local prefix = ""
local prefixNum = 0

if nameParts.count >= 1 then
(
	-- 获取前缀，可能是"H"、"L"、"HHair"等
	prefix = nameParts[1]
	
	-- 如果是单字符前缀(H或L)，需要结合第二个部分
	if prefix == "H" or prefix == "L" and nameParts.count >= 2 then
	(
		prefix = nameParts[2] -- 比如Hair、Face、Top等
	)
	
	-- 检查是否在限制表中
	prefixNum = findItem (for item in limitTable collect item[1]) prefix
)

-- 收集所有以 "Mesh" 开头的对象（不区分大小写）
local meshObjects = for o in objects where matchPattern o.name pattern:"Mesh*" ignoreCase:true collect o

if meshObjects.count == 0 do
(
	messageBox "场景中没有找到任何名称以 'Mesh' 开头的对象！" title:"提示"
	return()
)

local totalTris     = 0
local msgDetails    = ""
local objCount      = meshObjects.count

-- 遍历所有 Mesh 对象，计算总面数
for obj in meshObjects do
(
	local triCount = 0
	
	-- 正确的 if-else 语法
	if isKindOf obj.baseObject Editable_Poly then
	(
		triCount = polyOp.getNumFaces obj.baseObject
	)
	else if isKindOf obj.baseObject Editable_Mesh then
	(
		triCount = meshOp.getNumFaces obj.baseObject
	)
	else if canConvertTo obj Editable_Poly then
	(
		-- 快照方式兼容 Skin、Morph 等修改器
		local snap = snapshotAsMesh obj
		triCount = getNumFaces snap
		delete snap
	)
	else
	(
		continue  -- 不支持的面数统计，跳过
	)
	
	totalTris += triCount
	
	-- 累加详细信息
	msgDetails += obj.name + "：" + (triCount as string) + " 面\n"
)

-- 格式化总面数（加千位分隔符）
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
else
(
	formattedTotal = strTotal
)

-- 确定道具类型和面数限制
local modelType = "未知类型"
local faceLimit = 0
local isOverLimit = false

if prefixNum > 0 then
(
	modelType = limitTable[prefixNum][1]
	faceLimit = limitTable[prefixNum][2]
	isOverLimit = totalTris > faceLimit
)

local finalMsg = ""

finalMsg += "道具类型：" + modelType + "\n"
finalMsg += "Mesh数量：" + (objCount as string) + " 个\n"
finalMsg += "总三角面数：" + formattedTotal + "\n"

if prefixNum > 0 then
(
	finalMsg += "面数上限：" + (faceLimit as string) + "\n\n"
)
else
(
	finalMsg += "面数上限：未知（未匹配到类型）\n\n"
)

-- 超过限制的醒目提示
if isOverLimit then
(
	finalMsg += "（超出 " + ((totalTris - faceLimit) as string) + " 面）\n\n"
)

-- 详细统计
finalMsg += "---------\n"
finalMsg += msgDetails

-- 添加额外提示
if prefixNum == 0 then
(
	finalMsg += "\n提示：未识别到道具类型，请检查max文件名格式"
)

-- 显示结果
messageBox finalMsg title:"面数检查完成" beep:isOverLimit