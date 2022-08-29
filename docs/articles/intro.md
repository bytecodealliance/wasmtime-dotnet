# Introduction to .NET embedding of Wasmtime

[Wasmtime](https://github.com/bytecodealliance/wasmtime) is a standalone runtime capable of executing [WebAssembly](https://webassembly.org/) outside of a web browser.

The [.NET embedding of Wasmtime](https://github.com/bytecodealliance/wasmtime-dotnet) enables .NET developers to easily instantiate and execute WebAssembly modules using Wasmtime.

For this tutorial, we will create a WebAssembly module and use that WebAssembly module from a .NET 5 application.

# A simple WebAssembly module

One of the reasons why WebAssembly is so exciting is that [many languages are able to target WebAssembly](https://github.com/appcypher/awesome-wasm-langs).  This means, for example, a plugin model based on WebAssembly could enable developers to write sandboxed, cross-platform plugins in any number of languages.

For this introduction, however, we will use a  WebAssembly module in [WebAssembly Text Format](https://developer.mozilla.org/en-US/docs/WebAssembly/Understanding_the_text_format):

```text
(module
  (func $hello (import "" "hello"))
  (func (export "run") (call $hello))
)
```

This module simply imports a `hello` function from the host and exports a `run` function that calls the imported function.

# Using the WebAssembly module from .NET

## Installing a .NET 5 SDK

Install a [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0) for your platform if you haven't already.

This will add a `dotnet` command to your PATH.

## Creating the .NET project

The .NET program will be a simple console application, so create a new console project with `dotnet new`:

```text
mkdir tutorial
cd tutorial
dotnet new console
```

## Referencing the Wasmtime package

To use the .NET embedding of Wasmtime from the project, we need to add a reference to the [Wasmtime NuGet package](https://www.nuget.org/packages/Wasmtime):

```text
dotnet add package --version 0.40.0-preview1 wasmtime
```

_Note that the `--version` option is required because the package is currently prerelease._

This will add a `PackageReference` to the project file so that .NET embedding for Wasmtime can be used.

## Implementing the .NET code

Replace the contents of `Program.cs` with the following:

```c#
using System;
using Wasmtime;

namespace Tutorial
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new Engine();

            using var module = Module.FromText(
                engine,
                "hello",
                "(module (func $hello (import \"\" \"hello\")) (func (export \"run\") (call $hello)))"
            );

            using var linker = new Linker(engine);
            using var store = new Store(engine);

            linker.Define(
                "",
                "hello",
                Function.FromCallback(store, () => Console.WriteLine("Hello from C#!"))
            );

            var instance = linker.Instantiate(store, module);
            var run = instance.GetAction(store, "run");
            run();
        }
    }
}
```

The [`Linker`](https://bytecodealliance.github.io/wasmtime-dotnet/api/Wasmtime.Linker.html) class is responsible for linking in those defined functions, such as `hello` in this example.

Here we are defining a function named `hello` that simply prints `Hello from C#!` when called from WebAssembly.

A WebAssembly module _instantiation_ is the stateful representation of a module that can be executed.

This code is calling the `run` function defined in WebAssembly that is exported by the instance; this function then calls the `hello` function defined in C#.

## Building the .NET application

Use `dotnet build` to build the .NET application:

```text
dotnet build
```

This will create a `tutorial` (or `tutorial.exe` on Windows) executable in the `bin/Debug/net5.0` directory that implements the .NET application.

## Running the .NET application
 
To run the .NET application, simply invoke the executable file or use `dotnet`:

```text
dotnet run
```

This should result in the following output:

```text
Hello from C#!
```
