(module
  (import ""
    (instance $host
      (export "print" (func (param i32 i32)))
      (export "inst" (instance (export "write" (func (param i32) (result i32)))))
      (export "mem" (memory 1))
    )
  )
  (export "mem" (memory $host "mem"))
  (func (export "run") (local i32)
    i32.const 6
    call (func $host "inst" "write")
    i32.const 6
    i32.add
    local.tee 0
    i32.const 33 ;;; !
    i32.store align=1
    i32.const 0
    local.get 0
    i32.const 1
    i32.add
    call (func $host "print")
  )
  (data (i32.const 0) "Hello ")
)
