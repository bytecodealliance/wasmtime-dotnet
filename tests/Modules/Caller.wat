(module
  (import "env" "callback" (func $callback))

  (memory (export "memory") 1 1)

  (func (export "call_callback")
    (call $callback)
  )
  (func (export "add") (param $lhs i32) (param $rhs i32) (result i32)
    local.get $lhs
    local.get $rhs
    i32.add
  )
)