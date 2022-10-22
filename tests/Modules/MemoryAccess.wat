(module
  (memory (export "mem") 65536)
  (func $start
	i32.const 4294967295
	i32.const 99
	i32.store8
	i32.const 4294967294
	i32.const 100
	i32.store8
	i32.const 4294967293
	i32.const 101
	i32.store8
  )
  (start $start)
)
