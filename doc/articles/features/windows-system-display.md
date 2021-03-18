# Uno Support for Windows.System.Display APIs

## `DisplayRequest`

The `DisplayRequest` API is supported on all platforms.

In case of WASM, it is implemented using the [Screen Wake Lock API](https://w3c.github.io/screen-wake-lock/) which is not yet supported in many browsers. On unsupported browsers the API calls are ignored. In addition, because the JS API is asynchronous, it is recommended not to request and release the screen lock multiple times in quick succession (as the promise may not be completed yet).