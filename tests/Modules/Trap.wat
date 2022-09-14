(module
  (export "run" (func $run))
  (export "run_stack_overflow" (func $run_stack_overflow))
  (export "run_stack_overflow_with_result" (func $run_stack_overflow_with_result))

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

  (func $run_stack_overflow_with_result (result i32)
    (call $run_stack_overflow_with_result)
  )

  (func $run_stack_overflow
    (call $run_stack_overflow)
  )
)
