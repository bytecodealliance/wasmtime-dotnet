(module
  (import "" "callback" (func $callback (param funcref) (result externref)))
  (import "" "return_funcref" (func $return_funcref (result funcref)))
  (import "" "assert" (func $assert (param externref) (result externref)))
  (import "" "store_funcref" (func $store_funcref (param funcref)))
  (export "return_funcref" (func $return_funcref))
  (table $t 1 funcref)
  (elem declare func $f)
  (elem declare func $assert)
  (func (export "call_nested") (param funcref funcref) (result externref)
    (table.set $t (i32.const 0) (local.get 0))
    (call_indirect $t (param funcref) (result externref) (local.get 1) (i32.const 0))
  )
  (func $f (param externref) (result externref)
    (call $assert (local.get 0))
  )
  (func (export "call_callback") (result externref)
    (call $callback (ref.func $f))
  )
  (func (export "call_with_null") (result externref)
    (call $callback (ref.null func))
  )
  (func (export "call_store_funcref")
    (call $store_funcref (ref.func $assert))
  )
)