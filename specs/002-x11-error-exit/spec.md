# Spec 002: X11 Error Handler and BadWindow Race Fixes

> **Status**: Implemented
> **Author**: Jay Zuo
> **Date**  : 2026-02-26

### Reading Convention

This spec distinguishes between **current behavior** (what the code does today) and **target behavior** (what each fix delivers). Sections describing current behavior reference source code locations; sections describing target behavior describe the change introduced. **The code is the source of truth for current behavior.** Where this spec describes current behavior, it must match the code; discrepancies are spec bugs.

---

## Executive Summary

### The Problem

When a secondary Uno Platform application is loaded into the same process via ALC (AssemblyLoadContext), the X11 backend can terminate the **entire host process** silently with exit code 0. Two independent causes contribute:

1. **Default X11 error handler calls `exit()`**: Any X11 protocol error — even a benign `BadWindow` from a transient race — invokes the Xlib default error handler, which prints to stderr and exits the process. Uno Platform's X11 backend never installs a replacement.

2. **Post-close races produce BadWindow errors**: The secondary ALC app creates real X11 native windows during `X11XamlRootHost.Initialize()`. `InitializeAlcWindowMode()` then closes those windows. Several paths — a `DispatcherTimer`, background tasks, and event subscriptions — can execute X11 calls on the already-destroyed windows, generating the `BadWindow` protocol errors that then trigger the default `exit()`.

Together, these causes produce a silent process death that is extremely hard to diagnose: no .NET exception, no crash dump, no clear error message.

### What We're Changing

**1. Custom X11 error handler** (fail-safe, installed at process start)
Register a `XSetErrorHandler` callback in `X11ApplicationHost`'s static constructor. All X11 protocol errors are logged at Warning level and suppressed — the process continues running. This is the same approach used by GTK (`gdk_x_error()`), Qt (`qt_x_error()`), and SDL.

**2. BadWindow race guards** (root-cause hardening)
Add `Closed.IsCompleted` guards to every X11 operation in `X11WindowWrapper` and `X11XamlRootHost` that can be invoked after `SynchronizedShutDown` destroys the windows. This eliminates the race at its source rather than relying solely on the error handler to absorb it.

### Why This Approach

- **Layered defense**: The error handler is the safety net; the guards are the root-cause fix. Either alone is insufficient — the handler without the guards still produces spurious warnings in steady state; the guards without the handler leave a latent process-kill risk for unforeseen races.
- **Minimal diff**: Guards are one-line early returns. No changes to initialization order, window creation sequence, or the ALC window lifecycle protocol.
- **Precedent**: Every mature X11 toolkit installs a custom error handler. This aligns Uno with established practice.
- **Zero impact on non-ALC scenarios**: Guards check `Closed.IsCompleted`, which is false for the entire lifetime of a normal (non-ALC) window. No behavioral change for existing apps.

### Expected Gains

| Symptom | Before | After |
|---------|--------|-------|
| Secondary ALC app load exits process | Yes — silent exit code 0 | No — process continues |
| BadWindow errors logged | No — `exit()` fires first | Yes — Warning level with full context |
| `UpdatePositionAndSize` after window destroy | Races with `SynchronizedShutDown` | Suppressed by `Closed.IsCompleted` guard |
| `SetWindowIcon` background task after close | Accesses destroyed window | Suppressed by guard |
| TitleBar event handlers after close | Call `XGetWindowProperty` on destroyed window | Suppressed by guard |

### Scope

- **Fix 1**: Install custom X11 error handler — `X11ApplicationHost.cs`
- **Fix 2**: Guard `Move()`, `Resize()`, `UpdatePositionAndSize()` — `X11WindowWrapper.cs`
- **Fix 3**: Guard `SetWindowIcon()`, `ExtendContentIntoTitleBar()`, `UpdateWindowPropertiesFromCoreApplication()` — `X11XamlRootHost.cs`

### Key Constraints

- Error handler must be installed before any `XOpenDisplay` call — static constructor is the correct location.
- The static delegate for the native callback must be held in a static field to prevent GC collection.
- Guards must check `Closed.IsCompleted` rather than a separate boolean to avoid introducing a new synchronized state.
- No changes to `SynchronizedShutDown` logic, window creation sequence, or cross-platform code.

---

## 1. Problem Statement

### 1.1 Silent Process Death

When a secondary Uno Platform application loads via ALC (see spec `000-alc-secondary-app-support.md`), `X11XamlRootHost.Initialize()` creates two real X11 native windows and starts two event loop threads. Shortly after, `InitializeAlcWindowMode()` closes those windows — they are never shown and exist only for a few hundred milliseconds.

During teardown, X11 protocol errors (`BadWindow`) are generated. Because Uno Platform's X11 backend never calls `XSetErrorHandler()`, the Xlib default handler fires: it prints to stderr and calls `exit(0)`. The host process dies silently.

