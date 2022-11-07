# dotnet new templates for Uno Platform

The Uno Platform provides a set of command-line templates to create cross-platform applications.

To install the templates, type the following:
```
dotnet new --install Uno.ProjectTemplates.Dotnet
```

If you need to determine the parameters available for a template use `dotnet new [templatename] -h`.

> [!IMPORTANT]
> Installing the templates is done per dotnet CLI version. Meaning that the templates are installed for the version shown by `dotnet --version`. If you tried to use the templates with a version different than the one you used for installing, you'll get "No templates found matching: '<template-name>'." error.
>
> This is common when using `global.json` that alters the .NET CLI/SDK version. Specifically, it's common for the UI Test template.

[!include[getting-help](use-uno-check-inline.md)]

## Uno Platform Blank Application for WinAppSDK - WinUI 3

This template can be used to create a blank multi-platform application for iOS, Android, WebAssembly, macOS, Mac Catalyst, Linux, and Win32 Desktop which uses the new WinUI 3 APIs.

This template uses a single project head for iOS, Android, macOS, and Mac Catalyst. It requires Visual Studio 2022.

A basic example:
```
dotnet new unoapp -o MyApp
```

> [!NOTE]
> It is possible to create a .NET 7 template by using `-framework net7.0` parameter.

More articles on WinUI 3:

 * [WinUI 3, UWP, and Uno Platform.](uwp-vs-winui3.md)
 * [Updating from UWP to WinUI 3.](updating-to-winui3.md)

## Uno Platform Blank Application (UWP)

This template can be used to create a blank multi-platform application for iOS, Android, WebAssembly, macOS, Skia/GTK (Windows, Linux, macOS), and Skia/Wpf (Windows 7 and 10), using the UWP

A basic example:
```
dotnet new unoapp-uwp -o MyApp
```

A more advanced example that will not generate the android and macOS heads:

```
dotnet new unoapp-uwp -o MyApp -android=false -macos=false
```

## Uno Platform Blank Application (WinAppSDK - WinUI 3, Xamarin)

This template can be used to create a blank multi-platform application for iOS, Android, WebAssembly, macOS, Linux, and Win32 Desktop which uses the new WinUI 3 APIs.

[**Find detailed instructions here.**](get-started-winui3.md)

A basic example:
```
dotnet new unoapp-winui-xamarin -o MyApp
```

More articles on WinUI 3:

 * [WinUI 3, UWP, and Uno Platform.](uwp-vs-winui3.md)
 * [Updating from UWP to WinUI 3.](updating-to-winui3.md)


## Uno Platform Blank Application (UWP, .NET 6)

> .NET 6 Mobile support is currently in Preview, following Microsoft's support status. As of Uno 4.1, .NET 6 Mobile Preview 13 and above is supported with [Visual Studio 2022 17.2 Preview 1](https://visualstudio.microsoft.com/vs/preview). Previous releases of Visual Studio are not supported.

This template can be used to create a blank multi-platform application for iOS, Android, WebAssembly, macOS, Mac Catalyst, Skia/GTK (Windows, Linux, macOS), and Skia/Wpf (Windows 7 and 10).

This template uses a single project head for iOS, Android, macOS, and Mac Catalyst. It requires Visual Studio 2022.

A basic example:
```
dotnet new unoapp-uwp-net6 -o MyApp
```

A more advanced example that will not generate the android and macOS heads:

```
dotnet new unoapp-uwp-net6 -o MyApp --Mobile=false
```

## Uno Platform Extensions

Uno Platform provides an enhanced template to build your application with less effort.

See [this documentation](external/uno.extensions/doc/Overview/ExtensionsOverviewAndGettingStarted.md) for more information.

## Uno Platform Blank library

This template can be used to create a blank library. With this type of library, Skia and WebAssembly do not need to be built separately, but cannot be differentiated.

A basic example:
```
dotnet new unolib -o MyUnoLib
```

## Uno Platform Blank Cross-Runtime library

This template can be used to create a blank cross-runtime library, when platform specific code needs to be created for Skia and WebAssembly.

A basic example:
```
dotnet new unolib-crossruntime -o MyCrossRuntimeLibrary
```

## Uno Platform Blank Prism Application

This template is specializing in the creation of a [Prism Library](https://github.com/PrismLibrary/Prism) enabled blank application.

A basic example:
```
dotnet new unoapp-prism -o MyApp
```

A more advanced example which will not generate the android and macOS heads:

```
dotnet new unoapp -o MyApp -android=false -macos=false
```

## Uno Platform Uno.UITest library
This templates creates a project for creating and running UI Tests based on [Uno.UITest](https://github.com/unoplatform/Uno.UITest).

Considering you've created an application as follows:
- `dotnet new unoapp -o MyApp`

To create a UI Tests library:
- Create a folder name `MyApp\MyApp.UITests`
- In that folder, run `dotnet new unoapp-uitest`

This will automatically add the new project to the existing solution.

For additional information about UI Tests creation, visit the [Uno.UITest](https://github.com/unoplatform/Uno.UITest) documentation.

## Uno Platform WebAssembly support for Xamarin.Forms

This template is built to enhance an existing Xamarin.Forms application with the [Uno Platform WebAssembly support](https://github.com/unoplatform/Uno.Xamarin.Forms.Platform).

To use it:

1. Create a Xamarin.Forms project 
    1. Check **Place project and solution in the same directory**
    1. Check **Windows (UWP)**
1. Using a **VS Developer Command Prompt**, navigate to the folder containing the solution
    ```
    dotnet new wasmxfhead
    ```
1. Open or Reload the solution in Visual Studio 
1. Set the Wasm project as the startup project 
1. Open the **Nuget Package manager** for the Wasm project and update the `Uno.Xamarin.Forms.Platform` project to the latest **stable** package 
1. Run the app using **F5** (with the Visual Studio debugger), and you are good to go!

### Uninstalling the templates

Using a command line or terminal, run the following command:

`dotnet new -u Uno.ProjectTemplates.Dotnet`

[!include[getting-help](getting-help.md)]
