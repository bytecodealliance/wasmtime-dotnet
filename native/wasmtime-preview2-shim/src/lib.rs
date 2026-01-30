use anyhow::{bail, Context, Result};
use std::ffi::{CStr, CString};
use std::ops::Range;
use std::os::raw::{c_char, c_int};
use wasmtime::{Engine, Store};
use wasmtime::component::{Component, Linker, ResourceTable};
use wasmtime_wasi::{DirPerms, FilePerms};
use wasmtime_wasi::p2::{IoView, WasiCtx, WasiCtxBuilder, WasiView};
use wasmtime_wasi_http::{WasiHttpCtx, WasiHttpView};
use wasmparser::{Parser, Payload};

#[repr(C)]
pub struct Preview2Result {
    pub exit_code: c_int,
    pub error_message: *mut c_char,
}

struct Preview2State {
    table: ResourceTable,
    wasi: WasiCtx,
    http: WasiHttpCtx,
}

impl IoView for Preview2State {
    fn table(&mut self) -> &mut ResourceTable {
        &mut self.table
    }
}

impl WasiView for Preview2State {
    fn ctx(&mut self) -> &mut WasiCtx {
        &mut self.wasi
    }
}

impl WasiHttpView for Preview2State {
    fn ctx(&mut self) -> &mut WasiHttpCtx {
        &mut self.http
    }
}

#[no_mangle]
pub unsafe extern "C" fn wasmtime_preview2_run_component(
    component_path: *const c_char,
    argv: *const *const c_char,
    argc: usize,
    env: *const *const c_char,
    envc: usize,
    preopens: *const *const c_char,
    preopen_count: usize,
    inherit_stdio: bool,
    inherit_env: bool,
    inherit_network: bool,
    exit_code_out: *mut c_int,
    error_message_out: *mut *mut c_char,
) -> c_int {
    let result = std::panic::catch_unwind(|| {
        run_component_inner(
            component_path,
            argv,
            argc,
            env,
            envc,
            preopens,
            preopen_count,
            inherit_stdio,
            inherit_env,
            inherit_network,
        )
    });

    match result {
        Ok(Ok(exit_code)) => {
            if !exit_code_out.is_null() {
                unsafe { *exit_code_out = exit_code };
            }
            if !error_message_out.is_null() {
                unsafe { *error_message_out = std::ptr::null_mut() };
            }
            0
        }
        Ok(Err(err)) => {
            write_error(err, error_message_out);
            1
        }
        Err(_) => {
            write_error(anyhow::anyhow!("panic while executing component"), error_message_out);
            2
        }
    }
}

#[no_mangle]
pub unsafe extern "C" fn wasmtime_preview2_free_error(ptr: *mut c_char) {
    if ptr.is_null() {
        return;
    }
    unsafe {
        let _ = CString::from_raw(ptr);
    }
}

#[no_mangle]
pub unsafe extern "C" fn wasmtime_preview2_extract_core_module(
    component_path: *const c_char,
    output_path: *const c_char,
    error_message_out: *mut *mut c_char,
) -> c_int {
    let result = std::panic::catch_unwind(|| {
        extract_core_module_inner(component_path, output_path)
    });

    match result {
        Ok(Ok(())) => {
            if !error_message_out.is_null() {
                unsafe { *error_message_out = std::ptr::null_mut() };
            }
            0
        }
        Ok(Err(err)) => {
            write_error(err, error_message_out);
            1
        }
        Err(_) => {
            write_error(anyhow::anyhow!("panic while extracting core module"), error_message_out);
            2
        }
    }
}

fn write_error(err: anyhow::Error, error_message_out: *mut *mut c_char) {
    if error_message_out.is_null() {
        return;
    }
    let msg = format!("{err:#}");
    match CString::new(msg) {
        Ok(cstr) => unsafe {
            *error_message_out = cstr.into_raw();
        },
        Err(_) => unsafe {
            *error_message_out = std::ptr::null_mut();
        },
    }
}

