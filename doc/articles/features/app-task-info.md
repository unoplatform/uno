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

- **Skia on Windows** maps `Running` to indeterminate taskbar progress, `Paused` and `NeedsAttention` to paused progress, and `Error` to error progress.
- **Android** creates one notification per task, with active tasks marked as ongoing. Buttons and free-form text input are mapped to notification actions. Android 13 or later requires the `POST_NOTIFICATIONS` permission.
- **iOS, tvOS, and Mac Catalyst** show the number of active, attention-required, and errored tasks as the app icon badge. The app must request badge authorization.
- **WebAssembly** shows the number of active, attention-required, and errored tasks through the browser Badging API. The browser and installation mode must support `navigator.setAppBadge`.
- **macOS** shows the number of active, attention-required, and errored tasks in the Dock badge.
- **Linux with X11** publishes and updates notifications through `org.freedesktop.Notifications`. The notification shows the task title, state, current step or result summary, and question. Buttons and text input are persisted but not surfaced as notification actions. A D-Bus session and notification service are required.

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

Two behaviors are not reproduced, because Windows has no defined result to reproduce:

- **`null` URI arguments.** `AppTaskInfo.Create` and the `AppTaskResultAsset` constructor dereference a `null` `Uri` and terminate the process with an access violation (`0xC0000005`) instead of returning an `HRESULT`. Uno raises `ArgumentException`, which is what the same argument produces on the API's other entry points.
- **Remote preview thumbnails.** `CreatePreviewThumbnail` accepts `ms-appx`, `ms-appdata` and `file` URIs but returns `E_POINTER` for `http`, `https` and custom schemes, which the C#/WinRT projection surfaces as `NullReferenceException`. Uno accepts any absolute URI: the Uno shell presenters do not consume the thumbnail, and `NullReferenceException` is not a contract an app can act on.

### Platforms without an implementation

`AppTaskInfo.Create` throws `PlatformNotSupportedException` on targets where Uno has no app-task implementation at all — currently only the `netstandard2.0` reference assembly, which has no local storage to persist tasks into. This is Uno's standard signal for an unimplemented target rather than a contract difference; the Windows documentation states that the APIs "will not have any effect" when the operating-system feature is missing, but that path could not be exercised on the validation machine because `AppTaskInfo.IsSupported()` reports `true` there. On every implemented target, always guard calls with `AppTaskInfo.IsSupported()`.

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

The sample is excluded from `SamplesApp.Windows`, because `Microsoft.Windows.SDK.NET.Ref` does not project the experimental `Windows.UI.Shell.Tasks` types yet. Use the native Windows API directly from a packaged WinUI app until that projection ships.
