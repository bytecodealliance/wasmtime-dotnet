#[allow(warnings)]
mod bindings;

use bindings::{Greeting, Guest, Permissions, Point, Priority};

struct Component;

impl Guest for Component {
    fn origin() -> Point {
        Point { x: 3, y: 4 }
    }

    fn range() -> Vec<u32> {
        vec![10, 20, 30]
    }

    fn top_priority() -> Priority {
        Priority::High
    }

    fn defaults() -> Permissions {
        Permissions::READ | Permissions::WRITE
    }

    fn greet(formal: bool) -> Greeting {
        if formal {
            Greeting::Formal("Sir".into())
        } else {
            Greeting::Casual("hi".into())
        }
    }

    fn safe_divide(n: u32, d: u32) -> Result<u32, String> {
        if d == 0 {
            Err("division by zero".into())
        } else {
            Ok(n / d)
        }
    }

    fn find(needle: u32) -> Option<String> {
        if needle == 42 {
            Some("answer".into())
        } else {
            None
        }
    }

    fn pair() -> (u32, String) {
        (7, "seven".into())
    }

    fn square(n: u32) -> u32 {
        n * n
    }

    fn translate(p: Point, dx: u32, dy: u32) -> Point {
        Point {
            x: p.x + dx,
            y: p.y + dy,
        }
    }
}

bindings::export!(Component with_types_in bindings);
