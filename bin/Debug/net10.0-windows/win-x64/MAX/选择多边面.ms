(
	-- 检测对象是否有五边及以上面，返回 (是否找到, 面索引数组)
	fn processObject obj =
	(
		-- 检查对象是否可转换为Editable_Poly
		local canProcess = (classOf obj.baseObject == Editable_Poly) or (canConvertTo obj Editable_Poly)
		if not canProcess then return #(false, #())
		
		if classOf obj.baseObject == Editable_Poly then
		(
			-- 已是Editable_Poly，直接检测
			local numFaces = polyOp.getNumFaces obj
			local n_face = for f = 1 to numFaces where (polyOp.getFaceVerts obj f).count > 4 collect f
			if n_face.count > 0 then return #(true, n_face)
			else return #(false, #())
		)
		else
		(
			-- 临时转换检测
			local tmpObj = snapshot obj
			convertToPoly tmpObj
			local numFaces = polyOp.getNumFaces tmpObj
			local n_face = for f = 1 to numFaces where (polyOp.getFaceVerts tmpObj f).count > 4 collect f
			delete tmpObj
			if n_face.count > 0 then return #(true, n_face)
			else return #(false, #())
		)
	)
	
	fn main =
	(
		-- 情况1：选中单个对象且可处理
		if selection.count == 1 then
		(
			local obj = selection[1]
			local canProcess = (classOf obj.baseObject == Editable_Poly) or (canConvertTo obj Editable_Poly)
			if canProcess then
			(
				local result = processObject obj
				if result[1] then
				(
					-- 确保对象是Editable_Poly（用于后续选择）
					if classOf obj.baseObject != Editable_Poly then
					(
						convertToPoly obj
					)
					max modify mode
					modPanel.setCurrentObject obj
					subObjectLevel = 4
					polyOp.setFaceSelection obj result[2]
					update obj
				)
				else
				(
					messagebox "当前对象没有五边及以上面" title:"提示"
				)
				return()
			)
			-- 若选中对象不可处理，继续遍历
		)
		
		-- 情况2：遍历场景中所有可处理对象
		local found = false
		local foundObj = undefined
		local foundFaces = #()
		
		for obj in objects do
		(
			local canProcess = (classOf obj.baseObject == Editable_Poly) or (canConvertTo obj Editable_Poly)
			if canProcess then
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
		)
		
		if found then
		(
			-- 确保对象是Editable_Poly
			if classOf foundObj.baseObject != Editable_Poly then
			(
				convertToPoly foundObj
			)
			select foundObj
			max modify mode
			modPanel.setCurrentObject foundObj
			subObjectLevel = 4
			polyOp.setFaceSelection foundObj foundFaces
			update foundObj
		)
		else
		(
			messagebox "场景中没有找到包含五边及以上面的对象" title:"提示"
		)
	)
	
	main()
)