unsafe fn run_component_inner(
    component_path: *const c_char,
    argv: *const *const c_char,
    argc: usize,
    env: *const *const c_char,
    envc: usize,
    preopens: *const *const c_char,
    preopen_count: usize,
    inherit_stdio: bool,
    inherit_env: bool,
    inherit_network: bool,
) -> Result<c_int> {
    if component_path.is_null() {
        bail!("component path is null");
    }

    let component_path = unsafe { CStr::from_ptr(component_path) }
        .to_str()
        .context("component path is not valid UTF-8")?;

    let args = unsafe { read_cstr_array(argv, argc)? };
    let envs = unsafe { read_cstr_array(env, envc)? };
    let preopen_specs = unsafe { read_cstr_array(preopens, preopen_count)? };

    let mut builder = WasiCtxBuilder::new();
    if inherit_stdio {
        builder.inherit_stdio();
    }
    if inherit_env {
        builder.inherit_env();
    }
    if inherit_network {
        builder.inherit_network();
    }

    if !args.is_empty() {
        builder.args(&args);
    }

    for env in envs {
        let (key, value) = split_env(&env);
        builder.env(key, value);
    }

    for spec in preopen_specs {
        let (host, guest) = split_preopen(&spec)?;
        builder.preopened_dir(host, guest, DirPerms::all(), FilePerms::all())?;
    }

    let engine = Engine::default();
    let component = Component::from_file(&engine, component_path)
        .with_context(|| format!("failed to load component: {component_path}"))?;

    let mut linker = Linker::<Preview2State>::new(&engine);
    wasmtime_wasi::p2::add_to_linker_sync(&mut linker)
        .context("failed to add WASI preview2 to linker")?;
    wasmtime_wasi_http::add_only_http_to_linker_sync(&mut linker)
        .context("failed to add WASI HTTP to linker")?;

    let state = Preview2State {
        table: ResourceTable::new(),
        wasi: builder.build(),
        http: WasiHttpCtx::new(),
    };

    let mut store = Store::new(&engine, state);

    let command = wasmtime_wasi::p2::bindings::sync::Command::instantiate(&mut store, &component, &linker)
        .context("failed to instantiate component")?;

    let result = command
        .wasi_cli_run()
        .call_run(&mut store)
        .context("failed to call wasi:cli/run")?;

    Ok(match result {
        Ok(()) => 0,
        Err(()) => 1,
    })
}

unsafe fn extract_core_module_inner(
    component_path: *const c_char,
    output_path: *const c_char,
) -> Result<()> {
    if component_path.is_null() {
        bail!("component path is null");
    }
    if output_path.is_null() {
        bail!("output path is null");
    }

    let component_path = unsafe { CStr::from_ptr(component_path) }
        .to_str()
        .context("component path is not valid UTF-8")?;
    let output_path = unsafe { CStr::from_ptr(output_path) }
        .to_str()
        .context("output path is not valid UTF-8")?;

    let bytes = std::fs::read(component_path)
        .with_context(|| format!("failed to read component: {component_path}"))?;

    let mut ranges = Vec::new();
    collect_module_ranges(&bytes, 0, &mut ranges)
        .context("failed to parse component for core modules")?;

    let range = ranges
        .into_iter()
        .max_by_key(|r| r.end.saturating_sub(r.start))
        .ok_or_else(|| anyhow::anyhow!("no core modules found in component"))?;

    if range.end > bytes.len() || range.start >= range.end {
        bail!("core module range is out of bounds");
    }

    std::fs::write(output_path, &bytes[range])
        .with_context(|| format!("failed to write core module to {output_path}"))?;

    Ok(())
}

fn collect_module_ranges(
    bytes: &[u8],
    base_offset: usize,
    ranges: &mut Vec<Range<usize>>,
) -> Result<()> {
    for payload in Parser::new(0).parse_all(bytes) {
        match payload.context("failed to parse component payload")? {
            Payload::ModuleSection { unchecked_range, .. } => {
                let start = base_offset + unchecked_range.start;
                let end = base_offset + unchecked_range.end;
                if end > base_offset + bytes.len() {
                    bail!("module range is out of bounds");
                }
                ranges.push(start..end);
            }
            Payload::ComponentSection { unchecked_range, .. } => {
                let start = unchecked_range.start;
                let end = unchecked_range.end;
                if end > bytes.len() || start >= end {
                    bail!("nested component range is out of bounds");
                }
                collect_module_ranges(&bytes[start..end], base_offset + start, ranges)?;
            }
            _ => {}
        }
    }

    Ok(())
}

unsafe fn read_cstr_array(ptr: *const *const c_char, len: usize) -> Result<Vec<String>> {
    if len == 0 {
        return Ok(Vec::new());
    }
    if ptr.is_null() {
        bail!("string array pointer is null but length is non-zero");
    }

    let mut values = Vec::with_capacity(len);
    for i in 0..len {
        let item = unsafe { *ptr.add(i) };
        if item.is_null() {
            bail!("string array entry {i} is null");
        }
        let value = unsafe { CStr::from_ptr(item) }
            .to_str()
            .context("string array entry is not valid UTF-8")?;
        values.push(value.to_owned());
    }
    Ok(values)
}

fn split_env(env: &str) -> (&str, &str) {
    match env.split_once('=') {
        Some((k, v)) => (k, v),
        None => (env, ""),
    }
}

fn split_preopen(spec: &str) -> Result<(&str, &str)> {
    if let Some((host, guest)) = spec.split_once("::") {
        if host.is_empty() || guest.is_empty() {
            bail!("invalid preopen mapping '{spec}'");
        }
        return Ok((host, guest));
    }
    if let Some((host, guest)) = spec.split_once('=') {
        if host.is_empty() || guest.is_empty() {
            bail!("invalid preopen mapping '{spec}'");
        }
        return Ok((host, guest));
    }
    if spec.is_empty() {
        bail!("invalid preopen mapping '{spec}'");
    }
    Ok((spec, spec))
}
