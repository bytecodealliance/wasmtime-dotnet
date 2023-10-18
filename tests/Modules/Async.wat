(module
  (type $t0 (func))

  (import "" "no_args" (func $.no_args (type $t0)))
  (import "" "one_arg" (func $.one_arg (param i32)))

  (func $call_no_args
    call $.no_args
  )
  (export "call_no_args" (func $call_no_args))

  (func $call_one_arg (param i32)
    (call $.one_arg (local.get 0))
  )
  (export "call_one_arg" (func $call_one_arg))
)
