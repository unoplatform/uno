---
uid: Uno.Tutorials.ProfilingApplications
---

# Profiling Uno Platform Applications

## Profiling .NET Android/iOS applications

.NET provides the ability to do CPU profiling through [`dotnet-trace`](https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-trace) for iOS and Android applications.

### Pre-requisites

Run the following commands

- `dotnet tool update -g dotnet-dsrouter`
- `dotnet tool update -g dotnet-trace`
- `dotnet tool update -g dotnet-gcdump`

## Profiling .NET iOS applications

> [!NOTE]
> This documentation is based on [.NET iOS profiling](https://github.com/xamarin/xamarin-macios/wiki/Profiling) and [.NET Android profiling](https://github.com/dotnet/android/blob/main/Documentation/guides/tracing.md) documentation.

Profiling iOS apps needs to be done on a mac machine.

First, create an alias to `mlaunch`:

```bash
cd [your-folder-with-the-csproj]
alias mlaunch=$(dotnet build -getProperty:MlaunchPath *.csproj -f net10.0-ios)
```

### Profiling on an iOS Simulator

1. Build the app with the following parameters:

    ```dotnetcli
    cd [your-folder-with-the-csproj]
    dotnet build -f net10.0-ios -p:DiagnosticAddress=127.0.0.1 -p:DiagnosticPort=9000 -p:DiagnosticSuspend=true -p:DiagnosticListenMode=listen
    ```

1. Find the simulator you want to run on:

    ```dotnetcli
    $ xcrun simctl list devices
    ```

    Find a device that is shutdown or booted, and take note of its UDID.

1. Launch the app (it will be paused on startup waiting for the .NET tooling to connect):

    ```bash
    mlaunch --device :v2:udid=xxxxxxxx-yyyy-zzzz-aaaa-bbbbbbbbbbbb --wait-for-exit --stdout=$(tty) --stderr=$(tty) --launchsim=[your-app-path]/bin/Debug/net*-ios/*/*.app
    ```

    Replace the UDID with the one you found above.

1. Once the app is waiting, go ahead and start profiling:

    ```dotnetcli
    dotnet-trace collect --dsrouter ios-sim --format speedscope
    ```

1. Optionally take a GC dump:

    ```dotnetcli
    dotnet-gcdump collect --dsrouter ios-sim
    ```

### Profiling on a physical iOS device

1. Build the app with the following parameters:

    ```dotnetcli
    cd [your-folder-with-the-csproj]
    dotnet build -f net10.0-ios -p:DiagnosticAddress=127.0.0.1 -p:DiagnosticPort=9000 -p:DiagnosticSuspend=true -p:DiagnosticListenMode=listen
    ```

1. Install & launch the app:

    ```bash
    mlaunch --installdev bin/Debug/net*/*/*.app --devname ... 
    mlaunch --launchdev bin/Debug/net*/*/*.app --devname ... --wait-for-exit
    ```

1. Start CPU profiling:

    ```dotnetcli
    dotnet-trace collect --dsrouter ios --format speedscope
    ```

1. Optionally take a GC dump:

    ```dotnetcli
    dotnet-gcdump collect --dsrouter ios
    ```

## Profiling on Android

### Enable profiling in your application

In `Platforms/Android/environment.conf`, add **one** of the following lines:

- For devices:

    ```text
    DOTNET_DiagnosticPorts=127.0.0.1:9000,suspend,connect
    ```

- For emulators:

    ```text
    DOTNET_DiagnosticPorts=10.0.2.2:9000,suspend,connect
    ```

The `suspend` directive means that the application will wait for `dotnet-trace` connections before starting, `nosuspend` may also be used.

### Profiling the application

- Start the diagnostics router:

  - For devices, run `adb reverse tcp:9000 tcp:9001` then `dotnet-dsrouter android -v debug`

  - For emulators, run `dotnet-dsrouter android-emu -v debug`

- Run `dotnet-trace`, in the folder where you want your traces to be stored, using the **PID** provided by the `dotnet-dsrouter` output:

    ```dotnetcli
    dotnet-trace collect -p PID --format speedscope
    ```

- Start the `x64` emulator or the `arm64` device
    > Running on a 32 bits device is not supported and will generate unusable traces in SpeedScope

- Build the application with profiling enabled

    ```dotnetcli
    dotnet build -c Release -f net10.0-android -r android-arm64 -t:Run -p:AndroidEnableProfiler=true
    ```

  Use `-r android-x64` for emulators instead.

- The app will start and `dotnet-trace` will display a MB number counting up

- Use the app, once done, stop `dotnet-trace` by pressing `Enter` or `Ctrl+C`

- Open a browser at `https://speedscope.app` and drop the `*.speedscope.json` file in it

### Analyzing the trace data

This section provides insights into what to look for when analyzing flame charts.

- When building without AOT, a lot of the startup traces will show time spent in `System.Private.CoreLib!System.Runtime.CompilerServices.RuntimeHelpers.CompileMethod(object)`, indicating that that the JIT is doing a lot of work. This can make performance improvements harder to find.
- When building with AOT, most of the IL is compiled to native code with some exceptions. You may still find `RuntimeHelpers.CompileMethod` invocations. In such cases, you may need to find what is causing the AOT compiler to skip IL portions. If the JIT still impacts cold paths of your application, you may still need to adjust your code to avoid the JIT. For instance, some generics constructs force the AOT compiler to still use JITing. In other cases, it could be accessing static-type members. The JIT conditions are runtime version dependent, and [looking at the runtime code](https://github.com/dotnet/runtime/blob/9703660baa08914773b26e413e361c8ce04e6d94/src/mono/mono/mini/aot-compiler.c) can help to find out which ones.
- Some of the time is spent in the .NET Android binding framework (e.g. `Android.Runtime.JNIEnv` or `Java.Interop.TypeManager`), operations that cannot be adjusted by the application. One change to consider is to reduce the native code invocations to a strict minimum, where impactful.

## Analyzing GC memory dumps

You can analyze the GC memory `.gcdump` files using the [Visual Studio 2022/2026 memory profiler](https://learn.microsoft.com/en-us/visualstudio/profiling/memory-usage-without-debugging2?view=visualstudio&pivots=programming-language-dotnet#managed-types-reports) by using the File / Open menu and navigating the results.

## Profiling Skia Desktop applications

Profiling Skia-based Uno Platform targets can be done on Windows in Visual Studio 2019 and 2022 using [time and memory profilers](https://learn.microsoft.com/visualstudio/profiling/profiling-feature-tour?view=vs-2019).

## Profiling WebAssembly applications with runtime diagnostics

As of Dotnet 10.0, runtime diagnostics like performance traces and GC dumps can be collected by calling some Javascript methods exposed by the Dotnet runtime. For more details, see the [dotnet 10.0 release notes](https://github.com/dotnet/core/blob/main/release-notes/10.0/preview/preview4/aspnetcore.md#blazor-webassembly-runtime-diagnostics)

## Profiling WebAssembly applications with the browser's DevTools

Profiling WebAssembly applications can be done through the use of AOT compilation, and [browsers' performance tab](https://developer.chrome.com/docs/devtools/evaluate-performance/).

### Setup the WebAssembly application for profiling

- Enable emcc profiling:

    ```xml
    <PropertyGroup>
        <WasmShellEnableEmccProfiling>true</WasmShellEnableEmccProfiling>
    </PropertyGroup>
    ```

- Enable AOT compilation:

    ```xml
    <PropertyGroup>
        <WasmShellMonoRuntimeExecutionMode>InterpreterAndAOT</WasmShellMonoRuntimeExecutionMode>
    </PropertyGroup>
    ```

- Build and deploy the application
- Open the `Performance` tab in your browser
- Use your application or restart your application while recording the trace

### Troubleshooting

- Deep traces found in large async code patterns or complex UI trees may hit [this chromium issue](https://bugs.chromium.org/p/chromium/issues/detail?id=1206709). This generally makes traces very long to load; you'll need to be patient.
