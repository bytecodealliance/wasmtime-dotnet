(module
  (import "" "table" (table $t 4 funcref))
  (func (export "call_indirect") (param i32 i32 i32) (result i32)
    (call_indirect $t (param i32 i32) (result i32) (local.get 1) (local.get 2) (local.get 0))
  )
)
