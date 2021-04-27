# dotnet new templates for Uno Platform

The Uno Platform provides a set of command line templates to create cross platform applications.

To install the templates, type the following:
```
dotnet new -i Uno.ProjectTemplates.Dotnet
```

If you need to determine the parameters available for a template use `dotnet new [templatename] -h`.

## Uno Platform Blank Application

This template can be used to create a blank cross-platform application for iOS, Android, WebAssembly, macOS, Skia/GTK (Windows, Linux, macOS) and Skia/Wpf (Windows 7 and 10).

A basic example:
```
dotnet new unoapp -o MyApp
```

A more advanced example which will not generate the android and macOS heads:

```
dotnet new unoapp -o MyApp -android=false -macos=false
```

## Uno Platform Blank Application for Project Reunion - WinUI 3

This template can be used to create a blank cross-platform application for iOS, Android, WebAssembly, macOS, Linux, and Win32 Desktop which uses the new WinUI 3 apis.

[**Find detailed instructions here.**](get-started-winui3.md)

A basic example:
```
dotnet new unoapp-winui -o MyApp
```

More articles on WinUI 3:

 * [WinUI 3, UWP, and Uno Platform.](uwp-vs-winui3.md)
 * [Updating from UWP to WinUI 3.](updating-to-winui3.md)

## Uno Platform Blank library

This template can be used to create a blank library. With this type of library, Skia and WebAssembly do not need to built separately, but cannot be differentiated.

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

## Uno Platform WebAssembly support for Xamarin Forms

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
1. Run the app using **Ctrl+F5** (without the Visual Studio debugger), and you�re good to go!

### Uninstalling the templates

Using a command line or terminal, run the following command:

`dotnet new -u Uno.ProjectTemplates.Dotnet`

### Getting Help

If you need help with Uno Platform please visit our [Discord](https://www.platform.uno/discord) - #uno-platform channel or [StackOverflow](https://stackoverflow.com/questions/tagged/uno-platform) where our engineering team and community will be able to help you. 
