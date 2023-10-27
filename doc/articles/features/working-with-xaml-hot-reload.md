---
uid: Uno.Features.XamlHotReload
---

# Hot Reload

The Uno Platform Hot Reload feature provides a way to modify the XAML and C# of your running application, in order to iterate faster on UI or code changes. This makes the inner developer loop faster.
## Features
- XAML Hot Reload for iOS, Catalyst, Android, WebAssembly, Skia (Gtk and WPF)
- Supported in Visual Studio 2022 (Windows) and VS Code (Linux, macOS, Windows, CodeSpaces and GitPod)
- Supports XAML files in the main project, in shared projects, and referenced projects
- Partial tree hot reload is supported, where modifying a `UserControl`` instantiated in multiple locations will reload it without reloading its parents.
- XAML Bindings Hot Reload
- Full x:Bind expressions Hot Reload
- AppResources.xaml Hot Reload
- Cross-platform Hot Reload is supported

Hot Reload features vary between platforms and IDE, you can see at later in this page the list of currently supported features.

## How to use Hot Reload

# [**Visual Studio 2022**](#tab/vswin)
- Setup your environment by following our [getting started guides](xref:Uno.GetStarted.vs2022).
- Start your application (with or without the debugger, depending on the supported features below)
- Make changes to your XAML or C# code, save your file then press the red flame icon in the toolbar

# [**Visual Studio Code**](#tab/vscode)
- Setup your environment by following our [getting started guide](xref:Uno.GetStarted.vscode)
- Start the application (with or without the debugger, depending on the supported features below)
- Wait a few seconds for the hot reload engine to become available (see our troubleshooting tips below)
- Make changes to your XAML or C# code, then save your file

***

## Supported features

# [**Skia**](#tab/skia)

Skia-based targets provide support for full XAML Hot Reload and C# Hot Reload. There are some restrictions that are listed below:

- The Visual Studio 2022 for Windows support is fully available, with and without running under the debugger
- The VS Code Uno Platform extension does not support Hot Reload when running with a debugger
- Adding new C# or XAML files to the project is not yet supported

# [**WebAssembly**](#tab/wasm)

WebAssembly is currently providing both full and partial Hot Reload support, depending on the IDE.

- In Visual Studio Code:
  - Both C# and XAML Hot Reload are fully supported
  - Adding new C# or XAML files to the project is not yet supported
- In Visual Studio for Windows:
  - If your app is using port 5000 in development, [this Visual Studio issue](https://developercommunity.visualstudio.com/t/BrowserLink-WebSocket-is-disconnecting-a/10500228) may cause problems, changing to another port in `Properties/launchSettings.json` (e.g. 5001) will work around it.
  - [`MetadataUpdateHandlers`](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.metadata.metadataupdatehandlerattribute?view=net-7.0) are invoked without the list of changed types, which means that some hot reload features may not be available.

# [**iOS, Android Catalyst**](#tab/mobile)

Mobile targets are currently using a limited version of XAML Hot Reload and do not support C# Hot Reload until [this dotnet runtime](https://github.com/dotnet/runtime/issues/93860) issue is fixed.

- In Visual Studio, the "Hot Reload on File Save" feature must be disabled to avoid crashing the app
- In both VS and VS Code, C# Hot Reload is not supported
- x:Bind hot reload is limited to simple expressions and events

# [**WinAppSDK**](#tab/winappsdk)

Hot Reload is supported by Visual Studio for WinAppSDK and provides support in unpackaged deployment mode.

***

## Troubleshooting

### Common issues
- The application logs file changes. You should see diagnostics messages in the app when a XAML file is reloaded.
- The file named `RemoteControlGenerator\RemoteControl.g.cs` in the analyzers node for your project contains the connection information, verify that the information host addresses and the port number.
- WinAppSDK on Windows specific issues
    - Grid Succinct syntax [is not supported](https://github.com/microsoft/microsoft-ui-xaml/issues/7043#issuecomment-1120061686)

### Visual Studio 2022
- The output window in VS has an output named `Uno Platform` in its drop down. Diagnostics messages from the VS integration appear there.
- When a file is reloaded, XAML parsing errors will appear in the application's logs, on device or in browser.
- If there are multiple versions of the Uno.WinUI Package present in the solution, the newest will be used, regardless of the started application
- The app does not update its XAML, because the port number in `RemoteControl.g.cs` is `0`.
    - Ensure you have the latest version of the Visual Studio extension installed.
    - Rebuild the app until the number is different than zero.

### VS Code
- The output window in Code has an output named "Uno Platform - Hot Reload" in its drop down. Diagnostics messages from the extension appear there.
- Depending on your machine's performance, the hot reload engine may take a few moments to initialize.
- Make sure that the selected project in the status bar is not the solution file, but rather the project platform you are debugging.