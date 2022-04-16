(module
  (import "" "expensive" (func $.expensive))
  (func $expensive
    call $.expensive
  )
  (export "expensive" (func $expensive))
)
