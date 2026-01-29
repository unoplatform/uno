# In-process devserver hosting

This guide explains how to run the transport-agnostic Remote Control devserver ("ServerCore") inside any .NET process without Kestrel. It focuses on composing the required services, building a `RemoteControlServerHost`, and wiring it to an in-memory transport so tests or custom shells can talk to the server without opening HTTP/WebSocket endpoints.

## When to choose in-process hosting
- You want to co-locate the devserver with a custom IDE agent or diagnostic tool to avoid networking and port management.
- You need deterministic integration tests that spin up a full server/client conversation entirely in memory.
- You are prototyping new transports or launch experiences (Hot Design, MCP) and prefer to control the message pump directly via [`FrameTransportPair`](../Uno.UI.RemoteControl.Messaging/Transport.md).

## Required services
ServerCore purposely owns only abstractions; the host must register concrete services before building the server. The table below shows the minimum set you must provide:

| Service | Lifetime | Purpose | Typical implementation |
| --- | --- | --- | --- |
| `IRemoteControlConfiguration` | Singleton | Provides configuration values that processors expect (feature flags, file paths, etc.). | Wrap an `IConfiguration` (see `ConfigurationRemoteControlConfiguration` in `Uno.UI.RemoteControl.Host`). |
| `IIdeChannel` | Singleton | Bridges IDE-originated commands (e.g., keep alive, diagnostics) to the server. Must raise `MessageFromIde` and honor `WaitForReady`. | Reuse `IdeChannelServer` from `Uno.UI.RemoteControl.Host` or provide a test-specific stub. |
| `IApplicationLaunchMonitor` | Singleton | Tracks app launch/connect events for telemetry. See [ApplicationLaunchMonitor.md](ApplicationLaunchMonitor.md). | `Uno.UI.RemoteControl.Server.AppLaunch.ApplicationLaunchMonitor`. |
| `IRemoteControlProcessorFactory` | Scoped | Discovers and instantiates processors when a client sends `ProcessorsDiscovery`. | `DefaultRemoteControlProcessorFactory` (from `Uno.UI.RemoteControl.Server`) or a custom factory for embedded scenarios. |
| `IRemoteControlServer` / `IRemoteControlServerConnection` | Scoped | Connection-scoped runtime that processes frames. | Registered by `AddRemoteControlServerCore()`. |
| `ITelemetry` (optional) | Scoped | Receives server telemetry events per connection. | `ServiceCollectionExtensions.AddConnectionTelemetry()` in `Uno.UI.RemoteControl.Server`. |
| Logging (`ILoggerFactory`, `ILogger<T>`) | Singleton | Enables diagnostics emitted by ServerCore. | `services.AddLogging(...)`. |

> Tip: `Uno.UI.RemoteControl.Server.Helpers.ServiceCollectionExtensions` already contains helpers such as `AddRemoteControlServerCore()`, `AddConnectionTelemetry()`, and `AddGlobalTelemetry()`. Reference `Uno.UI.RemoteControl.Server` if you want the default implementations instead of maintaining your own.

## Quick-start helper: `InProcessDevServer`
When you do not want to assemble the dependency graph yourself, use [src/Uno.UI.RemoteControl.ServerCore/InProcess/InProcessDevServer.cs](src/Uno.UI.RemoteControl.ServerCore/InProcess/InProcessDevServer.cs). It lives in the `DevServerCore` namespace, creates all required services, and exposes a single method that hands an `IFrameTransport` to your runtime without touching Kestrel.

```csharp
using System.Threading;
using DevServerCore;
using Uno.UI.RemoteControl.Messaging;

await using var devserver = InProcessDevServer.Create(options =>
{
    options.ConfigurationValues["feature:EnableRealtime"] = "true";
});

IFrameTransport appTransport = devserver.ConnectApplication(ct: CancellationToken.None);

// Optional: emit IDE messages by providing your own IIdeChannel implementation
// via DevServerCoreHostOptions (IdeChannel or IdeChannelFactory).
```

- `Create` accepts a callback to tweak DI registrations and configuration values before the container is built.
- `ConnectApplication` returns the `IFrameTransport` you should hand to your runtime; disposing it tears down the underlying server loop.
- `InProcessDevServer` implements `IAsyncDisposable`, so `await using` ensures every pending connection and the root host are cleaned up automatically.

## Bootstrapping the host
1. Create an `IServiceCollection` and register the services listed above.
2. Build the root `IServiceProvider`. Decide whether ServerCore should dispose it (`Build`) or reuse an externally-owned provider (`BuildShared`).
3. Create a `RemoteControlServerBuilder`, pass the configured `IServiceCollection`, and call `Build*` to obtain a `RemoteControlServerHost`.
4. Call `StartAsync()` to make the host ready. (The current implementation is synchronous, but calling it keeps the pattern consistent with future lifecycle hooks.)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.UI.RemoteControl.Server.AppLaunch;
using Uno.UI.RemoteControl.Server.Helpers;
using Uno.UI.RemoteControl.Server.Processors;
using Uno.UI.RemoteControl.ServerCore;
using Uno.UI.RemoteControl.ServerCore.Configuration;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddSingleton<IRemoteControlConfiguration>(_ => new ConfigurationRemoteControlConfiguration(configuration));
services.AddSingleton<IIdeChannel, IdeChannelServer>();
services.AddSingleton<IApplicationLaunchMonitor>(_ => new ApplicationLaunchMonitor());
services.AddSingleton<IRemoteControlProcessorFactory, DefaultRemoteControlProcessorFactory>();
services.AddConnectionTelemetry(solutionPath);
services.AddRemoteControlServerCore();

