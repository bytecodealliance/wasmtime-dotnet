(module
  (import "" "mem" (memory 1))
  (func $name (export "write") (param i32) (result i32)
    local.get 0
    i32.const 119 ;;; w
    i32.store offset=0 align=1
    local.get 0
    i32.const 111 ;;; o
    i32.store offset=1 align=1
    local.get 0
    i32.const 114 ;;; r
    i32.store offset=2 align=1
    local.get 0
    i32.const 108 ;;; l
    i32.store offset=3 align=1
    local.get 0
    i32.const 100 ;;; d
    i32.store offset=4 align=1
    i32.const 5
  )
)
