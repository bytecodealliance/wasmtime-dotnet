;; Component exercising heap-carrying values across the guest boundary: a string parameter,
;; a string result and a list result. Lifting these requires the core module to export a
;; memory and a realloc, and to return results indirectly through a return area.
;;
;; The allocator is a bump allocator that never frees, which is all these tests need.
(component
  (core module $m
    (memory (export "memory") 1)
    (global $bump (mut i32) (i32.const 1024))

    (func $alloc (param $n i32) (result i32)
      (local $r i32)
      (local.set $r (global.get $bump))
      (global.set $bump
        (i32.and
          (i32.add (i32.add (global.get $bump) (local.get $n)) (i32.const 7))
          (i32.const -8)))
      (local.get $r))

    (func (export "realloc") (param $old i32) (param $oldsz i32) (param $align i32) (param $newsz i32) (result i32)
      (call $alloc (local.get $newsz)))

    ;; greet(name) -> "hi " ++ name
    (func (export "greet") (param $ptr i32) (param $len i32) (result i32)
      (local $out i32)
      (local $ret i32)
      (local.set $out (call $alloc (i32.add (i32.const 3) (local.get $len))))
      (i32.store8 (local.get $out) (i32.const 104))
      (i32.store8 offset=1 (local.get $out) (i32.const 105))
      (i32.store8 offset=2 (local.get $out) (i32.const 32))
      (memory.copy
        (i32.add (local.get $out) (i32.const 3))
        (local.get $ptr)
        (local.get $len))
      (local.set $ret (call $alloc (i32.const 8)))
      (i32.store (local.get $ret) (local.get $out))
      (i32.store offset=4 (local.get $ret) (i32.add (i32.const 3) (local.get $len)))
      (local.get $ret))

    ;; length(name) -> byte length of the string as seen by the guest
    (func (export "length") (param $ptr i32) (param $len i32) (result i32)
      (local.get $len))

    ;; numbers() -> [10, 20, 30]
    (func (export "numbers") (result i32)
      (local $data i32)
      (local $ret i32)
      (local.set $data (call $alloc (i32.const 12)))
      (i32.store (local.get $data) (i32.const 10))
      (i32.store offset=4 (local.get $data) (i32.const 20))
      (i32.store offset=8 (local.get $data) (i32.const 30))
      (local.set $ret (call $alloc (i32.const 8)))
      (i32.store (local.get $ret) (local.get $data))
      (i32.store offset=4 (local.get $ret) (i32.const 3))
      (local.get $ret))
  )
  (core instance $i (instantiate $m))

  (func (export "greet") (param "name" string) (result string)
    (canon lift (core func $i "greet")
      (memory (core memory $i "memory"))
      (realloc (core func $i "realloc"))
      string-encoding=utf8))

  (func (export "length") (param "name" string) (result s32)
    (canon lift (core func $i "length")
      (memory (core memory $i "memory"))
      (realloc (core func $i "realloc"))
      string-encoding=utf8))

  (func (export "numbers") (result (list s32))
    (canon lift (core func $i "numbers")
      (memory (core memory $i "memory"))
      (realloc (core func $i "realloc"))))
)
