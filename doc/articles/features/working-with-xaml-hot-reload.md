# XAML Hot Reload

The Uno Platform Hot Reload feature provides a way to modify the XAML displayed in a running application, in order to iterate faster on UI changes and Data Binding updates. This makes the inner developer loop faster.

## Features
- XAML Hot Reload for iOS, Android, and WebAssembly
- Supports XAML files in the main project, in shared projects, and referenced projects
- Partial hot reload is supported, whereby modifying a UserControl instantiated in multiple locations will reload it without reloading its parents.
- XAML Bindings Hot Reload
- Cross-platform Hot Reload is supported

## How to use the XAML Hot Reload
- Install the [Uno Platform Add-in](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin) from the Visual Studio Marketplace.
- Create a sample application using the **Uno Cross Platform App** template
- Build an Uno application head (iOS, Android or WebAssembly), start it (with or without the debugger)
- Change a XAML file from VS and the app should update

> Note: You can also get pre-released version of the Visual Studio Extension from the [master CI builds artifacts](https://dev.azure.com/uno-platform/Uno%20Platform/_build?definitionId=5&_a=summary). 

If you're using the XAML Hot Reload feature in an existing application, you'll need to add the following lines to your project:
```xml
<ItemGroup>
    <PackageReference Include="Uno.UI.RemoteControl" Version="2.0.512" Condition="'$(Configuration)'=='Debug'" />
</ItemGroup>
```
Make sure that the version number is the same as the `Uno.UI` package.

## Troubleshooting
- The application logs file reloads, so you should see diagnostics messages when a XAML file is reloaded.
- The output window in VS has an output named "Uno Platform" in its drop down. Diagnostics messages from the VS integration appear there.
- The file named `obj\Debug\XXX\g\RemoteControlGenerator\RemoteControl.g.cs` contains the connection information, verify that the information makes sense, particularly the port number.
- When a file is reloaded, XAML parsing errors will appear in the application's logs, on device or in browser.

## Known issues

- Events specified in the XAML are not yet supported for hot reload
- If there are multiple versions of the Uno.UI Package present in the solution, the newest will be used, regardless of the started application
- Changing the package version may confuse VS if a Hot Reload session has already been started
    - Resolution: Restart VS and rebuild the app
- The reload server may start twice (The VS **Uno Platform** output window shows two "Starting server" messages)
    - Resolution: Restart visual studio, rebuild the app.
- The app does not update its XAML, because the port number in `RemoteControl.g.cs` is `0`.
    - Ensure you have the right version of the _VSIX_ installed.
    - Resolution: Rebuild the app until the number is different than zero.
- Updating a standalone `ResourceDictionary` XAML file does not work
    - Resolution: None at this time

## XAML Hot Reload inside of the Uno Solution

- Select the project named `Uno.RemoteControl.Host`, then select **Run without debugging**
- Select a sample application (iOS, Android, or WebAssembly) and run it
- Update your XAML files and their content updated in the Samples app

This scenario is designed for contributors to the Uno platform, to test changes to the XAML directly in the running applications.

## Debugging the Visual Studio extension

1. Select a version of Uno.UI that is installed in your nuget cache, and set that version in the `crosstargeting_override.props` file. See [this document](../uno-development/debugging-uno-ui.md) for more information.
1. Open the Visual Studio solution using one of the hot-reload solution filters, we'll use the `Uno.UI-Wasm-hotreload.slnf` for this example.
1. Build the `SamplesApp.Wasm` project
1. Set the `UnoSolutiontemplate.VSIX` project as startup
1. Open the properties for this project and:
    - Set the startup executable to be your Visual Studio `devenv.exe` binary path.
    - Set the command line arguments to `/rootsuffix Exp`
1. Run the VSIX with or without the debugger
1. Create a Uno Cross Platform app using the template
1. Set the nuget package version for `Uno.UI` and `Uno.UI.RemoteControl` to that set previously in the `crosstargeting_override.props` file
1. Launch and debug the application you just created
