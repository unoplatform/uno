---
uid: Uno.Features.AppTaskInfo
---

# App tasks

> [!TIP]
> This article covers Uno-specific information for `Windows.UI.Shell.Tasks`. For the complete API contract, see [Windows.UI.Shell.Tasks](https://learn.microsoft.com/uwp/api/windows.ui.shell.tasks).

The `Windows.UI.Shell.Tasks` namespace represents long-running app work in an operating-system shell surface. Uno Platform preserves the Windows SDK task model and persistence contract on every supported target, then maps it to the closest platform surface.

The API is experimental in Windows SDK 10.0.26100.7705. C# callers must suppress `CS8305` at each usage site until Microsoft removes the experimental designation.

## Supported features

| Feature | Windows | Android | iOS and Mac Catalyst | Web (WASM) | macOS | Linux (X11) | Win 7 (Skia) |
|---------|---------|---------|----------------------|------------|-------|--------------|--------------|
| Create, update, enumerate, and remove tasks | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Persistent task identity and state | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Sequence, preview, summary, and generated-asset content | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Deep links, buttons, questions, and text-input values readable from the app | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| Buttons and text input rendered in the shell surface | ✔ | ✔ | ✖ | ✖ | ✖ | ✖ | ✖ |
| Deep link opened from the shell surface | ✔ | ✔ | ✖ | ✖ | ✖ | ✖ | ✖ |
| Shell presentation | Native task UI; taskbar progress on Skia | Notifications | App icon badge | Badging API | Dock badge | Desktop notifications | Taskbar progress |
| Native Windows rich app-task UI | ✔, when the Windows feature is available | ✖ | ✖ | ✖ | ✖ | ✖ | ✖ |
| `HiddenByUser` shell feedback | Native Windows only | ✖ | ✖ | ✖ | ✖ | ✖ | ✖ |

Task records are stored below `ApplicationData.Current.LocalFolder` and survive app restarts and operating-system reboots. Call `FindAll` during app startup to restore the records and synchronize the current platform presenter.

## Platform behavior

Uno Platform uses the nearest available shell surface outside native Windows:

- **Skia on Windows** maps `Running` to indeterminate taskbar progress, `Paused` and `NeedsAttention` to paused progress, and `Error` to error progress. Failed taskbar initialization is retried no more than once every five seconds, so a missing shell does not trigger COM activation on every update.
- **Android** creates one notification per task, with active tasks marked as ongoing. Buttons and free-form text input are mapped to notification actions. Android 13 or later requires the `POST_NOTIFICATIONS` permission.
- **iOS, tvOS, and Mac Catalyst** show the number of active, attention-required, and errored tasks as the app icon badge. The app must request badge authorization.
- **WebAssembly** shows the number of active, attention-required, and errored tasks through the browser Badging API. The browser and installation mode must support `navigator.setAppBadge`.
- **macOS** shows the number of active, attention-required, and errored tasks in the Dock badge.
- **Linux with X11** publishes and updates notifications through `org.freedesktop.Notifications`. The notification shows the task title, state, current step or result summary, and question. Buttons and text input are persisted but not surfaced as notification actions. A reachable D-Bus session and a running notification service, or one that D-Bus can activate, are required. `IsSupported()` is initially false while the asynchronous probe is pending; availability is refreshed every five seconds. A changed daemon owner invalidates notification IDs and replays the latest task snapshot without requiring the app to mutate a task.

An explicit badge set through `BadgeUpdater` takes precedence over the automatic app-task count. Clearing that explicit badge reveals the current app-task count again.

These surfaces do not reproduce the complete Windows taskbar card. Uno still preserves all task content so the app can render a complete in-app task list and future platform presenters can consume the same data. `HiddenByUser` remains `false` on approximation-based presenters because those shell surfaces do not expose equivalent user-hide state.

## Using app tasks with Uno Platform

```csharp
#pragma warning disable CS8305

using Windows.UI.Shell.Tasks;

if (AppTaskInfo.IsSupported())
{
    var content = AppTaskContent.CreateSequenceOfSteps(
        ["Download metadata"],
        "Download package");
    content.AddButton("Open details", new Uri("my-app://tasks/details"));
    content.SetTextInput(
        "Add a note",
        "my-app://tasks/note?text={userTextInput}");

    var task = AppTaskInfo.Create(
        "Install update",
        "Version 2.0",
        new Uri("my-app://tasks/update"),
        new Uri("ms-appx:///Assets/Update.png"),
        content);

    task.UpdateState(AppTaskState.Completed);
    task.Remove();
}
```

The text-input URI template is free-form. When it contains `{userTextInput}`, platform surfaces that support inline replies (Android notifications) replace that token with the URL-escaped user input before launching the URI.

`AppTaskContent.MaxButtons` is `2`, matching the Windows implementation. Calling `AddButton` after reaching the limit throws `ArgumentException`, which is what the Windows `E_INVALIDARG` result projects to. `SetTextInput` can only be called once per content object; a second call throws `ArgumentException`.

### Windows contract parity

The managed implementation follows the behavior observed on Windows 11 (build 26200) by calling the `Windows.UI.Shell.Tasks` activation factories directly, because `Microsoft.Windows.SDK.NET.Ref` does not project these experimental types.

| Behavior | Windows result | Uno |
|----------|----------------|-----|
| `AppTaskContent.MaxButtons` | `2` | `2` |
| `AddButton` past the limit, with a relative URI, or with `null` | `E_INVALIDARG` | `ArgumentException` |
| `SetTextInput` called twice on the same content | `E_INVALIDARG` | `ArgumentException` |
| `CreatePreviewThumbnail` with a null or relative URI | `E_INVALIDARG` | `ArgumentException` |
| `CreatePreviewThumbnail` with HTTP/custom schemes, UNC paths, or non-canonical app URI authorities | `E_POINTER` | `NullReferenceException` with the same HRESULT |
| `SetTextInput` template contents, `SetQuestion(null)`, `AddButton(null, uri)` | Not validated | Not validated |
| `CreateSequenceOfSteps` with an empty `executingStep` | `E_INVALIDARG` | `ArgumentException` |
| `CreateSequenceOfSteps` with `null` steps or `null` entries | `S_OK`; entries become empty strings | Same |
| `CreateTextSummaryResult` with an empty string | `E_INVALIDARG` | `ArgumentException` |
| `CreateGeneratedAssetsResult` with `null` or an empty array | `E_INVALIDARG` | `ArgumentException` |
| `CreateGeneratedAssetsResult` with `null` entries, including an all-`null` array | `S_OK`; only the array itself is validated | Accepted; `null` entries are dropped |
| `AppTaskResultAsset` with a `null` name or context | `S_OK` | Accepted as empty strings |
| `Create` with an empty title or subtitle | `S_OK` | Accepted |
| `Create` with `content: null` | `S_OK`; `GetCompletedSteps`/`GetExecutingStep` then return `E_INVALIDARG` | Accepted; both accessors throw `ArgumentException` |
| `Update` with `content: null` | `E_INVALIDARG`, task unchanged | `ArgumentException`, task unchanged |
| `UpdateTitles` with an empty title | `E_INVALIDARG` | `ArgumentException` |
| `UpdateDeepLink` with a relative URI | `E_INVALIDARG` | `ArgumentException` |
| `UpdateState`/`Update` to `NeedsAttention` without a non-empty question | `E_INVALIDARG`, stored task unchanged | `ArgumentException`, task unchanged |
| `UpdateState`/`Update` with a value outside `AppTaskState` | `S_OK`; the handle keeps the value and the task is permanently dropped from `FindAll` | Same |
| `EndTime` | Set when the task enters `Completed` or `Error`, kept while that state is re-applied, cleared when the task leaves it, and re-stamped on another ending state | Same |
| `Id` | A braced GUID, for example `{2c7f5d61-6f7c-4f7b-9ee0-2f6b0e0b1a55}` | Same |
| `FindAll` ordering | Ascending start time | Same |
| Mutating an `AppTaskContent` after it was passed to `Create`/`Update` | No effect on the task | Same |

**Null task/asset URI arguments** have no defined Windows result to reproduce: `AppTaskInfo.Create`
and the `AppTaskResultAsset` constructor dereference a null `Uri` and terminate the process with an
access violation (`0xC0000005`) instead of returning an HRESULT. Uno raises `ArgumentException`
rather than reproducing a native process crash.

Preview thumbnails follow the native URI contract: canonical `ms-appx:///`, `ms-appdata:///`, and
local `file:///` URIs are accepted; unsupported schemes and authorities return the managed
projection of `E_POINTER`. The shell adapters preserve the thumbnail URI but do not render it.

### Platforms without an implementation

`AppTaskInfo.Create` throws `PlatformNotSupportedException` when no supported app-task presenter is
available. This includes the reference assembly, denied Android notification permission, and X11 sessions
without a reachable notification service that is running or can be activated. On every target, guard creation with
`AppTaskInfo.IsSupported()`. On X11, retry that capability query after the initial asynchronous
probe instead of treating the first false result as permanent.

### Restore tasks at startup

```csharp
#pragma warning disable CS8305

var existingTasks = AppTaskInfo.IsSupported()
    ? AppTaskInfo.FindAll()
    : [];
```

`Remove` is idempotent. Updating a handle after it has been removed updates that detached object but does not add the task back to `FindAll`.

## Platform setup

### Native Windows

Native Windows support is supplied by Windows and is rolling out independently of Uno Platform. The app must be packaged and declare the app-task provider extension:

```xml
<uap3:Extension Category="windows.appExtension">
  <uap3:AppExtension
    Name="com.microsoft.apptaskprovider"
    PublicFolder="Public"
    Id="MyApp.AppTaskProvider"
    DisplayName="My app task provider" />
</uap3:Extension>
```

Always call `AppTaskInfo.IsSupported()` because the Windows feature might not be enabled even when the SDK API is present.

### Android

Declare the notification permission:

```xml
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
```

On Android 13 or later, request that permission before calling `AppTaskInfo.Create`. `IsSupported` returns `false` until permission is granted.

### Apple platforms

Request badge authorization before creating tasks:

```csharp
UNUserNotificationCenter.Current.RequestAuthorization(
    UNAuthorizationOptions.Badge,
    (_, _) => { });
```

## See app tasks in action

Run SamplesApp on an Uno head — Skia Desktop, WebAssembly, Android, or iOS — and open **Windows.UI.Shell.Tasks > AppTaskInfo**. The sample exercises every task state and content factory, restores persisted tasks, updates deep links, and shows the persisted public task properties beside the platform shell approximation.

The sample is excluded from the SamplesApp WinAppSDK target, because `Microsoft.Windows.SDK.NET.Ref`
does not project the experimental `Windows.UI.Shell.Tasks` types yet. Native Windows callers need
the Windows activation-factory contract until that managed projection ships.
