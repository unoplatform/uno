---
uid: Uno.GetStarted.dotnet-new
---

# dotnet new templates for Uno Platform

The Uno Platform provides a set of command-line templates to create cross-platform applications.

To install the templates, type the following:

```
dotnet new install Uno.Templates
```

To determine all parameters available for a template use `dotnet new [templatename] -h`.

> [!IMPORTANT]
> Installing the templates is done per dotnet CLI version. Meaning that the templates are installed for the version shown by `dotnet --version`. If you tried to use the templates with a version different than the one you used for installing, you'll get "No templates found matching: '<template-name>'." error.
>
> This is common when using `global.json` that alters the .NET CLI/SDK version. Specifically, it's common for the UI Test template.

[!include[getting-help](use-uno-check-inline.md)]

## Uno Platform Application

This template can be used to create a multi-platform application for iOS, Android, WebAssembly, Mac Catalyst, Linux, and Win32 Desktop which uses the new WinUI 3 APIs.

It comes with the **Blank** and **Recommended** presets.

To create a blank template, using minimal dependencies:

```
dotnet new unoapp -preset=blank -o test
```

To create from a recommended template, using [Uno.Extensions](xref:Overview.Extensions):

```
dotnet new unoapp -preset=recommended -o test
```

More articles on WinUI 3:

- [WinUI 3, UWP, and Uno Platform.](uwp-vs-winui3.md)
- [Updating from UWP to WinUI 3.](updating-to-winui3.md)

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

## Uno Platform Blank Application (UWP, .NET 6)

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

See [this documentation](xref:Overview.Features) for more information.

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

## Uno Platform Uno.UITest library

This templates creates a project for creating and running UI Tests based on [Uno.UITest](https://github.com/unoplatform/Uno.UITest).

Considering you've created an application with `dotnet new unoapp -o MyApp`, you can then create a UI Tests library with these steps:

- Create a folder name `MyApp\MyApp.UITests`
- In that folder, run `dotnet new unoapp-uitest`

This will automatically add the new project to the existing solution.

For additional information about UI Tests creation, visit the [Uno.UITest](https://github.com/unoplatform/Uno.UITest) documentation.

### Uninstalling the templates

Using a command line or terminal, run the following command:

```
dotnet new -u Uno.Templates
```

[!include[getting-help](getting-help.md)]
