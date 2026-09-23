---
uid: Uno.Features.BackgroundTasks
---

# Background tasks

Uno implements the `Windows.ApplicationModel.Background` time-triggered task
API on macOS and Linux Skia desktop targets.

## API and source provenance

These APIs are Windows OS WinRT contracts. They are not implemented by the
open-source `microsoft-ui-xaml` repository, which only consumes
`IBackgroundTaskInstance` for `XamlRenderingBackgroundTask` and reports that
the type is unavailable in WinUI 3.

Uno therefore provides an original native implementation that preserves the
`Windows.ApplicationModel.Background` public surface where supported. This is
not a translation of WinUI C++ implementation code.

The Windows App SDK also exposes
`Microsoft.Windows.ApplicationModel.Background.BackgroundTaskBuilder` for
packaged desktop applications and CLSID-based activation. That separate
builder is not implemented here. Uno Skia uses the existing
`Windows.ApplicationModel.Background.BackgroundTaskBuilder` and requires a
string `TaskEntryPoint`.

## Supported API

- `BackgroundExecutionManager`
- `BackgroundTaskBuilder`
- `BackgroundTaskRegistration`
- `TimeTrigger`
- out-of-process task activation through `IBackgroundTask`
- task progress, completion, cancellation, and deferrals

The application executable is also the background task host. Uno intercepts
its internal activation argument before creating a UI host, loads the
`TaskEntryPoint` type from the application assembly, and invokes
`IBackgroundTask.Run`.

```csharp
public sealed class RepositoryBackupTask : IBackgroundTask
{
    public void Run(IBackgroundTaskInstance taskInstance)
    {
        var deferral = taskInstance.GetDeferral();
        _ = RunAsync(taskInstance, deferral);
    }

    private static async Task RunAsync(
        IBackgroundTaskInstance taskInstance,
        BackgroundTaskDeferral deferral)
    {
        try
        {
            await BackupAsync();
            taskInstance.Progress = 100;
        }
        finally
        {
            deferral.Complete();
        }
    }
}

var builder = new BackgroundTaskBuilder
{
    Name = "Repository backup",
    TaskEntryPoint = typeof(RepositoryBackupTask).FullName!
};
builder.SetTrigger(new TimeTrigger(60, oneShot: false));
var registration = builder.Register();
```

The task type must have a public parameterless constructor. Applications that
enable trimming must preserve the task type and constructor because activation
uses reflection.

## Native implementation

### macOS

Uno writes a per-user LaunchAgent property list below
`~/Library/LaunchAgents` and registers it with `launchctl bootstrap`.
This requires a stable installed application path and is unavailable to
sandboxed Mac App Store applications. `launchd` does not support removing a
job while allowing its current process to finish, so
`Unregister(cancelTask: false)` can still terminate an active macOS task.
One-shot tasks use `LaunchOnlyOnce` and are disabled before their persisted
registration is removed.

### Linux

Uno writes a user service and timer below `~/.config/systemd/user` (or
`$XDG_CONFIG_HOME/systemd/user`) and enables the timer through
`systemctl --user`. A running systemd user manager is required. By default,
user services run while the user has a login session; running after logout
requires systemd linger to be enabled by the system administrator.

## Limitations

- `TimeTrigger` is the only supported trigger. Registration rejects intervals
  shorter than the WinRT minimum of 15 minutes.
- Native schedulers treat `FreshnessTime` as an interval. They do not provide
  exact daily, weekly, monthly, or calendar-time scheduling.
- `TaskEntryPoint` is required; in-process foreground activation is not
  supported.
- COM/CLSID entry points, registration groups, background conditions,
  maintenance triggers, and in-process `BackgroundActivated` delivery are not
  supported. Registration fails instead of silently accepting these options.
- The task type must have a public parameterless constructor. Reflection-based
  activation requires trimming and NativeAOT configurations to preserve the
  type and constructor.
- The executable must remain at the path captured when the task is registered.
  Re-register tasks after moving or replacing an unpackaged application.
- Background identity is captured from the Uno application assembly, which can
  differ from a desktop head executable in a multi-project application.
- `BackgroundExecutionManager` reports
  `AllowedSubjectToSystemPolicy` when the native scheduler is available.
  macOS and Linux have no equivalent user-access revocation API, so
  `RemoveAccess` is a no-op and application-id overloads represent only the
  current Uno application.
- `Completed` and `Progress` use local file notifications. They are delivered
  only while a foreground process has subscribed to the registration events;
  they are not queued for a later app launch.
- Cancellation allows five seconds for a task to complete its deferrals.
  systemd and launchd are configured to terminate a process that exceeds the
  native 30-second stop timeout.
- macOS can coalesce missed `StartInterval` activations while the computer is
  asleep. Linux monotonic timers do not catch up intervals missed while the
  systemd user manager is stopped.
- systemd tasks normally stop with the user's login session. Enabling linger
  is an administrator-controlled system policy and Uno does not change it.
- Native schedulers can delay or coalesce activations according to
  power-management and session policies; they do not provide Windows
  background-execution quotas or guarantees.

For native behavior details, see
[launchd.plist](https://www.manpagez.com/man/5/launchd.plist/) and
[systemd.timer](https://www.freedesktop.org/software/systemd/man/latest/systemd.timer.html).
For the Windows contracts, see
[Windows.ApplicationModel.Background](https://learn.microsoft.com/uwp/api/windows.applicationmodel.background)
and the
[Windows App SDK background task migration guidance](https://learn.microsoft.com/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/background-task-migration-strategy).
