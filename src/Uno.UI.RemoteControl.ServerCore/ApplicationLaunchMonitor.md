# Application launch monitoring

## What it is
- In-memory helper that correlates “I launched an app” with “that app connected back.”
- Register a launch, then report a connection. It matches them 1:1, launches that have not timed out first, and handles timeouts.
- When a launched app fails to connect, `OnTimeout` is invoked.
- Thread-safe and `IDisposable`.
- Pairs on MVID + Platform, preferring the same IsDebug. MVID is the module version ID of the app’s head assembly (unique per build). More info: https://learn.microsoft.com/en-us/dotnet/api/system.reflection.module.moduleversionid

## How to use
### 1) Create the monitor (optionally with callbacks and a custom timeout)

```csharp
using Uno.UI.RemoteControl.Server.AppLaunch;

var options = new ApplicationLaunchMonitor.Options
{
    // Default is 60s
    Timeout = TimeSpan.FromSeconds(30),
    OnRegistered = ev => logger.LogInformation($"Launch registered: {ev.Mvid} ({ev.Platform})"),
    OnConnected = ev => logger.LogInformation($"Connected: {ev.Mvid} ({ev.Platform})"),
    OnTimeout   = ev => logger.LogWarning($"Timed out: {ev.Mvid} ({ev.Platform})")
};

using var monitor = new ApplicationLaunchMonitor(options: options);
```

### 2) When you start a target app, register the launch
```csharp
// New signature includes IDE and Uno plugin version
monitor.RegisterLaunch(mvid, "Wasm", isDebug: true, ide: "VisualStudio", plugin: "uno-vs-extension-1.2.3");
```

### 3) When the app connects back to the dev server, report the connection
```csharp
monitor.ReportConnection(mvid, "Wasm", isDebug: true);
```

The monitor pairs the connection with a pending launch for the same (mvid, platform). It takes, in order:
1. the oldest launch with the same isDebug that has not timed out;
2. the oldest launch with the other isDebug that has not timed out;
3. the oldest timed-out launch with the same isDebug.

If no connection arrives before the timeout, `OnTimeout` is invoked, and the launch stays matchable until `Retention`.

The other isDebug is accepted because the IDE reports whether it started the debugger, while the app reports whether a debugger was attached when it connected, and the two can differ for the same launch. A launch registered after the app connected takes a pending connection in the same order: one within the timeout with the same isDebug, then with the other, then an older one with the same isDebug.

Step 2 comes before step 3 on purpose. A launch whose app never connects (a closed tab, a crash at startup) is assumed to be more common than an app that takes longer than the timeout to start while another launch of the same build is pending, so a live launch is taken as the likelier match. The cost is that a slow start overlapping a relaunch can pair the two connections crosswise.

## Notes
- Platform matching is case-sensitive ("Wasm" != "wasm").
- A null platform is normalized to "Unspecified".
- Registrations are consumed oldest first, launches that have not timed out before those that have.
- A launch counts as timed out once its timeout has fired; `OnTimeout` is then invoked for it, possibly at the same time as a connection that takes it reports it as timed out. A launch a connection takes before its timeout fires is never reported to `OnTimeout`, and a connection and a launch are paired in one step, so neither can be taken without the other.
- Always dispose the monitor (use `using` as shown).

## Analytics events
Events emitted by the monitor are prefixed with `uno/dev-server/app-launch/`. See `Telemetry.md` in the `.Host` project for details.

## Integration points (IDE, transport, HTTP)
The dev-server can receive registration and connection events through multiple channels:

- IDE → DevServer: `AppLaunchRegisterIdeMessage` (scope: `AppLaunch`) carrying MVID, Platform, IsDebug. Triggers `RegisterLaunch`.
- Runtime → DevServer over transport (WebSocket by default) (scope: `DevServerChannel`): `AppLaunchMessage` with `Step = Launched`. Triggers `RegisterLaunch`.
- HTTP GET → DevServer: `GET /applaunch/{mvid}?platform={platform}&isDebug={true|false}`. Triggers `RegisterLaunch`.

Connections are reported by the runtime after establishing the transport connection (WebSocket by default):

- Runtime → DevServer over transport (WebSocket by default) (scope: `DevServerChannel`): `AppLaunchMessage` with `Step = Connected`. Triggers `ReportConnection`.

If no matching `Connected` message arrives before timeout, a timeout event is emitted.

## Testing / time control

- The constructor accepts an optional `TimeProvider` which the monitor uses for timeout scheduling. Tests commonly inject `Microsoft.Extensions.Time.Testing.FakeTimeProvider` and advance time with `fake.Advance(...)` to trigger timeouts instantly.
- Example for tests:

```csharp
var fake = new FakeTimeProvider(DateTimeOffset.UtcNow);
using var monitor = new ApplicationLaunchMonitor(timeProvider: fake, options: options);
// register, then fake.Advance(TimeSpan.FromSeconds(11)); // triggers timeout callbacks
```

## Where it lives
- Project: Uno.UI.RemoteControl.Server
- Folder: AppLaunch
- File: `ApplicationLaunchMonitor.cs` (implementation)
