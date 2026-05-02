(component
  (core module $m
    (memory (export "memory") 1)
    (data (i32.const 0) "Hello, world!")
    (func (export "cabi_realloc") (param i32 i32 i32 i32) (result i32)
      i32.const 64)
    (func (export "hello") (result i32)
      ;; Write {ptr=0, len=13} into the return area at offset 64.
      (i32.store (i32.const 64) (i32.const 0))
      (i32.store (i32.const 68) (i32.const 13))
      (i32.const 64)))
  (core instance $i (instantiate $m))
  (func (export "hello") (result string)
    (canon lift
      (core func $i "hello")
      (memory $i "memory")
      (realloc (func $i "cabi_realloc"))
      string-encoding=utf8)))