var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
var builder = new RemoteControlServerBuilder(services);
await using var host = builder.BuildShared(serviceProvider);
await host.StartAsync(ct);
```

- Use `BuildShared` when another component should own/dispose the provider (typical inside ASP.NET or hosting tests). Use `Build` when the server host should dispose everything for you.
- You can call `host.StopAsync(ct)` when your process shuts down. It currently acts as a guard (no-op) but keeps the lifecycle symmetrical.

## Running an in-process connection
`RemoteControlServerHost` does not open sockets by itself. Instead, you call `RunConnectionAsync` with a factory that yields the transport representing a single client. `InProcessRemoteControlServerHost` wraps that pattern by creating a [`FrameTransportPair`](../Uno.UI.RemoteControl.Messaging/FrameTransportPair.cs) and exposing the peer that the runtime should use.

```csharp
using System.Collections.Generic;
using Uno.UI.RemoteControl.Messaging;
using Uno.UI.RemoteControl.ServerCore;
using Uno.UI.RemoteControl.ServerCore.InProcess;

await using var inProcessHost = InProcessRemoteControlServerHost.Create(host);

// Optionally describe the connection for logs/telemetry
var descriptor = new RemoteControlConnectionDescriptor(
    TransportName: "in-process",
    RemoteEndpoint: "runtime",
    Properties: new Dictionary<string, string>
    {
        ["IDE"] = "HotDesign",
        ["SessionId"] = sessionId,
    });

// Start the server loop – this consumes FrameTransportPair.Peer1 internally
var serverLoop = inProcessHost.StartAsync(descriptor, ct);

// Give Peer2 to your runtime/client component
IFrameTransport clientTransport = inProcessHost.ClientTransport;
```

Key points:
- Create one `InProcessRemoteControlServerHost` per simultaneous runtime connection. Each wrapper owns its own transport pair and must be disposed when the session ends.
- `ClientTransport` implements `IFrameTransport`, so you can feed it directly to test harnesses or any component that already speaks the Remote Control protocol.
- `Completion` lets you await the end of the server loop (e.g., for deterministic tests).

## Driving the runtime side
Once you have `ClientTransport`, you can script frames manually or plug it into existing tooling. The snippet below sends a processor discovery request entirely in memory:

```csharp
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messages;
using Uno.UI.RemoteControl.Messaging;

var discovery = new ProcessorsDiscovery(appAssemblyPath, appInstanceId: Guid.NewGuid().ToString("N"));
await clientTransport.SendAsync(
    Frame.Create(
        version: 1,
        scope: WellKnownScopes.DevServerChannel,
        name: ProcessorsDiscovery.Name,
        content: discovery),
    CancellationToken.None);

// Read the response without sockets
Frame? response = await clientTransport.ReceiveAsync(ct);
```

In real applications you typically hand `ClientTransport` to a `RemoteControlClient` derivative or a custom harness that knows how to interpret frames. The transport is already connected to the server side, so you do **not** open WebSockets or named pipes.

## Multiple connections and cleanup
- `RemoteControlServerHost` creates a dedicated DI scope per connection. You can call `RunConnectionAsync` repeatedly (either sequentially or concurrently) to serve many runtimes from the same host.
- When a transport completes, ServerCore automatically closes and disposes it. If you terminate the runtime first, call `CloseAsync` on the client transport so the host can finish gracefully.
- Always dispose both `InProcessRemoteControlServerHost` and the root `RemoteControlServerHost` to tear down `FrameTransportPair` instances and scoped services.

## Troubleshooting checklist
- Ensure `IIdeChannel.WaitForReady` completes before the runtime begins sending frames. In tests, stub the channel so it always returns `Task.FromResult(true)` once it is wired.
- If you see `InvalidOperationException: Transport factory returned null`, check that your transport factory actually instantiates an `IFrameTransport` per call and that you are not reusing a disposed instance.
- Processor discovery failures often stem from a missing `IRemoteControlProcessorFactory` registration. The default factory lives in `Uno.UI.RemoteControl.Server.Processors` and can be reused from embedded hosts.
- Use `RemoteControlConnectionDescriptor` metadata to distinguish multiple in-process sessions in logs or telemetry output. This greatly simplifies analyzing traces when you spin several transports in parallel.

With these pieces in place you can host the devserver anywhere: IDE plugins, CLI tools, runtime integration tests, or even nested inside the application under test. The only network-free contract you need to honor is `IFrameTransport`.
