# Profiling Uno Platform Applications

## Profiling .NET 6 Android applications

As of Preview 7, .NET 6 provides the ability to do CPU profiling through [`dotnet-trace`](https://docs.microsoft.com/dotnet/core/diagnostics/dotnet-trace) for android applications.


## Profiling Xamarin.Android and Xamarin.iOS applications

### Pre-requisites
Run the following commands
- `dotnet tool update -g dotnet-dsrouter --add-source=https://aka.ms/dotnet-tools/index.json`
- `dotnet tool update -g dotnet-trace --add-source=https://aka.ms/dotnet-tools/index.json`

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
    ```
    DOTNET_DiagnosticPorts=127.0.0.1:9000,suspend
    ```
- `environment.emulator.txt`
    ```
    DOTNET_DiagnosticPorts=10.0.2.2:9001,suspend
    ```

Note that the `suspend` directive means that if `dotnet-trace` is not running, the application waits for it to start.

### Profiling the application

- Start the diagnostics router, in any folder:
    ```
    dotnet-dsrouter client-server -tcps 127.0.0.1:9001 -ipcc /tmp/uno-app --verbose debug
    ```
- Start `dotnet-trace`, in the app folder or where you want your traces to be stored:
    ```
    dotnet-trace collect --diagnostic-port /tmp/uno-app --format speedscope -o uno-app-trace
    ```
- Start an `x86-64` emulator or `arm64` (`armv8`) device
    > Running on a 32 bits device is not supported and will generate unusable traces in SpeedScope
- Build the application with profiling enabled
    ```
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
        <WasmShellEnableEmccProfiling>InterpreterAndAOT</WasmShellEnableEmccProfiling>
    </PropertyGroup>
    ```

- Build and deploy the application
- Open the `Performance` tab in your browser
- Use your application or restart your application while recording the trace

### Troubleshooting
- Deep traces found in large async code patterns or complex UI trees may hit [this chromium issue](https://bugs.chromium.org/p/chromium/issues/detail?id=1206709). This generally makes traces very long to load; you'll need to be patient.

