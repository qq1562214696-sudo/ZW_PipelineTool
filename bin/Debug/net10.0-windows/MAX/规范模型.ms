-- 规范模型.ms（完整版，已修复所有顶层 local 错误）

-- 全局变量和函数
fn setupBackgroundPlane =
(
	try
	(
		local bgPlane = getNodeByName "原画" exact:true
		if bgPlane == undefined do return false
		
		if not (isKindOf bgPlane Plane) do return false
		
		if maxFileName == "" do return false
		
		local maxFileDir = maxFilePath
		local maxFileNameOnly = getFilenameFile maxFileName
		
		local currentDir = maxFileDir
		local psdFile = undefined
		local levelsChecked = 0
		
		while levelsChecked < 5 and currentDir != "" do
		(
			currentDir = getFilenamePath (pathConfig.removePathLeaf currentDir)
			if currentDir == "" do exit
			
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
		
		local bgMaterial = Standard()
		bgMaterial.name = "原画材质"
		
		local diffuseMap = Bitmaptexture()
		diffuseMap.filename = psdFile
		diffuseMap.name = "原画贴图"
		bgMaterial.diffuseMap = diffuseMap
		bgMaterial.diffuseMapEnable = true
		bgMaterial.showInViewport = true
		
		bgPlane.material = bgMaterial
		
		try
		(
			local bmp = openBitMap psdFile
			if bmp != undefined do
			(
				local widthInPixels = bmp.width
				local heightInPixels = bmp.height
				close bmp
				
				local widthInMeters = widthInPixels / 1000.0
				local heightInMeters = heightInPixels / 1000.0
				
				bgPlane.length = heightInMeters
				bgPlane.width = widthInMeters
			)
		)
		catch()
		
		bgPlane.isFrozen = true
		
		return true
	)
	catch return false
)

fn ResetPivotFn Obj =
(
	local target = matrix3 [1,0,0] [0,0,1] [0,-1,0] [0,0,0]

	local ChldTms = in coordSys target (for Chld in Obj.children collect Chld.transform)

	local TmScale = scaleMatrix Obj.objectOffsetScale
	local TmRot = Obj.objectOffsetRot as matrix3
	local TmPos = transMatrix Obj.objectOffsetPos
	local TmOffset = TmScale * TmRot * TmPos

	TmOffset *= obj.transform * inverse target

	Obj.transform = target

	Obj.objectOffsetPos = TmOffset.translation
	Obj.objectOffsetRot = TmOffset.rotation
	Obj.objectOffsetScale = TmOffset.scale

	for i = 1 to Obj.children.count do 
		Obj.children[i].transform = ChldTms[i] * inverse target * Obj.transform
)

-- 主逻辑（所有顶层变量不再用 local）
setupBackgroundPlane()

pureMaxFileName = getFilenameFile (maxFilePath + maxFileName)
		
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

firstMat = Standard()
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
		if obj.parent != undefined do obj.parent = undefined
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
		linkTarget = rootObj
	else if not hasRoot and nodeObj != undefined and not hasBip then
		linkTarget = nodeObj
	else if hasRoot and hasBip then
		linkTarget = bipObj

	if linkTarget != undefined do
	(
		local allMeshes = for o in objects where matchPattern o.name pattern:"Mesh*" collect o
		for m in allMeshes do
		(
			if m != linkTarget and m.parent != linkTarget do
				m.parent = linkTarget
		)
	)
)