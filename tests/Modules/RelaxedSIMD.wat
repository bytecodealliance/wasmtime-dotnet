(module
  (func $madd_v128 (param v128) (result v128)
	local.get 0
	local.get 0
	local.get 0
	f32x4.relaxed_madd
  )
  (export "$madd_v128" (func $madd_v128))
)
