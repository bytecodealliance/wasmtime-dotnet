(module
  (type $t0 (func))
  (import "" "store_data" (func $.store_data (type $t0)))
  (func $run
    call $.store_data
  )
  (export "run" (func $run))
)
