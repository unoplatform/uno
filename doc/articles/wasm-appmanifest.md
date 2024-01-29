---
uid: Uno.Development.WasmAppManifest
---

# AppManifest for WebAssembly head project

The `[AppName].Wasm` project in your solution typically includes a manifest file containing settings for the WebAssembly application. This file is typically generated for you under `WasmScripts/AppManifest.js`.

## AppManifest properties

This app manifest file allows you to customize certain aspects of the WebAssembly application, including its loading screen. This loading UI is often referred to as the "splash screen" in documentation. See the WebAssembly [section](xref:Uno.Development.SplashScreen#5-webassembly) for more information, including how to customize using the properties in the app manifest.

## Add a missing manifest file

If you created an application without using the default Uno [templates](xref:Uno.GetStarted.dotnet-new), you may need to add the manifest file manually.

To do this, create a folder named `WasmScripts` in your `[AppName].Wasm` project, with a file containing the JavaScript code below
(e.g. `AppManifest.js`).

Set the manifest file's build action to `Embedded resource`, and edit the contents of this file to resemble the following:

```javascript
var UnoAppManifest = {
    splashScreenImage: "Assets/SplashScreen.scale-200.png",
    splashScreenColor: "transparent",
    displayName: "SplashScreenSample"
}
```

## See also

- [WebAssembly: Supported AppManifest properties](xref:Uno.Development.SplashScreen#5-webassembly)
- [Deep-dive: How Uno works on WebAssembly](xref:Uno.Contributing.Wasm#web-webassembly)
- [Get Started: Get the Uno Platform templates](xref:Uno.GetStarted)