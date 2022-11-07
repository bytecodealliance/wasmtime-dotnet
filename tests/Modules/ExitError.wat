(module
  (import "wasi_snapshot_preview1" "proc_exit" (func $exit (param i32)))
  (memory (export "memory") 1)
  (func (export "exit") (param i32)
    local.get 0
    call $exit)
)
        
