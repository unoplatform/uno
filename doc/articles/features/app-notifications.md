---
uid: Uno.Features.AppNotifications
---

# App Notifications

> [!TIP]
> This article covers Uno-specific information for the `Microsoft.Windows.AppNotifications` namespace. For a full description of the feature and instructions on using it, see [Microsoft.Windows.AppNotifications Namespace](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.appnotifications).

* The `Microsoft.Windows.AppNotifications` namespace posts, updates, and removes app notifications, and raises `AppNotificationManager.NotificationInvoked` when the user interacts with one.
* Notification content is authored with `Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder`, which produces the same `ToastGeneric` XML payload on every target. Each platform translates that payload to its own notification API, ignoring the parts it cannot express.

## Supported features

| Feature | Windows | Android | iOS/tvOS | Web (WASM) | macOS | Linux (Skia) |
|---|---|---|---|---|---|---|
| `Register` / `Unregister` / `UnregisterAll` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| `IsSupported()` and `Setting` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| `Show` (text, images, audio, scenario) | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Buttons, text boxes, combo boxes | ✔ | ✔ | ✔ | ✔ (buttons only) | ✔ | ✔ (buttons only) |
| `NotificationInvoked` activation, including user input | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Cold-start activation (app launched by the notification) | ✔ | ✔ | ✔ | ✔ (service-worker mode) | ✔ | ✔ |
| `UpdateAsync` (progress bar updates) | ✔ | ✔ | ✖ | ✖ | ✖ | ✔ |
| `RemoveByIdAsync` / `ByTag` / `ByTagAndGroup` / `ByGroup` / `RemoveAllAsync` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| `GetAllAsync` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| `Expiration` and `ExpiresOnReboot` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| `ScheduledToastNotification` (`Windows.UI.Notifications`) | ✔ | ✔ | ✔ | ✖ | ✔ | ✔ |

Notes and platform constraints:

* **Progress updates** require a platform that can mutate a notification that is already on screen. Apple platforms and browsers replace the notification instead of updating it, so `UpdateAsync` returns `AppNotificationProgressResult.Unsupported` there rather than silently re-posting.
* **Combo boxes** have no representation in the browser Notification API or in the freedesktop notification specification; those payload parts are dropped and reported through the Uno log.
* **Linux** uses the `org.freedesktop.Notifications` D-Bus service. When no notification daemon is running, `AppNotificationManager.IsSupported()` is `false` and the remove operations fault instead of reporting a success that did not happen.
* **Web (WASM)** requires a secure context (`https://` or `localhost`) and user-granted notification permission. Scheduling (`ToastNotifier.AddToSchedule`) has no browser equivalent and is not emulated.
* **Windows** uses the Windows App SDK notification platform. For non-packaged Skia Win32 applications, the Windows App Runtime bootstrapper is initialized automatically; when it is unavailable, `IsSupported()` returns `false` and `Setting` returns `AppNotificationSetting.Unsupported`.
* On every target, notification state is persisted so that ids, tags, groups, progress data and pending activations survive a process restart.

## Using app notifications with Uno

### Requesting permission

`Register()` starts the platform permission flow where one exists (Android 13+, iOS, macOS, browsers). Call it from a user-initiated action and wait until `AppNotificationManager.Default.Setting` is `AppNotificationSetting.Enabled` before showing the first notification:

```csharp
var manager = AppNotificationManager.Default;
manager.NotificationInvoked += OnNotificationInvoked;
manager.Register();

if (manager.Setting == AppNotificationSetting.Enabled)
{
    manager.Show(new AppNotificationBuilder()
        .AddText("Download complete")
        .AddArgument("action", "open")
        .BuildNotification());
}
```

### Persistent notifications on WebAssembly

By default, WebAssembly uses document-scoped browser notifications, which disappear when the page is closed. Opt into service-worker-backed notifications during startup to keep notifications alive after the page is closed and to reopen or focus the application on activation:

```csharp
WinRTFeatureConfiguration.AppNotifications.UseServiceWorkerOnWebAssembly = true;
```

This flag must be set before `AppNotificationManager.Default` is first accessed. See [Feature flags](xref:Uno.Development.FeatureFlags) for details.

## See app notifications in action

The `AppNotificationManager` sample in the Uno Platform samples app (`Microsoft.Windows.AppNotifications` category) exercises registration, posting, progress updates, scheduling, activation, history, and removal on every target.
