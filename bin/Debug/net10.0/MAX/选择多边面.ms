-- 选择多边面.ms（完整版，无额外依赖）

-- 主逻辑（原 selMultiEdgeFace）
if selection.count != 1 do
(
	messageBox "请只选择一个物体！" title:"提示"
	return()
)

local obj = selection[1]
if classOf obj.baseObject != Editable_Poly do
(
	if canConvertTo obj Editable_Poly then
	(
		convertToPoly obj
	)
	else
	(
		messageBox "请选择可转换为 Editable Poly 的物体！" title:"不支持"
		return()
	)
)

polyOp.unhideAllFaces obj

max modify mode
modPanel.setCurrentObject obj
subObjectLevel = 4

local n_face = for f = 1 to (polyOp.getNumFaces obj) where (polyOp.getFaceVerts obj f).count > 4 collect f

polyOp.setFaceSelection obj n_face
update obj

if n_face.count == 0 do messageBox "没有五边及以上面" title:"结果"