# Application Launch Monitoring

## What it is
- Small in-memory helper used by the Uno Remote Control Dev Server to correlate “I launched an app” with “that app connected back.”
- You tell it that an app was launched, then you report when a matching app connects. It matches them 1:1 in launch order and handles timeouts.
- When a launched application fails to connect, it is reported as a timeout thru the OnTimeout callback.
- It is thread-safe / Disposable.
- It uses a composite key: MVID + Platform + IsDebug. The MVID is the _Module Version ID_ of the app (head) assembly, which is unique per build. More info: https://learn.microsoft.com/en-us/dotnet/api/system.reflection.module.moduleversionid

## How to Use

### 1) Create the Monitor

Create the monitor with optional callbacks and a custom timeout:

```csharp
using Uno.UI.RemoteControl.Server.AppLaunch;

var options = new ApplicationLaunchMonitor.Options
{
    // Default is 60 seconds
    Timeout = TimeSpan.FromSeconds(30),
    
    // Optional: Called when a launch is registered
    OnRegistered = ev => logger.LogInformation($"Launch registered: {ev.Mvid} ({ev.Platform})"),
    
    // Optional: Called when an app successfully connects
    OnConnected = ev => logger.LogInformation($"Connected: {ev.Mvid} ({ev.Platform})"),
    
    // Optional: Called when an app fails to connect within timeout
    OnTimeout = ev => logger.LogWarning($"Timed out: {ev.Mvid} ({ev.Platform})")
};

using var monitor = new ApplicationLaunchMonitor(options: options);
```

### 2) Register an App Launch

When you start a target app, register the launch with IDE and plugin version information:

```csharp
// Register a launch with IDE and Uno plugin version information
monitor.RegisterLaunch(
    mvid: "abc123-def456-...",
    platform: "Wasm",
    isDebug: true,
    ide: "VisualStudio",
    plugin: "uno-vs-extension-1.2.3"
);
```

### 3) When the app connects back to the dev server, report the connection:
```csharp
monitor.ReportConnection(mvid, "Wasm", isDebug: true);
```

That’s it. The monitor pairs the connection with the oldest pending launch for the same (mvid, platform, isDebug). If no connection arrives before the timeout, OnTimeout is invoked.

## Important Notes

- **Platform matching is case-sensitive**: `"Wasm"` ≠ `"wasm"`
- **Platform must not be null or empty**: Throws `ArgumentException` if invalid
- **Registrations are consumed in FIFO order** per key
- **Always dispose the monitor**: Use `using` statement as shown in examples
- **MVID is never transmitted in telemetry**: Used only internally for matching; telemetry events send only platform, debug status, IDE, and plugin version

## Analytics Events

The Application Launch Monitor emits telemetry events with the prefix `uno/dev-server/app-launch/`.

### Events

| Event Name | Properties | Measurements |
|------------|-----------|--------------|
| **app-launch/launched** | TargetPlatform, IsDebug, IDE, PluginVersion | None |
| **app-launch/connected** | TargetPlatform, IsDebug, IDE, PluginVersion, WasTimedOut, WasIdeInitiated | LatencyMs |
| **app-launch/connection-timeout** | TargetPlatform, IsDebug, IDE, PluginVersion | TimeoutSeconds |

**Privacy Note:** MVID is **not included** in telemetry events. Only platform, debug mode, IDE type, and plugin version are transmitted.

For more details on these events, see:
- [DevServer Telemetry Documentation](../Uno.UI.RemoteControl.Host/Telemetry.md)
- [Uno Platform Telemetry Overview](../../doc/articles/uno-platform-telemetry.md)

## Integration Points

The dev-server can receive registration and connection events through multiple channels:

### Launch Registration Channels

1. **IDE → DevServer**: `AppLaunchRegisterIdeMessage` (scope: `AppLaunch`)
   - Carries: MVID, Platform, IsDebug, IDE, PluginVersion
   - Triggers: `RegisterLaunch`

2. **Runtime → DevServer (WebSocket)**: `AppLaunchMessage` with `Step = Launched` (scope: `DevServerChannel`)
   - Carries: MVID, Platform, IsDebug
   - Triggers: `RegisterLaunch`

3. **HTTP GET → DevServer**: `GET /applaunch/{mvid}?platform={platform}&isDebug={true|false}`
   - Carries: MVID, Platform, IsDebug
   - Triggers: `RegisterLaunch`

### Connection Reporting

- **Runtime → DevServer (WebSocket)**: `AppLaunchMessage` with `Step = Connected` (scope: `DevServerChannel`)
  - Carries: MVID, Platform, IsDebug
  - Triggers: `ReportConnection`

### Timeout Handling

If no matching `Connected` message arrives before the timeout period expires, a timeout event is emitted and the `OnTimeout` callback is invoked.

## Testing and Time Control

The constructor accepts an optional `TimeProvider` which the monitor uses for timeout scheduling. This allows for controlled time manipulation in tests.

### Using FakeTimeProvider for Testing

Tests commonly inject `Microsoft.Extensions.Time.Testing.FakeTimeProvider` and advance time with `fake.Advance(...)` to trigger timeouts instantly.

Example:
```csharp
using Microsoft.Extensions.Time.Testing;

var fake = new FakeTimeProvider(DateTimeOffset.UtcNow);
var options = new ApplicationLaunchMonitor.Options
{
    Timeout = TimeSpan.FromSeconds(10)
};

using var monitor = new ApplicationLaunchMonitor(timeProvider: fake, options: options);

// Register a launch
monitor.RegisterLaunch("test-mvid", "Wasm", true, "TestIDE", "1.0.0");

// Advance time past timeout to trigger timeout callbacks instantly
fake.Advance(TimeSpan.FromSeconds(11));
```

## Project Location

- **Project**: `Uno.UI.RemoteControl.Server`
- **Folder**: `AppLaunch`
- **Files**:
  - `ApplicationLaunchMonitor.cs` - Implementation
  - `ApplicationLaunchMonitor.md` - This documentation

## See Also

- [Uno Platform Telemetry Overview](../../doc/articles/uno-platform-telemetry.md)
- [DevServer Telemetry](../Uno.UI.RemoteControl.Host/Telemetry.md)
- [Hot Reload Telemetry](../Uno.UI.RemoteControl.Server.Processors/Telemetry.md)
- [Build Tools Telemetry](../../doc/articles/uno-toolchain-telemetry.md)
