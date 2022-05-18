(module
 (type $i32_=>_i32 (func (param i32) (result i32)))
 (import "env" "getInt" (func $assembly/env/getInt (param i32) (result i32)))
 (memory $0 1 1)
 (export "testFunction" (func $assembly/index/testFunction))
 (export "shiftLeft" (func $assembly/index/shiftLeft))
 (export "memory" (memory $0))
 (func $assembly/index/testFunction (param $0 i32) (result i32)
  local.get $0
  call $assembly/env/getInt
 )
 (func $assembly/index/shiftLeft (param $0 i32) (result i32)
  local.get $0
  i32.const 1
  i32.shl
 )
)