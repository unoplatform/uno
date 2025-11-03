# Uno DevServer Processors

This document describes how server processors are discovered, loaded, and resolved by the Uno DevServer (RemoteControl host). It targets Uno contributors and maintainers and references concrete code in this repository.

## TL;DR

- Processors are discovered from a base directory (optionally scoped to a target framework subfolder like `net10.0`) using the pattern `Uno.*.Processor*.dll`.
- Each connection (application instance) gets its own collectible AssemblyLoadContext (ALC) to isolate processors and enable unloading.
- Dependency resolution for a processor assembly is rooted at the processor assembly’s directory. The server sets a per-connection resolve location before loading and uses a custom `Resolving` handler to load neighbor assemblies.
- To be discovered, your assembly name must contain the segment `.Processor` (e.g., `Uno.Test.Processor.dll`). `Uno.*.TestProcessor.dll` will NOT match the discovery pattern.
- To be instantiable, all processor dependencies must be present adjacent to the processor assembly (same folder). The server will try to resolve them from there.

## Message flow

- Client (IDE/test) sends `ProcessorsDiscovery` with a `BasePath` and an `AppInstanceId`.
  - Code: [ProcessorsDiscovery.cs](../Uno.UI.RemoteControl/Messages/ProcessorsDiscovery.cs#L1-L30)
- Server handles it in `RemoteControlServer.ProcessDiscoveryFrame`.
  - Code: [RemoteControlServer.cs](./RemoteControlServer.cs#L448-L640) (method `ProcessDiscoveryFrame`)
- Server responds with `ProcessorsDiscoveryResponse` containing:
  - The list of assembly paths that were processed
  - A list of discovered processors: type, version, `IsLoaded`, and optional `LoadError`
  - Code: [ProcessorsDiscoveryResponse.cs](../Uno.UI.RemoteControl/Messages/ProcessorsDiscoveryResponse.cs#L1-L40)

## Discovery algorithm

Inside `ProcessDiscoveryFrame` the server:

1. Normalizes the base path and, when applicable, appends the current TFMs subfolder:
   - `basePath/net10.0` (or `net9.0`), falling back to `basePath` if it doesn’t exist.
2. Enumerates processor candidates by file name:
   - `Directory.GetFiles(basePath, "Uno.*.Processor*.dll")`
   - Note the required segment: `.Processor` must appear in the assembly name. Examples:
     - Matches: `Uno.Test.Processor.dll`, `Uno.HotReload.Processor.Custom.dll`
     - Does NOT match: `Uno.UI.RemoteControl.TestProcessor.dll` (contains `TestProcessor`, not `.Processor`)
3. Skips the server’s own assembly by name.
4. Loads assemblies into a connection-specific ALC after priming dependency resolution (details below).
5. For each assembly, finds `[assembly: ServerProcessor(typeof(...))]` attributes and attempts instantiation via DI.
6. Registers successfully created processors and returns a `ProcessorsDiscoveryResponse`.

Relevant code:
- File enumeration and load: [RemoteControlServer.cs](./RemoteControlServer.cs#L456-L500)
- Resolve-base assignment and robust load: [RemoteControlServer.cs](./RemoteControlServer.cs#L468-L490)
- Attribute discovery and instantiation (ActivatorUtilities): [RemoteControlServer.cs](./RemoteControlServer.cs#L498-L560)

## Loading context and isolation

- The server creates one collectible `AssemblyLoadContext` per application instance ID.
  - Code: [RemoteControlServer.cs – GetAssemblyLoadContext](./RemoteControlServer.cs#L60-L140)
- A per-connection dictionary maps the app ID to the current resolve base path: `_resolveAssemblyLocations[appId]`.
- The ALC’s `Resolving` event handler attempts to resolve missing dependencies by probing the directory of the processor assembly (the resolve base):
  - `Path.Combine(dir, assemblyName.Name + ".dll")`
  - If found, the file is loaded via `TryLoadAssemblyFromPath`

This design ensures:
- Processors are isolated from each other and from the host’s default load context
- Dependencies can be resolved side-by-side with the processor assembly
- Contexts can be unloaded when the connection ends

Key code:
- ALC creation and Resolving handler: [RemoteControlServer.cs – GetAssemblyLoadContext](./RemoteControlServer.cs#L60-L140)
- Robust file load helper with retries: [RemoteControlServer.cs – TryLoadAssemblyFromPath](./RemoteControlServer.cs#L148-L210)

## Dependency resolution contract

To guarantee successful processor instantiation:

- Place all processor dependencies next to the processor assembly (same directory). When discovery targets `basePath/net10.0`, put dependencies in that folder.
- The server sets the resolve base before loading each processor:
  - `_resolveAssemblyLocations[appId] = processorPath` (the full path to the processor DLL)
  - The ALC Resolving handler uses this to probe the processor’s directory
- Assemblies are loaded via `TryLoadAssemblyFromPath(ALC, processorPath)` to avoid transient locks and to normalize paths.

If the resolve base is not set prior to loading, the ALC may fail to locate neighbor dependencies, resulting in a `FileNotFoundException` when the processor constructor runs. This was the root cause of the regression captured by the test `Telemetry_ProcessorDiscovery_Should_Load_Dependencies`.

### Naming and layout requirements

- Assembly naming must include `.Processor` segment to be discovered:
  - Good: `Uno.SuperFeature.Processor.dll`
  - Avoid: `MyProject.Something.MyProcessor.dll`
- Folder layout for discovery:
  - Base path provided to `ProcessorsDiscovery`
  - If `<base>/<ftm>` exists, the server enumerates there; otherwise it uses `<base>`
  - Place your processor and its dependencies in that folder

### Attribute requirements

- Annotate your assembly with the processor marker:
  - `[assembly: ServerProcessor(typeof(MyProcessor))]`
- Optional telemetry attribute to wire telemetry context:
  - `[assembly: Telemetry("MyKey", EventsPrefix = "uno/dev-server/my-proc")]`
- Implement `IServerProcessor` and provide a valid `Scope` name

Example (excerpt from test processor):

```csharp
// Assembly-level attributes in the processor assembly
[assembly: ServerProcessor<TelemetryTestProcessor>]
[assembly: Telemetry("TestProcessorKey", EventsPrefix = "uno/dev-server/test-proc")]

public class TelemetryTestProcessor : IServerProcessor
{
    public TelemetryTestProcessor(IRemoteControlServer server, ITelemetry<TelemetryTestProcessor> telemetry)
    {
        // Accessing a dependency triggers resolution via the ALC Resolving hook
        _ = DummyDependency.Touch();
    }

    public string Scope => "TelemetryTest";
    public Task ProcessFrame(Frame frame) => Task.CompletedTask;
}
```

See the real file for context: [TelemetryTestProcessor.cs](../Uno.UI.RemoteControl.TestProcessor/TelemetryTestProcessor.cs#L6-L12).

## The fixed regression (why tests were red)

Symptoms captured by `Telemetry_ProcessorDiscovery_Should_Load_Dependencies`:
- Processor discovered, but instantiation fails with:
  - `System.IO.FileNotFoundException: Could not load file or assembly 'Uno.UI.RemoteControl.TestProcessor.Dependency, ...'`
- Root cause: The resolve base (`_resolveAssemblyLocations[appId]`) was not set before loading/instantiating the processor, so the ALC’s `Resolving` handler had no directory to probe for dependencies.

Fix implemented:
- Before loading each candidate assembly:
  - Compute `processorPath = Path.GetFullPath(file)`
  - Set `_resolveAssemblyLocations[appId] = processorPath`
  - Load using `TryLoadAssemblyFromPath(alc, processorPath)`
- Result: Dependencies next to the processor are correctly resolved; the test goes green.

Code references:
- Discovery, resolve base assignment, loading: [RemoteControlServer.cs](./RemoteControlServer.cs#L448-L640)
- ALC and `Resolving` handler: [RemoteControlServer.cs – GetAssemblyLoadContext](./RemoteControlServer.cs#L60-L140)
- Robust loader: [RemoteControlServer.cs – TryLoadAssemblyFromPath](./RemoteControlServer.cs#L148-L210)

## Advanced notes

- Additional discovery path: the client can pass an additional processors discovery path to the `RemoteControlClient` when initializing (see `RemoteControlClient.Initialize` overload). The server merges these with local discovery.
  - Code: [RemoteControlClient.cs – Initialize overload](../Uno.UI.RemoteControl/RemoteControlClient.cs#L134-L151)

## How to run DevServer tests (MTP)

For fast iterations, run the Microsoft.Testing.Platform (MTP) test executable directly. This avoids VSTest property shims that can ignore filters.

Steps on Windows PowerShell:

1) Build the test project

```powershell
cd d:\src\Uno\src
dotnet build .\Uno.UI.RemoteControl.DevServer.Tests\Uno.UI.RemoteControl.DevServer.Tests.csproj -c Debug --no-restore
```

2) Run the test executable with filtering (recommended)

```powershell
# List tests
.\Uno.UI.RemoteControl.DevServer.Tests\bin\Debug\net10.0\Uno.UI.RemoteControl.DevServer.Tests.exe --list-tests

# Run a single test using MTP filter
.\Uno.UI.RemoteControl.DevServer.Tests\bin\Debug\net10.0\Uno.UI.RemoteControl.DevServer.Tests.exe --filter "FullyQualifiedName~Telemetry_ProcessorDiscovery_Should_Load_Dependencies_From_BasePath"

# Optional: increase verbosity
.\Uno.UI.RemoteControl.DevServer.Tests\bin\Debug\net10.0\Uno.UI.RemoteControl.DevServer.Tests.exe --filter "FullyQualifiedName~Telemetry_ProcessorDiscovery_Should_Load_Dependencies" --verbosity detailed
```

Notes:
- When invoking via `dotnet test`, MTP may ignore VSTest-specific properties (e.g., `--filter` and `--logger`), running the full suite. Prefer the executable for precise filtering.
- The tests target `net10.0`. Adjust the path if you build a different configuration or TFM.
- Unloading: Each connection’s ALC is collectible; contexts are reference-counted and unloaded when not used, to minimize memory footprint.
- Telemetry: Discovery emits `processor-discovery-complete` with success/partial-failure metrics and lists failed processors.

## Troubleshooting checklist

- Name matches discovery pattern: `Uno.*.Processor*.dll` with a `.Processor` segment.
- Files placed in the correct folder: `basePath/net10.0` if it exists; otherwise `basePath`.
- All dependencies copied adjacent to the processor assembly.
- Assembly-level attributes present (`ServerProcessor` at minimum).
- If running into `FileNotFoundException` during constructor:
  - Ensure the server fix is present (resolve base set before load).
  - Confirm the dependency file name matches the requested assembly identity (e.g., `Uno.UI.RemoteControl.TestProcessor.Dependency.dll`).