```
ErrorCode: 3 (BadWindow), RequestCode: 15 (X_QueryTree),       ResourceId: 0xC00002, Serial: 80
ErrorCode: 3 (BadWindow), RequestCode: 15 (X_QueryTree),       ResourceId: 0x0,      Serial: 81
ErrorCode: 3 (BadWindow), RequestCode: 12 (X_ConfigureWindow), ResourceId: 0x0,      Serial: 82
ErrorCode: 3 (BadWindow), RequestCode: 20 (X_GetProperty),     ResourceId: 0xC00002, Serial: 86
```

All four are `BadWindow` (error code 3): the referenced window ID has already been destroyed. These are non-fatal protocol errors; the danger is `exit()`, not the errors themselves.

### 1.2 Reproduction

1. Build and run any Uno Platform Skia.X11 desktop app as the host.
2. Set `WindowHelper.ContentHostOverride` and load a secondary Uno app binary into the process via ALC (per `000-alc-secondary-app-support.md`).
3. Without the fix: the process silently terminates with exit code 0 during the inner app's X11 initialization.
4. With the fix: the errors are logged at Warning level and the app continues running.

---

## 2. Root Cause Analysis

### 2.1 Missing Error Handler

`Uno.UI.Runtime.Skia.X11` never calls `XSetErrorHandler()`. The X11 default error handler (`_XDefaultIOError`) prints to stderr and calls `exit()`. This is documented Xlib behavior; every production X11 toolkit replaces it.

**Code location**: `src/Uno.UI.Runtime.Skia.X11/Hosting/X11ApplicationHost.cs` — the static constructor performs early X11 initialization but omits `XSetErrorHandler`.

The P/Invoke binding for `XSetErrorHandler` already exists:

```csharp
// src/Uno.UI.Runtime.Skia.X11/X11_Bindings/x11bindings_XLib.cs
[LibraryImport(libX11)]
public static partial IntPtr XSetErrorHandler(XErrorHandler error_handler);
```

The delegate type (`XErrorHandler`) and struct (`XErrorEvent`) are also already defined in `x11bindings_X11Structs.cs`.

### 2.2 BadWindow Race: `_configureTimer`

`X11XamlRootHost` holds a `DispatcherTimer` (`_configureTimer`) that calls `_configureCallback()` = `X11WindowWrapper.UpdatePositionAndSize()`. The timer is started by `RaiseConfigureCallback()` when `ConfigureNotify` events arrive too quickly for immediate dispatch.

The timer is **never stopped on window close**. After `SynchronizedShutDown` destroys both X11 windows, the timer fires on the Uno dispatcher, and `UpdatePositionAndSize()` calls `XQueryTree` and `XResizeWindow` on the destroyed window IDs.

Error chain from a single `UpdatePositionAndSize()` invocation on destroyed windows:

| Call | X11 Request | Serial | Result |
|------|-------------|--------|--------|
| `XQueryTree(display, 0xC00002_destroyed)` | `X_QueryTree` | 80 | BadWindow; `parent=0`, `root=0` |
| `XQueryTree(display, parent=0)` | `X_QueryTree` | 81 | BadWindow |
| `XResizeWindow(display, windowToResize=0)` | `X_ConfigureWindow` | 82 | BadWindow |

This matches serials 80–82 exactly.

### 2.3 BadWindow Race: Event Subscriptions

`X11XamlRootHost` subscribes two event handlers in its constructor:

```csharp
CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged += UpdateWindowPropertiesFromCoreApplication;
winUIWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBarChanged += ExtendContentIntoTitleBar;
```

These are unsubscribed in the `Closed.ContinueWith` continuation. However, `Closed.ContinueWith` runs on the thread pool and may execute after `SynchronizedShutDown` has already destroyed the windows. If either event fires in that window, `ExtendContentIntoTitleBar()` calls `X11Helper.SetMotifWMDecorations()` → `XGetWindowProperty()` on the destroyed window — matching serial 86 (`X_GetProperty` on `0xC00002`).

### 2.4 BadWindow Race: Background Tasks

`UpdateWindowPropertiesFromPackage()` spawns `Task.Run(SetWindowIcon)`, which calls `XChangeProperty` on `RootX11Window.Window`. This task may complete after `SynchronizedShutDown` destroys the window.

---

## 3. Implementation

### 3.1 Fix 1 — Custom X11 Error Handler

**File**: `src/Uno.UI.Runtime.Skia.X11/Hosting/X11ApplicationHost.cs`

Add a static `XErrorHandler` delegate field and install it immediately after `XInitThreads()` in the static constructor:

