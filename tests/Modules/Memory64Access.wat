(module
  (memory (export "mem") i64 0x10001 0x1000000000000)
  (func $start
	i64.const 0x10000FFFF
	i32.const 99
	i32.store8
	i64.const 0x10000FFFE
	i32.const 100
	i32.store8
	i64.const 0x10000FFFD
	i32.const 101
	i32.store8
  )
  (start $start)
)
