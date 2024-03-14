---
uid: Uno.Development.WasmAppManifest
---

# AppManifest for WebAssembly head project

The `[AppName].Wasm` project in your solution typically includes a manifest file containing settings for the WebAssembly application. This file is typically generated for you under `WasmScripts/AppManifest.js`.

## AppManifest properties

<<<<<<< HEAD
<<<<<<< HEAD
The properties are :

* **splashScreenImage**: defines the image that will be centered on the window during the application's loading time.
* **splashScreenColor**: defines the background color of the splash screen.
* **displayName**: defines the default name of the application in the browser's window title.
=======
This app manifest file allows you to customize certain aspects of the WebAssembly application, including its loading screen. This loading UI is often referred to as the "splash screen" in documentation.
>>>>>>> 20fb76b60b (docs: Align wasm manifest resources)

See [this](xref:Uno.Development.SplashScreen#5-webassembly) section for more information about the supported properties.
=======
This app manifest file allows you to customize certain aspects of the WebAssembly application, including its loading screen. This loading UI is often referred to as the "splash screen" in documentation. For more information, see [How to manually add a splash screen](xref:Uno.Development.SplashScreen#5-webassembly).
>>>>>>> 98b7c36e94 (docs: Apply suggestions from code review)

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
