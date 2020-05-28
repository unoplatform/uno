# Getting started with dotnet new templates

The Uno Platform provides a set of command line templates to create cross platform applications.

To install the templates, type the following:
```
dotnet new -i Uno.ProjectTemplates.Dotnet::2.4
```

If you need to determine the parameters available for a template use `dotnet new [templatename] -h`.

## Uno Platform Blank Application

This template can be used to create a blank cross-platform application for iOS, Android, WebAssembly and macOS.

A basic example:
```
dotnet new unoapp -o MyApp
```

A more advanced example which will not generate the android and macOS heads:

```
dotnet new unoapp -o MyApp -android=false -macos=false
```

## Uno Platform Blank Application for WinUI 3.0 - Preview

This template can be used to create a blank cross-platform application for iOS, Android, WebAssembly and macOS which uses the new WinUI 3.0 apis.

A basic example:
```
dotnet new unoapp-winui -o MyApp
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
1. Run the app using **Ctrl+F5** (without the Visual Studio debugger), and you’re good to go!
