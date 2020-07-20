(module
  (import "" "concat" (func $.concat (param externref externref) (result externref)))
  (func (export "run") (param externref externref) (result externref)
    local.get 0
    local.get 1
    call $.concat
  )
)