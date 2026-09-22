---
uid: Uno.Features.AppNotifications
---

# App Notifications

> [!TIP]
> This article covers Uno-specific information for the `Microsoft.Windows.AppNotifications` namespace. For a full description of the feature and instructions on using it, see [Microsoft.Windows.AppNotifications Namespace](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.appnotifications).

* The `Microsoft.Windows.AppNotifications` namespace posts, updates, and removes app notifications, and raises `AppNotificationManager.NotificationInvoked` when the user interacts with one.
* Notification content is authored with `Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder`, which produces a `ToastGeneric` XML payload. Portable backends translate the supported payload model to their notification APIs. Unsupported structures are rejected rather than silently truncated.
* The Windows backend passes the original XML to Windows App SDK unchanged. Native-only structures such as adaptive groups, notification headers, and legacy Windows templates also survive storage, replacement, and history retrieval; they do not have to fit the portable translation model.

## Supported features

| Feature | Windows (Skia Win32) | Android | iOS | Web (WASM) | macOS | Linux (Skia) |
|---|---|---|---|---|---|---|
| `Register` / `Unregister` / `UnregisterAll` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| `IsSupported()` and `Setting` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| `Show` (text, images, audio, scenario) | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Raw Windows XML beyond the portable payload model | ✔ | ✖ | ✖ | ✖ | ✖ | ✖ |
| Buttons, text boxes, combo boxes | ✔ | ✔ | ✔ | ✔ (buttons, service-worker mode only) | ✔ | ✔ (buttons only) |
| `NotificationInvoked` activation, including user input | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Cold-start activation (app launched by the notification) | ✔ | ✔ | ✔ | ✔ (service-worker mode) | ✔ | ✔ |
| `UpdateAsync` (progress bar updates) | ✔ | ✔ | ✖ | ✖ | ✖ | ✔ |
| `RemoveByIdAsync` / `ByTag` / `ByTagAndGroup` / `ByGroup` / `RemoveAllAsync` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| `GetAllAsync` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| `Expiration` and `ExpiresOnReboot` | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| `ScheduledToastNotification` (`Windows.UI.Notifications`) | ✖ | ✔ | ✔ | ✖ | ✔ | ✖ |

Notes and platform constraints:

* **Progress updates** require a platform that can mutate a notification that is already on screen. Apple platforms and browsers replace the notification instead of updating it, so `UpdateAsync` returns `AppNotificationProgressResult.Unsupported` there rather than silently re-posting.
* **Combo boxes** have no representation in the browser Notification API or in the freedesktop notification specification; those payload parts are dropped and reported through the Uno log. Browser action buttons are only rendered by `ServiceWorkerRegistration.showNotification()`, so they are reported as unsupported in the default document mode.
* **Linux** uses the `org.freedesktop.Notifications` D-Bus service. When no notification daemon is running, `AppNotificationManager.IsSupported()` is `false` and the remove operations fault instead of reporting a success that did not happen.
* **Web (WASM)** requires a secure context (`https://` or `localhost`) and user-granted notification permission. Scheduling (`ToastNotifier.AddToSchedule`) has no browser equivalent and is not emulated.
* **Windows** uses the Windows App SDK notification platform. For non-packaged Skia Win32 applications, the Windows App Runtime bootstrapper is initialized automatically; when it is unavailable, `IsSupported()` returns `false` and `Setting` returns `AppNotificationSetting.Unsupported`.
* **Scheduling** is implemented on Android, iOS, and macOS only. On Skia Win32, Linux, and WebAssembly, `ToastNotifier.AddToSchedule` throws `NotSupportedException`; an in-process timer is not substituted for durable platform scheduling. Native WinUI applications use the Windows SDK's implementation and are not subject to this Skia Win32 limitation.
* **tvOS** has no app notification backend; `IsSupported()` returns `false`.
* On every target, notification state is persisted so that ids, tags, groups, progress data and pending activations survive a process restart.
* **Windows progress updates** call the native `UpdateAsync` operation rather than showing another toast. Durable recovery distinguishes progress-only updates from content replacements.
* **History progress** retains the originally posted values and restores the transient sequence number as `1`, matching Windows App SDK. The latest update sequence is tracked separately for ordering and recovery. Older persisted schemas retain their available progress snapshot when upgraded.

## Source alignment

