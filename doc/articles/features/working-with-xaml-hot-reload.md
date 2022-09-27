# XAML Hot Reload

The Uno Platform Hot Reload feature provides a way to modify the XAML displayed in a running application, in order to iterate faster on UI changes and Data Binding updates. This makes the inner developer loop faster.

## Features
- XAML Hot Reload for iOS, Android, WebAssembly, Skia (Gtk, WebAssembly)
- Supported in Visual Studio 2019, 2022 (Windows) and VS Code (Linux, macOS, Windows, CodeSpaces and GitPod)
- Supports XAML files in the main project, in shared projects, and referenced projects
- Partial hot reload is supported, where modifying a UserControl instantiated in multiple locations will reload it without reloading its parents.
- XAML Bindings Hot Reload
- x:Bind to events
- Cross-platform Hot Reload is supported

## How to use the XAML Hot Reload

### Visual Studio 2022
- Install the [Uno Platform Add-in](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin) from the Visual Studio Marketplace.
- Create a sample application using the **Uno Platform App** template
- Build an Uno application head (iOS, Android or WebAssembly), start it (with or without the debugger)
- Change a XAML file from VS and the app should update

### Visual Studio Code
- Follow the [getting started guide](../get-started-vscode.md)
- Start the application (WebAssembly or Skia)
- Make modifications to XAML files and save the file

## Troubleshooting

### Common issues
- The application logs file changes. You should see diagnostics messages in the app when a XAML file is reloaded.
- The file named `obj\Debug\XXX\g\RemoteControlGenerator\RemoteControl.g.cs` (Xamarin iOS/Android) or the `RemoteControlGenerator\RemoteControl.g.cs` node (Wasm, `net6` or Skia) in the Analyzers node in your project contains the connection information, verify that the information makes sense, particularly the port number.
- WebAssembly: `Hot Reload fails to start with Mixed Content: The page at XXX was loaded over HTTPS, but attempted to connect to the insecure WebSocket endpoint`. This issue is caused by Visual Studio 2022 enforcing https connections for locally served apps. You can work around this by either:
    - Removing the https endpoint in the `Properties/launchSettings.json` file
    - Unchecking the `Use SSL` option in the project's Debug launch profiles (in VS 2022 17.0 or earlier)
    - Removing the https App URL in the project's Debug launch profiles (in VS 2022 17.1 or later)
    - Selecting the project name instead of IISExpress in the toolbar debug icon drop down list

### Visual Studio 2019/2022
- The output window in VS has an output named "Uno Platform" in its drop down. Diagnostics messages from the VS integration appear there.
- Android
- When a file is reloaded, XAML parsing errors will appear in the application's logs, on device or in browser.
- If there are multiple versions of the Uno.UI Package present in the solution, the newest will be used, regardless of the started application
- The reload server may start twice (The VS **Uno Platform** output window shows two "Starting server" messages)
    - Resolution: Restart visual studio, rebuild the app.
- The app does not update its XAML, because the port number in `RemoteControl.g.cs` is `0`.
    - Ensure you have the right version of the _VSIX_ installed.
    - Resolution: Rebuild the app until the number is different than zero.

### VS Code
- The output window in Code has an output named "Uno Platform - Hot Reload" in its drop down. Diagnostics messages from the extension appear there.
- Make sure that the selected project in the status bar is not the solution file, but rather the project platform you are debugging.

## XAML Hot Reload inside of the Uno Solution

- Select the project named `Uno.RemoteControl.Host`, then select **Run without debugging**
- Select a sample application (iOS, Android, or WebAssembly) and run it
- Update your XAML files and their content updated in the Samples app

This scenario is designed for contributors to the Uno platform, to test changes to the XAML directly in the running applications.

### Disabling XAML Hot Reload

If you want to disable Uno's XAML Hot Reload support for some reason, you can do so by adding the following code to your `App` constructor:
```csharp
#if HAS_UNO
        // This disables hot reload during debugging.
        Uno.UI.FeatureConfiguration.Xaml.ForceHotReloadDisabled = true;
#endif
```

## Debugging the Visual Studio extension

1. Select a version of Uno.UI that is installed in your nuget cache, and set that version in the `crosstargeting_override.props` file. See [this document](../uno-development/debugging-uno-ui.md) for more information.
1. Open the Visual Studio solution using one of the hot-reload solution filters, we'll use the `Uno.UI-Wasm-hotreload-vsix-only.slnf` for this example.
1. Build the `SamplesApp.Wasm` project
1. Set the `UnoSolutiontemplate.VSIX.2019` or `UnoSolutiontemplate.VSIX.2022` project as startup.
1. Open the properties for this project and:
    - Set the startup executable to be your Visual Studio `devenv.exe` binary path.
    - Set the command line arguments to `/rootsuffix Exp`
1. Run the VSIX with or without the debugger
1. Create a Uno Cross Platform app using the template
1. Set the nuget package version for `Uno.UI` (or `Uno.WinUI`) and `Uno.UI.RemoteControl` (or `Uno.WinUI.RemoteControl`) to that set previously in the `crosstargeting_override.props` file
1. Launch and debug the application you just created
