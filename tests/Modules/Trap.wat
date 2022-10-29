(module
  (import "" "host_trap" (func $host_trap))
  (export "ok" (func $ok))
  (export "ok_value" (func $ok_value))
  (export "run" (func $run))
  (export "run_div_zero" (func $run_div_zero))
  (export "run_div_zero_with_result" (func $run_div_zero_with_result))
  (export "host_trap" (func $host_trap))

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

  (func $run_div_zero_with_result (result i32)
    (i32.const 1)
    (i32.const 0)
    (i32.div_s)
  )

  (func $run_div_zero
    (call $run_div_zero_with_result)
    (drop)
  )

  (func $ok)
  (func $ok_value (result i32)
    (i32.const 1)
  )
)