The notification objects, builder components, and activation-argument decoder follow the [Windows App SDK notification sources](https://github.com/microsoft/WindowsAppSDK/tree/6b178e79e59d28efb10ef5c8c68b051d2615c3e6/dev/AppNotifications). Platform registration, delivery, and durable state remain Uno adapters; the Windows backend delegates presentation and progress changes to Windows App SDK.

CLR collection and XML-safety adaptations preserve mutable raw input and escape it once when creating XML, rather than exposing the native builder's encoded storage. These adaptations also handle empty collections safely. Calling-preview and conferencing additions are outside the implemented contract versions.

## Using app notifications with Uno

### Requesting permission

`Register()` starts the platform permission flow where one exists (Android 13+, iOS, macOS, browsers). Android requires the manifest opt-in below. Call it from a user-initiated action on the UI thread. Registration returns before the permission dialog completes; it does not imply that permission was granted. Check `AppNotificationManager.Default.Setting` in a later user action before showing the first notification:

```csharp
var manager = AppNotificationManager.Default;
manager.NotificationInvoked += OnNotificationInvoked;
manager.Register();
```

In a later user action, after responding to the permission dialog:

```csharp
if (manager.Setting == AppNotificationSetting.Enabled)
{
    manager.Show(new AppNotificationBuilder()
        .AddText("Download complete")
        .AddArgument("action", "open")
        .BuildNotification());
}
```

### Android manifest opt-in

Uno does not add notification permissions to every Android application. Apps that use notifications must declare this permission in `Platforms/Android/AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
```

Place the element directly inside `<manifest>`, not inside `<application>`. On Android 13 and later, `Register()` requests this permission only when it is declared and not yet granted. A missing declaration yields `AppNotificationSetting.DisabledByManifest`; denial leaves notifications disabled. Registration and posting do not override the user's decision.

When a user-initiated flow needs to await the Android permission result before registering or posting, the Android head can use the same asynchronous permission helper as the samples app:

```csharp
if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu &&
    !await Windows.Extensions.PermissionsHelper.TryGetPermission(
        System.Threading.CancellationToken.None,
        "android.permission.POST_NOTIFICATIONS"))
{
    return; // The user did not grant permission.
}

AppNotificationManager.Default.Register();
```

Apps that use `ScheduledToastNotification` must additionally opt into recovery after device restart or app update:

```xml
<uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED" />
```

The notification recovery receiver is declared disabled and is enabled only while the permission is declared and durable schedules or pending scheduling operations exist. Adding a new schedule without this permission throws `InvalidOperationException` before saving it. Existing schedules remain visible and removable if an app update removes the permission, and pending cancellations can still recover; these operations do not grant permission to add new durable schedules. Without the permission, the recovery receiver stays disabled, so future reboot recovery is unavailable. Removing the last pending schedule also disables the receiver. Android uses inexact alarms, so delivery can be delayed by Doze and other system restrictions.

### Windows activation

Subscribe to `NotificationInvoked` before calling `Register()` during startup. The Skia Win32 backend forwards the startup app-notification arguments retained by Windows App SDK as well as subsequent native activation events. Startup arguments are consumed once per process, so unregistering and registering again does not replay the initial activation.

Startup decoding is limited to the notification platform's toast COM-launch contract. Ordinary, protocol, and push launches are left to their owning activation handlers, so registering app notifications does not initialize or decode push activation before the push manager is registered.

If reading the initial toast activation fails, registration rolls back only the foreground registration and handler created by that call, allowing a retry without deleting persistent registration or notification state.

The Skia Win32 host package supplies the notification and AppLifecycle projections and copies their native metadata to the app output. Applications do not need a custom assembly resolver or manual projection copies.

### Persistent notifications on WebAssembly

By default, WebAssembly uses document-scoped browser notifications, which disappear when the page is closed. Opt into service-worker-backed notifications during startup to keep notifications alive after the page is closed and to reopen or focus the application on activation:

```csharp
WinRTFeatureConfiguration.AppNotifications.UseServiceWorkerOnWebAssembly = true;
```

This flag must be set before `AppNotificationManager.Default` is first accessed. See [Feature flags](xref:Uno.Development.FeatureFlags) for details.

## See app notifications in action

The `AppNotificationManager` sample in the Uno Platform samples app (`Microsoft.Windows.AppNotifications` category) exercises registration, posting, progress updates, scheduling, activation, history, and removal on every target.
