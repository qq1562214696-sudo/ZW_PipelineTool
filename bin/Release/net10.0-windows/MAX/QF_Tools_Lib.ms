-- ================== QF_Tools 共享库 ==================
prefixArray = #("H","L","HHair","HFace","HTop","HBottom","HShoe","LHair","LFace","LTop","LBottom","LShoe","HPet","LPet",\
"Head","Wing","Hand","FlyDecoration","Tail","LeftBracelet","RightBracelet","LeftAnklet","RightAnklet","Earring","Mask","Glass",\
"LeftHandChain","RightHandRing")

-- 【下面所有 fn 直接从你贴的 <DOCUMENT> 里复制粘贴】
-- 从 "-- 新增：原画设置函数" 开始一直到 "fn ResetPivotFn Obj = (...)" 结束，全部复制进来
-- （包括 setupBackgroundPlane、copyToClipboard、copyMaxFileNameToClipboard、showListenerWindow、safeFormatToListener、detectGender、getExportFolder、getTexFolder、ResetPivotFn）

-- 新增：独立读取 Assets 路径（不再依赖 rollout）
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