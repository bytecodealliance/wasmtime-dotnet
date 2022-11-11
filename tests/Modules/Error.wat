(module
  (import "wasi_snapshot_preview1" "proc_exit" (func $exit (param i32)))
  (import "" "error_from_host_exception" (func $error_from_host_exception))
  (import "" "call_host_callback" (func $call_host_callback))
  (export "error_from_host_exception" (func $error_from_host_exception))
  (export "call_host_callback" (func $call_host_callback))
  (export "error_in_wasm" (func $error_in_wasm))
  (start $start)

  (func $start
    (call $call_host_callback)
  )
  (func $error_in_wasm
    i32.const 0
    call $exit
  )
)
