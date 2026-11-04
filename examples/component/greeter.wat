;; A component that combines a caller-provided string with a host-provided name.
;;
;;   import name: func() -> string
;;   export greet: func(string) -> string
;;
;; The core module needs an exported memory and realloc so that strings can cross
;; the component boundary.
(component
  (import "host" (instance $h
    (export "name" (func (result string)))))
  (alias export $h "name" (func $name))

  (core module $libc
    (memory (export "memory") 1)
    (global $bump (mut i32) (i32.const 1024))
    (func (export "realloc") (param $old i32) (param $oldsz i32) (param $align i32) (param $n i32) (result i32)
      (local $r i32)
      (local.set $r (global.get $bump))
      (global.set $bump
        (i32.and (i32.add (i32.add (global.get $bump) (local.get $n)) (i32.const 7)) (i32.const -8)))
      (local.get $r))
  )
  (core instance $libc_i (instantiate $libc))

  (core func $name_core
    (canon lower (func $name)
      (memory (core memory $libc_i "memory"))
      (realloc (core func $libc_i "realloc"))
      string-encoding=utf8))

  (core module $m
    (import "libc" "memory" (memory 1))
    (import "libc" "realloc" (func $realloc (param i32 i32 i32 i32) (result i32)))
    (import "host" "name" (func $name (param i32)))

    (func (export "greet") (param $ptr i32) (param $len i32) (result i32)
      (local $host_result i32)
      (local $host_ptr i32)
      (local $host_len i32)
      (local $out i32)
      (local $suffix i32)
      (local $ret i32)

      ;; Ask the host for its name, then build "hello, " ++ name ++ " from " ++ host name.
      (local.set $host_result (call $realloc (i32.const 0) (i32.const 0) (i32.const 4) (i32.const 8)))
      (call $name (local.get $host_result))
      (local.set $host_ptr (i32.load (local.get $host_result)))
      (local.set $host_len (i32.load offset=4 (local.get $host_result)))

      (local.set $out
        (call $realloc (i32.const 0) (i32.const 0) (i32.const 1)
          (i32.add (i32.const 13) (i32.add (local.get $len) (local.get $host_len)))))
      (i32.store8 (local.get $out) (i32.const 104))
      (i32.store8 offset=1 (local.get $out) (i32.const 101))
      (i32.store8 offset=2 (local.get $out) (i32.const 108))
      (i32.store8 offset=3 (local.get $out) (i32.const 108))
      (i32.store8 offset=4 (local.get $out) (i32.const 111))
      (i32.store8 offset=5 (local.get $out) (i32.const 44))
      (i32.store8 offset=6 (local.get $out) (i32.const 32))
      (memory.copy (i32.add (local.get $out) (i32.const 7)) (local.get $ptr) (local.get $len))

      (local.set $suffix (i32.add (i32.add (local.get $out) (i32.const 7)) (local.get $len)))
      (i32.store8 (local.get $suffix) (i32.const 32))
      (i32.store8 offset=1 (local.get $suffix) (i32.const 102))
      (i32.store8 offset=2 (local.get $suffix) (i32.const 114))
      (i32.store8 offset=3 (local.get $suffix) (i32.const 111))
      (i32.store8 offset=4 (local.get $suffix) (i32.const 109))
      (i32.store8 offset=5 (local.get $suffix) (i32.const 32))
      (memory.copy (i32.add (local.get $suffix) (i32.const 6)) (local.get $host_ptr) (local.get $host_len))

      (local.set $ret (call $realloc (i32.const 0) (i32.const 0) (i32.const 4) (i32.const 8)))
      (i32.store (local.get $ret) (local.get $out))
      (i32.store offset=4 (local.get $ret)
        (i32.add (i32.const 13) (i32.add (local.get $len) (local.get $host_len))))
      (local.get $ret))
  )
  (core instance $i (instantiate $m
    (with "libc" (instance $libc_i))
    (with "host" (instance (export "name" (func $name_core))))))

  (func (export "greet") (param "name" string) (result string)
    (canon lift (core func $i "greet")
      (memory (core memory $libc_i "memory"))
      (realloc (core func $libc_i "realloc"))
      string-encoding=utf8))
)
