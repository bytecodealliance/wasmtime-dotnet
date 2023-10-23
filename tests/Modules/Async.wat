(module
  (type $t0 (func))

  (import "" "no_args" (func $.no_args (type $t0)))
  (import "" "one_arg" (func $.one_arg (param i32)))
  (import "" "no_args_one_result" (func $.no_args_one_result (result i32)))
  (import "" "two_args_one_result" (func $.two_args_one_result (param f32 f32) (result f32)))
  (import "" "many_args_many_results" (func $.many_args_many_results (param f32 f32 i32 f64) (result f32 i32 f32 f64)))

  (func $call_no_args
    call $.no_args
  )
  (export "call_no_args" (func $call_no_args))

  (func $call_one_arg (param i32)
    (call $.one_arg (local.get 0))
  )
  (export "call_one_arg" (func $call_one_arg))

  (func $call_no_args_one_result (result i32)
    (call $.no_args_one_result)
  )
  (export "call_no_args_one_result" (func $call_no_args_one_result))

  (func $call_two_args_one_result (param f32 f32) (result f32)
    (call $.two_args_one_result (local.get 0) (local.get 1))
  )
  (export "call_two_args_one_result" (func $call_two_args_one_result))

  (func $call_many_args_many_results (param f32 f32 i32 f64) (result f32 i32 f32 f64)
    (call $.many_args_many_results (local.get 0) (local.get 1) (local.get 2) (local.get 3))
  )
  (export "call_many_args_many_results" (func $call_many_args_many_results))
)
