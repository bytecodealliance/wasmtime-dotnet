(module
  (import "" "inout" (func $.inout (param externref) (result externref)))
  (func (export "inout") (param externref) (result externref)
    local.get 0
    call $.inout
  )
  (func (export "nullref") (result externref)
    ref.null extern
    call $.inout
  )
)