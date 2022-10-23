(module
  (memory (export "mem") 0x10000)
  (func $start
	i32.const 0xFFFFFFFF
	i32.const 99
	i32.store8
	i32.const 0xFFFFFFFE
	i32.const 100
	i32.store8
	i32.const 0xFFFFFFFD
	i32.const 101
	i32.store8
  )
  (start $start)
)
