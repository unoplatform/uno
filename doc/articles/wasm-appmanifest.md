---
uid: Uno.Development.WasmAppManifest
---

# AppManifest for WebAssembly head project

The `[AppName]` project in your solution typically includes a manifest file containing settings for the WebAssembly application. This file is typically generated for you under `Platforms/WebAssembly/WasmScripts/AppManifest.js`.

## AppManifest properties

This app manifest file allows you to customize certain aspects of the WebAssembly application, including its loading screen. This loading UI is often referred to as the "splash screen" in documentation. For more information, see [Splash Screen Customization](xref:Uno.Development.SplashScreen#webassembly).

## Add a missing manifest file

If you created an application without using the default Uno [templates](xref:Uno.GetStarted.dotnet-new), you may need to add the manifest file manually.

To do this, create a folder named `Platforms/WebAssembly/WasmScripts` in your `[AppName]` project, with a file containing the JavaScript code below
(e.g. `AppManifest.js`).

If the project is not using the [Uno.Sdk](xref:Uno.Features.Uno.Sdk), Set the manifest file's build action to `Embedded resource`, and edit the contents of this file to resemble the following:

```javascript
var UnoAppManifest = {
    splashScreenImage: "Assets/SplashScreen.scale-200.png",
    splashScreenColor: "transparent",
    displayName: "SplashScreenSample"
}
```

## See also

- [Splash Screen Customization: WebAssembly Configuration](xref:Uno.Development.SplashScreen#webassembly)
- [Deep-dive: How Uno works on WebAssembly](xref:Uno.Contributing.Wasm#web-webassembly)
- [Get Started: Get the Uno Platform templates](xref:Uno.GetStarted)
