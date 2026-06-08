---
uid: Uno.Features.BrowserInputHelper
---

# Preventing Browser Keyboard Shortcuts on WebAssembly

When running an Uno Platform Skia WebAssembly app, certain browser keyboard shortcuts (such as <kbd>Ctrl</kbd>+<kbd>S</kbd>, <kbd>F5</kbd>, or <kbd>Ctrl</kbd>+<kbd>+</kbd>/<kbd>-</kbd>) can interfere with the application by triggering browser-native actions instead of being handled by the Uno input pipeline.

The `BrowserInputHelper` class provides APIs to control this behavior, giving developers fine-grained control over browser zoom and keyboard lock features.

## Default behavior

By default, the Uno Skia WebAssembly runtime routes keyboard events through the Uno input pipeline. When a key event is marked as `Handled` (either by your app or by Uno's built-in handling), `preventDefault()` is called on the underlying DOM event, preventing the corresponding browser shortcut from firing. This means:

- Keys like <kbd>F5</kbd> (refresh), <kbd>Ctrl</kbd>+<kbd>S</kbd> (save), and <kbd>Ctrl</kbd>+<kbd>P</kbd> (print) can be intercepted by your app when you handle those key events.
- Standard text input, clipboard operations (<kbd>Ctrl</kbd>+<kbd>C</kbd>/<kbd>V</kbd>/<kbd>X</kbd>), and other in-app keyboard interactions continue to work normally.
- Browser zoom via <kbd>Ctrl</kbd>+mouse wheel is **allowed** by default.

For most in-app shortcuts, no additional configuration is required beyond handling the relevant key events. For OS- or browser-reserved shortcuts (such as tab management or focusing the address bar), you may need the Keyboard Lock API described below.

## Prerequisites

The `BrowserInputHelper` class is WebAssembly Skia-specific. It resides in the `Uno.UI.Runtime.Skia` namespace and is only available in the `Uno.WinUI.Runtime.Skia.WebAssembly.Browser` package.

When using this API, guard your code with platform checks:

```csharp
#if HAS_UNO_SKIA
using Uno.UI.Runtime.Skia;
#endif
```

## Disabling browser zoom

By default, <kbd>Ctrl</kbd>+mouse wheel triggers the browser's built-in page zoom. You can disable this so your app receives the wheel events instead (for example, to implement custom zoom-to-cursor behavior in a canvas or map control):

```csharp
#if HAS_UNO_SKIA
// Disable browser zoom - Ctrl+wheel events are delivered to the Uno app instead
BrowserInputHelper.IsBrowserZoomEnabled = false;
#endif
```

To re-enable browser zoom:

```csharp
#if HAS_UNO_SKIA
BrowserInputHelper.IsBrowserZoomEnabled = true;
#endif
```

## Keyboard Lock API

Some system-level keys (like <kbd>Escape</kbd>, <kbd>Alt</kbd>+<kbd>Tab</kbd>, or <kbd>Meta</kbd>) are intercepted by the browser or OS before they reach the web page. The [Keyboard Lock API](https://developer.mozilla.org/en-US/docs/Web/API/Keyboard/lock) allows you to capture even these keys.

> [!IMPORTANT]
> The Keyboard Lock API requires **HTTPS** and is only supported in **Chromium-based browsers** (Chrome, Edge). It does not work in Firefox or Safari. The app must also be in **fullscreen mode** for the lock to take effect.

### Checking browser support

Before calling `LockKeysAsync`, you can check whether the current browser supports the Keyboard Lock API:

```csharp
#if HAS_UNO_SKIA
if (BrowserInputHelper.IsKeyboardLockSupported)
{
    await BrowserInputHelper.LockKeysAsync("Escape", "F11");
}
else
{
    // Fallback: Keyboard Lock API not available in this browser
}
#endif
```

### Locking specific keys

To lock specific keys so they are delivered to your app instead of the browser or OS:

```csharp
#if HAS_UNO_SKIA
// Lock Escape and F11 so they reach the Uno app
await BrowserInputHelper.LockKeysAsync("Escape", "F11");
#endif
```

Key codes use the [`KeyboardEvent.code`](https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/code) format (e.g., `"KeyW"`, `"F5"`, `"Escape"`, `"MetaLeft"`).

### Locking all keys

To lock all keys:

```csharp
#if HAS_UNO_SKIA
// Lock all capturable keys
await BrowserInputHelper.LockKeysAsync();
#endif
```

### Unlocking keys

To release all locked keys and restore default browser key handling:

```csharp
#if HAS_UNO_SKIA
BrowserInputHelper.UnlockKeys();
#endif
```

## API reference

### `BrowserInputHelper.IsBrowserZoomEnabled`

| | |
|-|-|
| **Type** | `bool` |
| **Default** | `true` |
| **Description** | Gets or sets whether the browser's built-in <kbd>Ctrl</kbd>+mouse wheel zoom is enabled. When `false`, the Skia pointer handler calls `preventDefault()` on <kbd>Ctrl</kbd>+wheel events, blocking browser zoom while still delivering the event to the Uno pointer pipeline. |

### `BrowserInputHelper.IsKeyboardLockSupported`

| | |
|-|-|
| **Type** | `bool` |
| **Description** | Gets a value indicating whether the browser supports the [Keyboard Lock API](https://developer.mozilla.org/en-US/docs/Web/API/Keyboard/lock) (`navigator.keyboard`). Returns `true` on Chromium-based browsers over HTTPS, `false` otherwise. Use this to conditionally show/hide UI or choose fallback behavior. |

### `BrowserInputHelper.LockKeysAsync(params string[] keyCodes)`

| | |
|-|-|
| **Returns** | `Task` |
| **Description** | Locks the specified keyboard keys using the browser [Keyboard Lock API](https://developer.mozilla.org/en-US/docs/Web/API/Keyboard/lock). When locked, these keys are delivered to the app instead of being intercepted by the browser or OS. Pass no arguments to lock all keys. Requires HTTPS and a Chromium-based browser. |

### `BrowserInputHelper.UnlockKeys()`

| | |
|-|-|
| **Returns** | `void` |
| **Description** | Unlocks all previously locked keys, restoring default browser key handling. |

## Browser compatibility

| Feature | Chrome / Edge | Firefox | Safari |
|---------|--------------|---------|--------|
| Keyboard shortcut prevention (`preventDefault`) | Yes | Yes | Yes |
| `IsBrowserZoomEnabled` | Yes | Yes | Yes |
| `LockKeysAsync` / `UnlockKeys` (Keyboard Lock API) | Yes | No | No |
