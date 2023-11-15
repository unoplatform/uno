---
uid: Uno.Features.WSDisplay
---

# Windows.System.Display APIs

> [!TIP]
> This article covers Uno-specific information for `Windows.System.Display` namespace. For a full description of the feature and instructions on using it, consult the UWP documentation: https://learn.microsoft.com/en-us/uwp/api/windows.system.display

* The `Windows.System.Display.DisplayRequest` class enables an application to request to keep the device's screen on.

## `DisplayRequest`

The `DisplayRequest` API is supported on all platforms.

In case of WASM, it is implemented using the [Screen Wake Lock API](https://w3c.github.io/screen-wake-lock/) which is not yet supported in many browsers. On unsupported browsers the API calls are ignored. In addition, because the JS API is asynchronous, it is recommended not to request and release the screen lock multiple times in quick succession (as the promise may not be completed yet).
