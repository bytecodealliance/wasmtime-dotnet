(module
 (import "env" "add" (func $env.add (param i32 i32) (result i32)))
 (import "env" "add_reflection" (func $env.add_reflection (param i32 i32) (result i32)))
 (import "env" "swap" (func $env.swap (param i32 i32) (result i32 i32)))
 (import "env" "do_throw" (func $env.do_throw))
 (import "env" "check_string" (func $env.check_string (param i32 i32)))
 (import "env" "return_i32" (func $env.return_i32 (result i32)))
 (import "env" "return_15_values" (func $env.return_15_values (result i32 i32 i32 i32 i32 i32 i32 i32 i32 i32 i32 i32 i32 i32 i32)))
 (import "env" "accept_15_values" (func $env.accept_15_values (param i32 i32 i32 i32 i32 i32 i32 i32 i32 i32 i32 i32 i32 i32 i32)))
 (memory (export "mem") 1)
 (export "add" (func $add))
 (export "add_reflection" (func $add_reflection))
 (export "swap" (func $swap))
 (export "do_throw" (func $do_throw))
 (export "check_string" (func $check_string))
 (export "return_i32" (func $env.return_i32))
 (export "get_and_pass_15_values" (func $get_and_pass_15_values))
 (func $add (param i32 i32) (result i32)
  (call $env.add (local.get 0) (local.get 1))
 )
  (func $add_reflection (param i32 i32) (result i32)
  (call $env.add_reflection (local.get 0) (local.get 1))
 )
 (func $swap (param i32 i32) (result i32 i32)
  (call $env.swap (local.get 0) (local.get 1))
 )
 (func $do_throw
  (call $env.do_throw)
 )
 (func $check_string
  (call $env.check_string (i32.const 0) (i32.const 11))
 )
 (func $get_and_pass_15_values
	call $env.return_15_values
	call $env.accept_15_values
 )
 (data (i32.const 0) "Hello World")

 (func (export "noop"))
 (func $echo_i32 (param i32) (result i32) local.get 0)
 (export "$echo_i32" (func $echo_i32))
 (func $echo_i64 (param i64) (result i64) local.get 0)
 (export "$echo_i64" (func $echo_i64))
 (func $echo_f32 (param f32) (result f32) local.get 0)
 (export "$echo_f32" (func $echo_f32))
 (func $echo_f64 (param f64) (result f64) local.get 0)
 (export "$echo_f64" (func $echo_f64))
 (func $echo_v128 (param v128) (result v128) local.get 0)
 (export "$echo_v128" (func $echo_v128))
 (func $echo_funcref (param funcref) (result funcref) local.get 0)
 (export "$echo_funcref" (func $echo_funcref))
 (func $echo_externref (param externref) (result externref) local.get 0)
 (export "$echo_externref" (func $echo_externref))

 (func $echo_tuple2 (result i32 i32) i32.const 1 i32.const 2)
 (export "$echo_tuple2" (func $echo_tuple2))
 (func $echo_tuple3 (result i32 i32 i32) i32.const 1 i32.const 2 i32.const 3)
 (export "$echo_tuple3" (func $echo_tuple3))
 (func $echo_tuple4 (result i32 i32 i32 f32) i32.const 1 i32.const 2 i32.const 3 f32.const 3.141)
 (export "$echo_tuple4" (func $echo_tuple4))
 (func $echo_tuple5 (result i32 i32 i32 f32 f64) i32.const 1 i32.const 2 i32.const 3 f32.const 3.141 f64.const 2.71828)
 (export "$echo_tuple5" (func $echo_tuple5))
 (func $echo_tuple6 (result i32 i32 i32 f32 f64 i32) i32.const 1 i32.const 2 i32.const 3 f32.const 3.141 f64.const 2.71828 i32.const 6)
 (export "$echo_tuple6" (func $echo_tuple6))
 (func $echo_tuple7 (result i32 i32 i32 f32 f64 i32 i32) i32.const 1 i32.const 2 i32.const 3 f32.const 3.141 f64.const 2.71828 i32.const 6 i32.const 7)
 (export "$echo_tuple7" (func $echo_tuple7))
 (func $echo_tuple8 (result i32 i32 i32 f32 f64 i32 i32 i32) i32.const 1 i32.const 2 i32.const 3 f32.const 3.141 f64.const 2.71828 i32.const 6 i32.const 7 i32.const 8)
 (export "$echo_tuple8" (func $echo_tuple8))
)