```csharp
// Must be a static field to prevent GC collection while the native callback is active.
private static readonly XErrorHandler s_x11ErrorHandler = OnX11Error;

private static int OnX11Error(IntPtr display, ref XErrorEvent error_event)
{
    if (typeof(X11ApplicationHost).Log().IsEnabled(LogLevel.Warning))
    {
        typeof(X11ApplicationHost).Log().LogWarning(
            $"X11 protocol error — " +
            $"ErrorCode: {error_event.error_code}, " +
            $"RequestCode: {error_event.request_code}, " +
            $"MinorCode: {error_event.minor_code}, " +
            $"ResourceId: 0x{error_event.resourceid:X}, " +
            $"Serial: {error_event.serial}");
    }

    return 0;
}

static X11ApplicationHost()
{
    _ = X11Helper.XInitThreads();

    // Install a custom error handler so X11 protocol errors (e.g. BadWindow from a secondary
    // app loaded via ALC) are logged and ignored instead of calling exit().
    XLib.XSetErrorHandler(s_x11ErrorHandler);

    // ... rest of static constructor unchanged
}
```

No new P/Invoke required. `XErrorHandler`, `XErrorEvent`, and `XSetErrorHandler` are already defined in the existing bindings.

### 3.2 Fix 2 — Guards in `X11WindowWrapper`

**File**: `src/Uno.UI.Runtime.Skia.X11/UI/Xaml/Window/X11WindowWrapper.cs`

Add `if (_host.Closed.IsCompleted) return;` at the start of every method that performs X11 operations on the host's windows:

- `Move(PointInt32 position)`
- `Resize(SizeInt32 size)`
- `UpdatePositionAndSize()`

The guard is placed **before** acquiring the X display lock to avoid a TOCTOU scenario where the lock is acquired just before `SynchronizedShutDown` destroys the windows.

### 3.3 Fix 3 — Guards in `X11XamlRootHost`

**File**: `src/Uno.UI.Runtime.Skia.X11/Hosting/X11XamlRootHost.cs`

Add `if (Closed.IsCompleted) return;` at the start of:

- `SetWindowIcon()` — background task that calls `XChangeProperty`
- `ExtendContentIntoTitleBar(bool extend)` — calls `SetMotifWMDecorations` → `XGetWindowProperty`
- `UpdateWindowPropertiesFromCoreApplication()` — calls `ExtendContentIntoTitleBar`

---

## 4. Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Log **all** X11 errors, not just `BadWindow` | Other error codes can arise. The danger is `exit()`, not the specific error code. |
| Static delegate field for error handler | Prevents GC collection while the native callback is active — required pattern for native callbacks. |
| Install error handler in static constructor | Ensures the handler is in place before any X11 window operations, regardless of how the host is initialized. |
| Warning level, not Error | `BadWindow` errors in the ALC scenario are expected and non-fatal. Logging at Error would alarm users of correctly functioning systems. |
| `Closed.IsCompleted` guard, not a separate flag | Reuses the existing close-signaling mechanism; no new synchronized state to manage. |
| Guard before acquiring the display lock | Avoids a narrow TOCTOU window where the lock acquisition races with `SynchronizedShutDown`. |

---

## 5. Validation

### 5.1 A/B Test Results (StudioLive host app)

| Metric | Without fixes | With fixes |
|--------|--------------|------------|
| X11 errors caused process exit | Yes (4 errors → `exit()`) | No — 4 warnings logged, process continues |
| App survived past X11 errors | No — log stops mid-operation | Yes — continued ~1800 more log lines |
| Secondary ALC content loaded and rendered | No | Yes |

### 5.2 Compile Validation

Both changed files (`X11ApplicationHost.cs`, `X11WindowWrapper.cs`, `X11XamlRootHost.cs`) compile without errors in the `Uno.UI.Runtime.Skia.X11` project. Pre-existing build errors in `LinuxSystemThemeHelper.cs` and picker extensions are unrelated (missing DBus package in isolated build environment).

### 5.3 Runtime Validation

Runtime validation requires a Linux/X11 environment with a host app configured to load a secondary ALC app. The test path is: start host → set `WindowHelper.ContentHostOverride` → load secondary ALC app binary → verify the process does not exit and secondary content renders.

---

## 6. References

- [Xlib error handling documentation](https://www.x.org/releases/current/doc/libX11/libX11/libX11.html#Using_the_Default_Error_Handlers)
- GTK custom handler: `gdk_x_error()` in `gdk/x11/gdkmain-x11.c`
- Qt custom handler: `qt_x_error()` in `qtbase/src/plugins/platforms/xcb/qxcbconnection.cpp`
- Spec `000-alc-secondary-app-support.md` — ALC secondary app architecture
- `src/Uno.UI.Runtime.Skia.X11/Hosting/X11ApplicationHost.cs` — error handler install point
- `src/Uno.UI.Runtime.Skia.X11/UI/Xaml/Window/X11WindowWrapper.cs` — `Move`, `Resize`, `UpdatePositionAndSize` guards
- `src/Uno.UI.Runtime.Skia.X11/Hosting/X11XamlRootHost.cs` — `SetWindowIcon`, `ExtendContentIntoTitleBar`, `UpdateWindowPropertiesFromCoreApplication` guards
