-- 规范模型.ms（完整版，包含所有依赖）

-- 全局变量和函数（从原脚本复制，确保独立运行）
fn setupBackgroundPlane =
(
	try
	(
		-- 1. 查找名为"原画"的对象
		local bgPlane = getNodeByName "原画" exact:true
		if bgPlane == undefined do return false
		
		-- 检查对象是否为平面类型
		if not (isKindOf bgPlane Plane) do return false
		
		-- 2. 获取max文件所在目录并向上查找PSD文件
		if maxFileName == "" do return false
		
		local maxFileDir = maxFilePath
		local maxFileNameOnly = getFilenameFile maxFileName
		
		-- 向上查找最多5级父目录
		local currentDir = maxFileDir
		local psdFile = undefined
		local levelsChecked = 0
		
		while levelsChecked < 5 and currentDir != "" do
		(
			-- 获取父级目录
			currentDir = getFilenamePath (pathConfig.removePathLeaf currentDir)
			if currentDir == "" do exit
			
			-- 在父级目录中查找包含max文件名的PSD文件
			local psdFiles = getFiles (currentDir + "\\*.psd")
			
			for file in psdFiles do
			(
				local fileNameOnly = filenameFromPath file
				if findString fileNameOnly maxFileNameOnly != undefined do
				(
					psdFile = file
					exit
				)
			)
			
			if psdFile != undefined do exit
			levelsChecked += 1
		)
		
		if psdFile == undefined do return false
		
		-- 3. 创建材质并应用贴图
		local bgMaterial = Standard()
		bgMaterial.name = "原画材质"
		
		local diffuseMap = Bitmaptexture()
		diffuseMap.filename = psdFile
		diffuseMap.name = "原画贴图"
		bgMaterial.diffuseMap = diffuseMap
		bgMaterial.diffuseMapEnable = true
		bgMaterial.showInViewport = true
		
		-- 应用材质到原画对象
		bgPlane.material = bgMaterial
		
		-- 获取PSD图片尺寸并调整平面大小
		try
		(
			local bmp = openBitMap psdFile
			if bmp != undefined do
			(
				local widthInPixels = bmp.width
				local heightInPixels = bmp.height
				close bmp
				
				-- 计算尺寸（像素/1000 米）
				local widthInMeters = widthInPixels / 1000.0
				local heightInMeters = heightInPixels / 1000.0
				
				-- 调整平面大小
				bgPlane.length = heightInMeters
				bgPlane.width = widthInMeters
			)
		)
		catch
		(
			-- 读取尺寸失败，不做调整
		)
		
		-- 冻结原画对象
		bgPlane.isFrozen = true
		
		return true
	)
	catch
	(
		return false
	)
)

fn ResetPivotFn Obj =
(
	--目标矩阵
	local target = matrix3 [1,0,0] [0,0,1] [0,-1,0] [0,0,0]

	--保存子物体的transforms矩阵
	local ChldTms = in coordSys target ( for Chld in Obj.children collect Chld.transform )

	--当前物体的transform矩阵
	local TmScale = scaleMatrix Obj.objectOffsetScale
	local TmRot = Obj.objectOffsetRot as matrix3
	local TmPos = transMatrix Obj.objectOffsetPos
	local TmOffset = TmScale * TmRot * TmPos

	--新的偏移transform矩阵
	TmOffset *= obj.transform * inverse target

	--应用矩阵
	Obj.transform = target

	--重置偏移
	Obj.objectOffsetPos = TmOffset.translation
	Obj.objectOffsetRot = TmOffset.rotation
	Obj.objectOffsetScale = TmOffset.scale

	--重制子物体的transform
	for i = 1 to Obj.children.count do Obj.children[i].transform = ChldTms[i] * inverse target * Obj.transform
)

-- 主逻辑（原 standardizeBtn）
setupBackgroundPlane()

local pureMaxFileName = getFilenameFile (maxFilePath + maxFileName)
		
if matchPattern pureMaxFileName pattern:"*_fin" ignoreCase:true do
(
	pureMaxFileName = substring pureMaxFileName 1 (pureMaxFileName.count - 4)
)

if maxFileName == "" do
(
	messageBox "请先保存当前max文件！"
	return()
)

for i in 1 to 24 do
(
	meditMaterials[i] = Standard()
)

local firstMat = Standard()
firstMat.name = pureMaxFileName + "_D"
firstMat.showInViewport = true

local diffuseMapPath = maxFilePath + pureMaxFileName + "_D.png"
local alphaMapPath = maxFilePath + pureMaxFileName + "_A.png"

if doesFileExist diffuseMapPath then
(
	local diffuseMap = Bitmaptexture()
	diffuseMap.filename = filenameFromPath diffuseMapPath
	diffuseMap.name = pureMaxFileName + "_D"
	firstMat.diffuseMap = diffuseMap
	firstMat.diffuseMapEnable = true
)
else
(
	messageBox ("未找到漫反射贴图: " + pureMaxFileName + "_D.png" + "\n请确保贴图文件与max文件在同一目录下。")
)

if doesFileExist alphaMapPath then
(
	local alphaMap = Bitmaptexture()
	alphaMap.filename = filenameFromPath alphaMapPath
	alphaMap.name = pureMaxFileName + "_A"
	firstMat.opacityMap = alphaMap
	firstMat.opacityMapEnable = true
)

meditMaterials[1] = firstMat

undo on
(
	local meshCount = 0
	for obj in objects where matchPattern obj.name pattern:"Mesh*" ignoreCase:true do
	(
		try
		(
			obj.material = firstMat
			meshCount += 1
		)
		catch()
	)

	for obj in objects where matchPattern obj.name pattern:"Mesh*" ignoreCase:true do
	(
		if obj.parent != undefined do
		(
			obj.parent = undefined
		)
	)

	local allPolyObjects = for obj in objects where classOf obj == Editable_Poly collect obj
	
	for obj in allPolyObjects do
	(
		polyop.setMapSupport obj 0 false
		polyop.setMapSupport obj 2 false
		polyop.setMapSupport obj 3 false
	)
	
	if allPolyObjects.count > 0 do
	(
		local oldSelection = selection as array
		
		select allPolyObjects
		
		macros.run "Animation Tools" "DeleteSelectedAnimation"
		
		select oldSelection
	)
	
	for obj in allPolyObjects do
	(
		ResetPivotFn obj
		obj.scale = [1.0,1.0,1.0]
	)

	local hasRoot  = false
	local hasBip   = false
	local rootObj  = undefined
	local bipObj   = undefined
	local nodeObj  = undefined

	for o in objects do
	(
		local n = o.name
		if matchPattern n pattern:"Root*"  do (hasRoot = true;  if rootObj  == undefined do rootObj  = o)
		if matchPattern n pattern:"Bip*"   do (hasBip  = true;  if bipObj   == undefined do bipObj   = o)
		if matchPattern n pattern:"Node*"  and nodeObj == undefined do nodeObj = o
	)

	local linkTarget = undefined

	if hasRoot and not hasBip and nodeObj == undefined then
	(
		linkTarget = rootObj
	)
	else if not hasRoot and nodeObj != undefined and not hasBip then
	(
		linkTarget = nodeObj
	)
	else if hasRoot and hasBip then
	(
		linkTarget = bipObj
	)
	else
	(
		-- 其他情况，不链接
	)

	if linkTarget != undefined do
	(
		local allMeshes = for o in objects where matchPattern o.name pattern:"Mesh*" collect o
		for m in allMeshes do
		(
			if m != linkTarget and m.parent != linkTarget do
			(
				m.parent = linkTarget
			)
		)
	)
)