# Welcome to Uno Platform!

## Please read important installation information below.

Congratulations, you've just created a new project using the [Uno Platform](https://platform.uno/) project templates!

* You can give the [Single Page app getting started guide](https://platform.uno/docs/articles/getting-started-tutorial-1.html) a try
* Next you can try our [Bug Tracker sample walk-through](https://platform.uno/docs/articles/getting-started-tutorial-2.html)
* More advanced examples in our [Uno.Samples repository](https://github.com/unoplatform/uno.samples)
* Fork a fully-fledged [Ch9 application and source code ](https://platform.uno/code-samples/#ch9)
* You can find detailed documentation on Uno topics [on our web site](https://platform.uno/docs/articles/intro.html).
* Run the [uno-check CLI tool](external/uno.check/doc/using-uno-check.md) to ensure your dev environment is set up correctly

## Common Issues
The Uno Platform features and support is constantly evolving, but you may encounter some of the  issues below while building your application.

#### WebAssembly: Access to fetch at 'https://XXXX' from origin 'http://XXXX' has been blocked by CORS policy

This is a security restriction from the JavaScript `fetch` API, where the endpoint you're calling needs to provide [CORS headers](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS) to work properly.

If you control the API, you'll need to use the features from your framework to enable CORS, and if you don't you'll need to ask the maintainers of the endpoint to enable CORS.

To test if CORS is really the issue, you can use [CORS Anywhere](https://cors-anywhere.herokuapp.com/) to proxy the queries.

#### error NETSDK1148: A referenced assembly was compiled using a newer version of Microsoft.Windows.SDK.NET.dll.

See [this article](features/winapp-sdk-specifics.md#adjusting-windows-sdk-references) to solve this issue.

#### The XAML editor shows `The type 'page' does not support direct content` message
XAML Intellisense [is not working properly](https://developercommunity.visualstudio.com/content/problem/587980/xaml-intellisense-does-not-use-contentpropertyattr.html) in Visual Studio when the active project is not the UWP one. 

To work around this issue, close all XAML editors, open a C# file and select 'UWP' in the top-left drop-down list of the text editor sector. Once selected, re-open the XAML file.

#### Error MSB3030: Could not copy the file "MyProject.Shared\MainPage.xbf" because it was not found.
This issue is present in Visual Studio 17.2 and 17.3, and can be addressed by [taking a look at this issue](https://github.com/unoplatform/uno/discussions/5007#discussioncomment-2583741).

#### `InitializeComponent` or `x:Name` variable is not available in code-behind
Visual Studio [does not refresh the intellisense cache](https://developercommunity.visualstudio.com/content/problem/588021/the-compile-itemgroup-intellisense-cache-is-not-re.html) properly, causing variables to be incorrectly defined.

To fix this issue, build your project once, close the solution and reopen it.

It is also important to note that Uno uses a multi-project structure, for which each project has to be build individually for errors to disapear from the **Error List** window (notice the **Project** column values).

In order to clear the **Error List** window, build the whole solution completely once. Thereafter, build a specific project and prefer the use of the **Output** tool window (in the menu **View** -> **Output**), taking build messages by order of appearance.

#### Event handler cannot be added automatically

Event handlers [cannot be automatically](https://github.com/unoplatform/uno/issues/1348#issuecomment-520300471) added using the XAML editor. 

A workaround is to use the [`x:Bind` to events feature](features/windows-ui-xaml-xbind.md#examples). This feature allows to use a simpler syntax like `<Button Click="{x:Bind MyClick}" />` and declare a simple method `private void MyClick() { }` in the code-behind.

#### `Don't know how to marshal a return value of type 'System.IntPtr'`
[This issue](https://github.com/unoplatform/uno/issues/9430) may happen for Uno.UI 4.4.20 and later when deploying an application using the iOS Simulator or MacCatalyst when the application contains a `TextBox`.

In order to fix this, add the following to your csproj (Xamarin, `net6.0-ios`, `net6.0-maccatalyst`):
```xml
<PropertyGroup>
  <MtouchExtraArgs>$(MtouchExtraArgs) --registrar=static</MtouchExtraArgs>
</PropertyGroup>
```

#### Build error `Failed to generate AOT layout`

When building for WebAssembly with AOT mode enabled, the following error may appear:
```
Failed to generate AOT layout (More details are available in diagnostics mode or using the MSBuild /bl switch)
```

To troubleshoot this error, you can change the text output log level:
  - Go to **Tools**, **Options**, **Projects and Solution**, then **Build and Run**
  - Set **MSBuild project build output verbosity** to **Normal** or **Detailed**
  - Build your project again and take a look at the additional output next to the `Failed to generate AOT layout` error

You can get additional build [troubleshooting information here](uno-builds-troubleshooting.md).

#### Runtime error `No parameterless constructor defined for XXXX`
This error is generally caused by some missing [IL Linker](https://github.com/mono/linker/tree/master/docs) configuration on WebAssembly. You may need to add some of your application assemblies in the LinkerConfig.xml file of your project. You can find [additional information in the documentation](features/using-il-linker-webassembly.md).

Similar error messages using various libraries:
- `Don't know how to detect when XXX is activated/deactivated, you may need to implement IActivationForViewFetcher` (ReactiveUI)

#### System.DllNotFoundException: Gtk: libgtk-3-0.dll

When running the Skia.GTK project head, the following error may happen:

```
Unhandled exception. System.TypeInitializationException: The type initializer for 'Gtk.Application' threw an exception.
---> System.DllNotFoundException: Gtk: libgtk-3-0.dll, libgtk-3.so.0, libgtk-3.0.dylib, gtk-3.dll
```

On Windows, you will need to install the [GTK+3 runtime](https://github.com/tschoonj/GTK-for-Windows-Runtime-Environment-Installer/releases). **Make sure to restart Visual Studio** for the changes to be used by Visual Studio.
On Linux, you'll need to follow the [Uno Platform](get-started-with-linux.md#setting-up-for-linux) setup instructions.
On macOS, you'll need to follow the [Uno Platform](get-started-vsmac.md) setup instructions.

#### Abnormally long build times with using WebAssembly and WSL
When building an application that uses native dependencies (such as Skia, SQLite) or using PG-AOT/AOT, using WSL 2 may cause abnormally long build times.

You can migrate your WSL v2 installation into a v1, by [visiting this document](get-started-with-linux.md).

#### "Missing value for TargetPlatformWinMDLocation property" when adding a project reference
This issue is caused by [VS 2019 support for SDK-Style projects](https://developercommunity.visualstudio.com/content/problem/1170010/missing-value-for-targetplatformwinmdlocation-prop.html).

To add a reference change the list of `<TargetFramework>` to place `netstandard2.0` at the first position in the project you are trying to add the reference to.

#### Abnormally long build times when using Roslyn analyzers
It is a good practice to use Roslyn analyzers to validate your code during compilation, but some generators may have difficulty handling the source generated by the Uno Platform (one notable example is [GCop](https://github.com/Geeksltd/GCop)). You may need to disable those for Uno projects or get an update from the analyzer's vendor.

#### XAML Hot Reload troubleshooting

The XAML Hot reload provides a Visual Studio for Windows output window name "Uno Platform" with diagnotics messages. You can find additional information there in case XAML Hot Reload does not work properly.

Some common troubleshooting steps:
- Make sure to rebuild your application if the XAML changes are not applied
- Ensure that the Uno.UI.RemoteControl package has the same version as the Uno.UI package (Similar step is valid for Uno.WinUI packages)

More troubleshooting information is available [in this section](features/working-with-xaml-hot-reload.md).

#### WebAssembly: Hot Reload fails to start with Mixed Content: The page at XXX was loaded over HTTPS, but attempted to connect to the insecure WebSocket endpoint

This issue is caused by visual studio enforcing https connections for local content. You can work around this by either:
- Removing the https endpoint in the `Properties/launchSettings.json` file
- Unchecking the `Use SSL` option in the project's Debug launch profiles
- Selecting the project name instead of IISExpress in the toolbar debug icon drop down list

#### Build fails with `error : Error reading response`
In general, this error happens when the XAML parser detects a syntax error. Fixing the error generally fixes the build.

This error may happen occasionally without any explicit error message, rebuilding the project may fix the issue.
