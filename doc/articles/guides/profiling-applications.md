---
uid: Uno.Tutorials.ProfilingApplications
---

# Profiling Uno Platform Applications

## Profiling .NET Android/iOS applications

.NET 7 and later provides the ability to do CPU profiling through [`dotnet-trace`](https://docs.microsoft.com/dotnet/core/diagnostics/dotnet-trace) for android applications.

### Pre-requisites

Run the following commands

- `dotnet tool update -g dotnet-dsrouter --add-source=https://aka.ms/dotnet-tools/index.json`
- `dotnet tool update -g dotnet-trace --add-source=https://aka.ms/dotnet-tools/index.json`

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

    ```bash
    $ dotnet-dsrouter client-server -ipcc ~/my-sim-port -tcps 127.0.0.1:9000
    ```

2. Launch the app and make it suspend upon launch (waiting for the .NET tooling to connect):

    ```bash
    $ mlaunch --launchsim bin/Debug/net*/*/*.app --device :v2:runtime=com.apple.CoreSimulator.SimRuntime.iOS-15-4,devicetype=com.CoreSimulator.SimDeviceType.iPhone-11 --wait-for-exit --stdout=$(tty) --stderr=$(tty) --argument --connection-mode --argument none '--setenv:DOTNET_DiagnosticPorts=127.0.0.1:9000,suspend'
    ```

3. At this point it's necessary to wait until the following line shows up in the terminal:

    ```console
    The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command from a Diagnostic Port
    ```

4. Once that's printed, go ahead and start profiling:

    ```bash
    $ dotnet-trace collect --diagnostic-port ~/my-sim-port --format speedscope
    ```

To find which device to use, use:

```bash
$ xcrun simctl list devices
```

Then reference the UDID of the simulator in the mlaunch command:

```bash
$ mlaunch ... --device :v2:udid=50BCC90D-7E56-4AFB-89C5-3688BF345998 ...
```

### Profiling on a physical iOS device

Launch the tool that bridges the app and the .NET tracing tools:

```bash
$ dotnet-dsrouter server-client -ipcs ~/my-dev-port -tcpc 127.0.0.1:9001 --forward-port iOS
```

Install & launch the app and make it suspended upon launch:

```bash
$ mlaunch --installdev bin/Debug/net*/*/*.app --devname ... 
$ mlaunch --launchdev bin/Debug/net*/*/*.app --devname ... --wait-for-exit --argument --connection-mode --argument none '--setenv:DOTNET_DiagnosticPorts=127.0.0.1:9001,suspend,listen'
```

At this point, it's necessary to wait until the following line shows up in the terminal:

```console
The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command from a Diagnostic Port
```

Once that's printed, go ahead and start profiling:

```bash
$ dotnet-trace collect --diagnostic-port ~/my-dev-port,connect --format speedscope
```

## Profiling Catalyst apps

1. Launch the executable, passing the `DOTNET_DiagnosticPorts` variable directly:

    ```bash
    $ DOTNET_DiagnosticPorts=~/my-desktop-port,suspend ./bin/Debug/net6.0-*/*/MyTestApp.app/Contents/MacOS/MyTestApp
    ```

2. At this point it's necessary to wait until the following line shows up in the terminal:

    ```bash
    The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command from a Diagnostic Port
    ```

3. Once that's printed, go ahead and start profiling:

    ```bash
    $ dotnet-trace collect --diagnostic-port ~/my-desktop-port --format speedscope
    ```

## Profiling .NET Android applications

### Adjust your application to enable profiling

Profiling has to first be enabled in the application. Some additional properties need to be added in the `MyApp.Mobile` project :

```xml
<PropertyGroup>
    <RuntimeIdentifier Condition="'$(TargetFramework)' == 'net6.0-android'">android-x64</RuntimeIdentifier>
</PropertyGroup>

<PropertyGroup Condition="'$(AndroidEnableProfiler)'=='true'">
    <IsEmulator Condition="'$(IsEmulator)' == ''">true</IsEmulator>
    <AndroidLinkResources>true</AndroidLinkResources>
</PropertyGroup>

<ItemGroup Condition="'$(AndroidEnableProfiler)'=='true'">
    <AndroidEnvironment Condition="'$(IsEmulator)' == 'true'" Include="Android\environment.emulator.txt" />
    <AndroidEnvironment Condition="'$(IsEmulator)' != 'true'" Include="Android\environment.device.txt" />
</ItemGroup>
```

Then in the `Android` application folder, add the following two files:

- `environment.device.txt`

    ```text
    DOTNET_DiagnosticPorts=127.0.0.1:9000,suspend
    ```

- `environment.emulator.txt`

    ```text
    DOTNET_DiagnosticPorts=10.0.2.2:9001,suspend
    ```

Note that the `suspend` directive means that if `dotnet-trace` is not running, the application waits for it to start.

### Profiling the application

- Start the diagnostics router, in any folder:

    ```dotnetcli
    dotnet-dsrouter client-server -tcps 127.0.0.1:9001 -ipcc /tmp/uno-app --verbose debug
    ```

- Start `dotnet-trace`, in the app folder or where you want your traces to be stored:

    ```dotnetcli
    dotnet-trace collect --diagnostic-port /tmp/uno-app --format speedscope -o uno-app-trace
    ```

- Start an `x86-64` emulator or `arm64` (`armv8`) device
    > Running on a 32 bits device is not supported and will generate unusable traces in SpeedScope
- Build the application with profiling enabled

    ```dotnetcli
    dotnet build -f net6.0-android -t:run -c Release -p:IsEmulator=true /p:RunAOTCompilation=true /p:AndroidEnableProfiler=true
    ```

- The app will start and the `dotnet-trace` will display a MB number counting up
- Use the app and once done, stop `dotnet-trace` using the specified method (Likely `Enter` or `Ctr+C`)
- Open a browser at `https://speedscope.app` and drop the `uno-app-trace.speedscope.json` file on it

### Analyzing the trace data

This section provides insights into what to look for when analyzing flame charts.

- When building without AOT, a lot of the startup traces will show time spent in `System.Private.CoreLib!System.Runtime.CompilerServices.RuntimeHelpers.CompileMethod(object)`, indicating that that the JIT is doing a lot of work. This can make performance improvements harder to find.
- When building with AOT, most of the IL is compiled to native code with some exceptions. You may still find `RuntimeHelpers.CompileMethod` invocations. In such cases, you may need to find what is causing the AOT compiler to skip IL portions. If the JIT still impacts cold paths of your application, you may still need to adjust your code to avoid the JIT. For instance, some generics constructs force the AOT compiler to still use JITing. In other cases, it could be accessing static type members. The JIT conditions are runtime version dependent, and [looking at the runtime code](https://github.com/dotnet/runtime/blob/9703660baa08914773b26e413e361c8ce04e6d94/src/mono/mono/mini/aot-compiler.c) can help finding out which ones.
- Some of the time is spent in the .NET Android binding framework (e.g. `Android.Runtime.JNIEnv` or `Java.Interop.TypeManager`), operations that cannot be adjusted by the application. One change to consider is to reduce the native code invocations to the strict minimum, where impactful.

## Profiling Skia/GTK and Skia/WPF applications

Profiling Skia based Uno Platform targets can be done on Windows in Visual Studio 2019 and 2022 using [time and memory profilers](https://docs.microsoft.com/visualstudio/profiling/profiling-feature-tour?view=vs-2019).

## Profiling WebAssembly applications

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
