(module
  (export "run" (func $run))
  (func $run
    (call $first)
  )
  (func $first
    (call $second)
  )
  (func $second
    (call $third)
  )
  (func $third
    unreachable
  )
)
