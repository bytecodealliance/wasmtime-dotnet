(module
  (import "" "trap_from_host_exception" (func $trap_from_host_exception))
  (import "" "call_host_callback" (func $call_host_callback))
  (export "ok" (func $ok))
  (export "ok_value" (func $ok_value))
  (export "run" (func $run))
  (export "run_div_zero" (func $run_div_zero))
  (export "run_div_zero_with_result" (func $run_div_zero_with_result))
  (export "trap_from_host_exception" (func $trap_from_host_exception))
  (export "call_host_callback" (func $call_host_callback))
  (export "trap_in_wasm" (func $third))
  (start $start)

  (func $start
    (call $call_host_callback)
  )

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
