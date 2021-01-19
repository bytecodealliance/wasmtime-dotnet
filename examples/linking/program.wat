(module
  (import "" "print" (func $print (param i32 i32)))
  (import "" "inst" (instance $inst (export "write" (func $write (param i32) (result i32)))))
  (import "" "mem" (memory 1))
  (export "mem" (memory 0))
  (func (export "run") (local i32)
    i32.const 6
    call $inst.$write
    i32.const 6
    i32.add
    local.tee 0
    i32.const 33 ;;; !
    i32.store align=1
    i32.const 0
    local.get 0
    i32.const 1
    i32.add
    call $print
  )
  (data (i32.const 0) "Hello ")
)
