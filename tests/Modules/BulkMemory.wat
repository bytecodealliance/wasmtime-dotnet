(module
  (func (param $dst i32) (param $src i32) (param $size i32) (result i32)
	  local.get $dst
	  local.get $src
	  local.get $size
	  memory.copy
	  local.get $dst
  )
)
