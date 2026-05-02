;; Component that imports `host:math/add: func(a: u32, b: u32) -> u32` from
;; the host and re-exports it as `compute` so a host can verify the
;; round-trip through wasmtime's lowering/lifting machinery.
(component
  (import "host-add" (func $host-add (param "a" u32) (param "b" u32) (result u32)))

  (core func $core-add (canon lower (func $host-add)))

  (core module $m
    (func (import "imports" "host-add") (param i32 i32) (result i32))
    (func (export "compute") (param i32 i32) (result i32)
      local.get 0
      local.get 1
      call 0))

  (core instance $imports (export "host-add" (func $core-add)))
  (core instance $i (instantiate $m (with "imports" (instance $imports))))

  (func $compute (param "a" u32) (param "b" u32) (result u32)
    (canon lift (core func $i "compute")))
  (export "compute" (func $compute)))
