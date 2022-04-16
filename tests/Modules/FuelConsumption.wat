(module
  (import "env" "expensive" (func $env.expensive))
  (import "env" "free" (func $env.free))
  (func $expensive
    call $env.expensive
  )
  (func $free
    call $env.free
  )
  (export "expensive" (func $expensive))
  (export "free" (func $free))
)
