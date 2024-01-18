---
uid: Uno.Development.WasmAppManifest
---

# AppManifest for WebAssembly head project

The `AppManifest.js` file contains settings for your WebAssembly application. It's normally located in the `[AppName].Wasm` project under the `WasmScripts` folder.

## AppManifest properties

The properties are :

* **splashScreenImage**: defines the image that will be centered on the window during the application's loading time.
* **splashScreenColor**: defines the background color of the splash screen.
* **displayName**: defines the default name of the application in the browser's window title.

## Add a missing manifest

If you created an application without using the default Uno templates, you may need to add the manifest file manually.

In your WASM head, create a folder named `WasmScripts`, with a file containing the JavaScript code below
(e.g. `AppManifest.js`) and the `Embedded resource` build action.

The manifest file should contain the following:

```javascript
var UnoAppManifest = {

    splashScreenImage: "Assets/AppSplashScreen.scale-200.png",
    splashScreenColor: "#3750D1",
    displayName: "My Sample App"

}
```
