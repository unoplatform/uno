---
uid: Uno.Tutorials.ProfilingApplications
---

# Profiling Uno Platform Applications

## Profiling .NET Android/iOS applications

.NET 7 and later provides the ability to do CPU profiling through [`dotnet-trace`](https://learn.microsoft.com/dotnet/core/diagnostics/dotnet-trace) for Android applications.

### Pre-requisites

Run the following commands

- `dotnet tool update -g dotnet-dsrouter`
- `dotnet tool update -g dotnet-trace`
- `dotnet tool update -g dotnet-gcdump`

## Profiling .NET iOS applications

> [!NOTE]
> This documentation is based on [.NET iOS profiling documentation](https://github.com/xamarin/xamarin-macios/wiki/Profiling).

Profiling iOS apps needs to be done on a mac machine.

First, create an alias to mlaunch:

```bash
alias mlaunch=/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mlaunch
```

### Profiling on an iOS Simulator

1. The first step is to launch the tool that provides a connection between the app and the .NET tracing tools:

    ```dotnetcli
    dotnet-dsrouter client-server -ipcc ~/my-sim-port -tcps 127.0.0.1:9000
    ```

2. Launch the app and make it suspend upon launch (waiting for the .NET tooling to connect):

    ```bash
    mlaunch --launchsim bin/Debug/net*/*/*.app --device :v2:runtime=com.apple.CoreSimulator.SimRuntime.iOS-15-4,devicetype=com.CoreSimulator.SimDeviceType.iPhone-11 --wait-for-exit --stdout=$(tty) --stderr=$(tty) --argument --connection-mode --argument none '--setenv:DOTNET_DiagnosticPorts=127.0.0.1:9000,suspend'
    ```

3. At this point it's necessary to wait until the following line shows up in the terminal:

    ```console
    The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command from a Diagnostic Port
    ```

4. Once that's printed, go ahead and start profiling:

    ```dotnetcli
    dotnet-trace collect --diagnostic-port ~/my-sim-port --format speedscope
    ```

To find which device to use, use:

```bash
xcrun simctl list devices
```

Then reference the UDID of the simulator in the mlaunch command:

```bash
mlaunch ... --device :v2:udid=50BCC90D-7E56-4AFB-89C5-3688BF345998 ...
```

### Profiling on a physical iOS device

Launch the tool that bridges the app and the .NET tracing tools:

```bash
dotnet-dsrouter server-client -ipcs ~/my-dev-port -tcpc 127.0.0.1:9001 --forward-port iOS
```

Install & launch the app and make it suspended upon launch:

```bash
mlaunch --installdev bin/Debug/net*/*/*.app --devname ... 
mlaunch --launchdev bin/Debug/net*/*/*.app --devname ... --wait-for-exit --argument --connection-mode --argument none '--setenv:DOTNET_DiagnosticPorts=127.0.0.1:9001,suspend,listen'
```

At this point, it's necessary to wait until the following line shows up in the terminal:

```console
The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command from a Diagnostic Port
```

Once that's printed, go ahead and start profiling:

```bash
dotnet-trace collect --diagnostic-port ~/my-dev-port,connect --format speedscope
```

## Profiling .NET Android applications (.NET 8)

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
    dotnet build -c Release -f net9.0-android -r android-arm64 -t:Run -p:AndroidEnableProfiler=true
    ```

  Use `-r android-x64` for emulators instead.

- The app will start and `dotnet-trace` will display a MB number counting up

- Use the app, once done, stop `dotnet-trace` by pressing `Enter` or `Ctrl+C`

- Open a browser at `https://speedscope.app` and drop the `*.speedscope.json` file in it

### Getting GC memory dumps

To take a GC memory dump of a running android app, follow the same steps above, but instead of `dotnet-trace collect -p <port>`, use `dotnet-gcdump collect -p <port>`. It will create a `.gcdump` file that can be viewed in Visual Studio and Perfview on Windows and [heapview](https://github.com/1hub/dotnet-heapview) on non-Windows platforms.

See complete [documentation](https://github.com/dotnet/android/blob/main/Documentation/guides/tracing.md) for more details.

### Analyzing the trace data

This section provides insights into what to look for when analyzing flame charts.

- When building without AOT, a lot of the startup traces will show time spent in `System.Private.CoreLib!System.Runtime.CompilerServices.RuntimeHelpers.CompileMethod(object)`, indicating that that the JIT is doing a lot of work. This can make performance improvements harder to find.
- When building with AOT, most of the IL is compiled to native code with some exceptions. You may still find `RuntimeHelpers.CompileMethod` invocations. In such cases, you may need to find what is causing the AOT compiler to skip IL portions. If the JIT still impacts cold paths of your application, you may still need to adjust your code to avoid the JIT. For instance, some generics constructs force the AOT compiler to still use JITing. In other cases, it could be accessing static-type members. The JIT conditions are runtime version dependent, and [looking at the runtime code](https://github.com/dotnet/runtime/blob/9703660baa08914773b26e413e361c8ce04e6d94/src/mono/mono/mini/aot-compiler.c) can help to find out which ones.
- Some of the time is spent in the .NET Android binding framework (e.g. `Android.Runtime.JNIEnv` or `Java.Interop.TypeManager`), operations that cannot be adjusted by the application. One change to consider is to reduce the native code invocations to a strict minimum, where impactful.

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
