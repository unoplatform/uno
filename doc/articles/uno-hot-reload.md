# Using the Uno Platform XAML Hot Reload

How to use the XAML Hot Reload:
- Get the [XAML Hot Reload Preview Release](https://github.com/unoplatform/uno/releases) on GitHub
- In the release download:
    - `UnoPlatform-XXXX.vsix`
    - `Uno.UI.1.XX.nupkg`
- Create a folder on your disk (e.g. `C:\temp\nuget-local`) and place both files there
- Make sure all your VS instances are closed, install the VSIX file
- Open Visual Studio, in **Tools/Options** go to the **Nuget Package Manager**, then **Package Manager**
- Click the **Green Plus**, set the **Name** to `Local feed`, the source to `C:\temp\nuget-local` or your own path)
- Click the **Update** button
- Create a sample application using the **Uno Cross Platform App** template
- The Uno.UI package should already be at the version you just downloaded, but you're upgrading the package, make sure you're using the latest you've downloaded.
- Build an application head, start it (with or without the debugger)
- Change a XAML file from VS and the app should update.
- Make sure to add `Newtonsoft.Json` package to all heads, and `Uno.Wasm.WebSockets` to the Wasm head.

## Troubleshooting
- The application logs file reloads, so you should see diagnostics messages when a XAML file is reloaded.
- The output window in VS has an output named "Uno Platform" in its drop down. Diagnostics messages from the VS integration appear there.
- The file named `obj\Debug\XXX\g\RemoteControlGenerator\HotReload.g.cs` contains the connection informations, verify that the information makes sense, particularly the port number.

## Known issues

- Changing the package version may confuse VS if a Hot Reload session has already been started
    - Resolution: Restart VS and rebuild the app
- The reload server may start twice (The VS **Uno Platform** output window shows two "Starting server" messages)
    - Resolution: Restart visual studio, rebuild the app.
- The app does not update its XAML, because the port number in `HotReload.g.cs` is `0`.
    - Resolution: Rebuild the app until the number is different than zero.
- Updating a standalone `ResourceDictionary` XAML file does not work
    - Resolution: None at this time
