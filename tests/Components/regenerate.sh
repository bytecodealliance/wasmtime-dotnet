#!/usr/bin/env bash
#
# Rebuilds the wasm component fixtures used by the test suite.
#
# - tests/Components/fixtures-src/  (componentize-dotnet) -> fixtures.wasm
# - tests/Components/fixtures.wit  + wasm-tools             -> fixtures.wit.json
# - tests/Components/host-add.wat  + wasm-tools             -> host-add.wasm
# - tests/Components/add.wat       + wasm-tools             -> add.wasm
# - tests/Components/hello-string.wat + wasm-tools          -> hello-string.wasm
#
# Requirements:
#   - docker (for arm64 Linux container that builds the .NET fixture; macOS
#     hosts cannot run NativeAOT-LLVM directly)
#   - nix shell support, or wasm-tools available in PATH
#
# Run from the repository root, e.g.:
#   ./tests/Components/regenerate.sh

set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
WASM_TOOLS=(wasm-tools)
if ! command -v wasm-tools >/dev/null 2>&1; then
    if command -v nix >/dev/null 2>&1; then
        WASM_TOOLS=(nix shell nixpkgs#wasm-tools --command wasm-tools)
    else
        echo "wasm-tools not found; install it or run inside nix shell." >&2
        exit 1
    fi
fi

if ! command -v docker >/dev/null 2>&1; then
    echo "docker is required to build the .NET fixture (NativeAOT-LLVM has no macOS prebuilts)" >&2
    exit 1
fi

echo "==> Compiling primitive WAT fixtures"
"${WASM_TOOLS[@]}" parse "$ROOT/add.wat" --output "$ROOT/add.wasm"
"${WASM_TOOLS[@]}" parse "$ROOT/hello-string.wat" --output "$ROOT/hello-string.wasm"
"${WASM_TOOLS[@]}" parse "$ROOT/host-add.wat" --output "$ROOT/host-add.wasm"

echo "==> Building the .NET component fixture in arm64 Linux container"
docker run --rm --platform linux/arm64 \
    -v "$ROOT/fixtures-src:/work" \
    -w /work \
    mcr.microsoft.com/dotnet/sdk:10.0 \
    dotnet build --configuration Release \
        --property:WasiSdkUrl=https://github.com/WebAssembly/wasi-sdk/releases/download/wasi-sdk-24/wasi-sdk-24.0-arm64-linux.tar.gz

cp "$ROOT/fixtures-src/bin/Release/net10.0/wasi-wasm/publish/fixtures.wasm" "$ROOT/fixtures.wasm"

echo "==> Generating WIT JSON IR for the source generator"
"${WASM_TOOLS[@]}" component wit "$ROOT/fixtures.wit" --json > "$ROOT/fixtures.wit.json"

echo "==> Done. Re-run dotnet test to verify."
