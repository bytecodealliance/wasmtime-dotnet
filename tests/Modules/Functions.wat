(module
 (import "env" "add" (func $env.add (param i32 i32) (result i32)))
 (import "env" "swap" (func $env.swap (param i32 i32) (result i32 i32)))
 (import "env" "do_throw" (func $env.do_throw))
 (import "env" "check_string" (func $env.check_string (param i32 i32)))
 (memory (export "mem") 1)
 (export "add" (func $add))
 (export "swap" (func $swap))
 (export "do_throw" (func $do_throw))
 (export "check_string" (func $check_string))
 (func $add (param i32 i32) (result i32)
  (call $env.add (local.get 0) (local.get 1))
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
 (data (i32.const 0) "Hello World")

 (func $echo_v128 (param v128) (result v128)
	local.get 0
 )
 (export "$echo_v128" (func $echo_v128))
)
