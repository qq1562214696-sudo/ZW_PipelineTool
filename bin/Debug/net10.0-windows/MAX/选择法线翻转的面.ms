-- 选择法线翻转的面.ms（完整版，无额外依赖）

-- 主逻辑（原 selFilpNormalFace）
if (selection.count == 0 or selection.count > 1) do
(
	messagebox "请选择一个mesh物体"
	return()
)

local obj = selection[1]

if (iskindof obj PolyMeshObject or iskindof obj Editable_Poly) then
(
	local n_face = #() as array
	local cp = snapshot obj
	local nf = #{1..cp.numFaces}
	local g = for i in nf collect getFaceNormal cp i
	meshop.unifyNormals cp nf
	local x = for i in nf collect getFaceNormal cp i
	for i in nf do
	(
		if g[i] != x[i] do append n_face i
	)
	setFaceSelection cp n_face
	update cp
	convertToPoly cp
	local thefaces = polyop.getFaceSelection cp
	delete cp
	select obj
	max modify mode
	modPanel.setCurrentObject obj.baseObject
	subObjectLevel = 4
	polyop.setFaceSelection obj thefaces 
	update obj
)
else
(
	messagebox "请选择Editable Poly类型的模型"
)