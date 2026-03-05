(
	-- 检测对象是否有翻转法线面，返回 (是否找到, 面索引数组)
	fn processObject obj =
	(
		if not (iskindof obj PolyMeshObject or iskindof obj Editable_Poly) then return #(false, #())
		
		local n_face = #()
		local cp = snapshot obj
		local nf = #{1..cp.numFaces}
		local g = for i in nf collect getFaceNormal cp i
		meshop.unifyNormals cp nf
		local x = for i in nf collect getFaceNormal cp i
		for i in nf do
		(
			if g[i] != x[i] do append n_face i
		)
		delete cp
		if n_face.count > 0 then
			return #(true, n_face)
		else
			return #(false, #())
	)
	
	fn main =
	(
		-- 情况1：选中单个对象且为可处理类型
		if selection.count == 1 then
		(
			local obj = selection[1]
			if (iskindof obj PolyMeshObject or iskindof obj Editable_Poly) then
			(
				local result = processObject obj
				if result[1] then
				(
					select obj
					max modify mode
					modPanel.setCurrentObject obj.baseObject
					subObjectLevel = 4
					polyop.setFaceSelection obj result[2]
					update obj
				)
				else
				(
					messagebox "当前对象没有法线翻转的面" title:"提示"
				)
				return()
			)
			-- 若选中对象不可处理，则继续遍历（不返回）
		)
		
		-- 情况2：遍历场景中所有可处理对象
		local found = false
		local foundObj = undefined
		local foundFaces = #()
		
		for obj in objects where (iskindof obj PolyMeshObject or iskindof obj Editable_Poly) do
		(
			local result = processObject obj
			if result[1] then
			(
				found = true
				foundObj = obj
				foundFaces = result[2]
				exit
			)
		)
		
		if found then
		(
			select foundObj
			max modify mode
			modPanel.setCurrentObject foundObj.baseObject
			subObjectLevel = 4
			polyop.setFaceSelection foundObj foundFaces
			update foundObj
		)
		else
		(
			messagebox "场景中没有找到包含法线翻转面的对象" title:"提示"
		)
	)
	
	main()
)