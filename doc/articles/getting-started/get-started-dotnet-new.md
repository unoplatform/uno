---
uid: Uno.GetStarted.dotnet-new
---

# dotnet new templates for Uno Platform

The Uno Platform provides a set of command-line templates to create cross-platform applications.

To install the templates, type the following:

```dotnetcli
dotnet new install Uno.Templates
```

To determine all parameters available for a template use `dotnet new [templatename] -h`.

> [!IMPORTANT]
> Installing the templates is done per dotnet CLI version. Meaning that the templates are installed for the version shown by `dotnet --version`. If you tried to use the templates with a version different than the one you used for installing, you'll get "No templates found matching: '<template-name>'." error.
>
> This is common when using `global.json` that alters the .NET CLI/SDK version. Specifically, it's common for the UI Test template.

[!include[use-uno-check](includes/use-uno-check-inline.md)]

## Uno Platform Application

This template can be used to create a multi-platform application for iOS, Android, WebAssembly, Mac Catalyst, Linux, and Win32 Desktop which uses the new WinUI 3 APIs.

> [!TIP]
> To create a new Uno Platform app and browse all the capabilities of the template, head to our <a target="_blank" href="https://aka.platform.uno/app-wizard">Live Wizard</a> to create a `dotnet new` command line. For a detailed overview of the Uno Platform project wizard and all its options, see the [Wizard guide](xref:Uno.GettingStarted.UsingWizard).

It comes with the **Blank** and **Recommended** presets.

To create a blank template, using minimal dependencies:

```dotnetcli
dotnet new unoapp -preset=blank -o test
```

To create from a recommended template using [Uno.Extensions](xref:Uno.Extensions.Overview):

```dotnetcli
dotnet new unoapp -preset=recommended -o test
```

More articles on WinUI 3:

- [WinUI 3, UWP, and Uno Platform.](uwp-vs-winui3.md)
- [Updating from UWP to WinUI 3.](updating-to-winui3.md)

## Uno Platform Blank Application (UWP)

This template can be used to create a blank multi-platform application for iOS, Android, WebAssembly, macOS, Desktop (Windows, Linux, macOS), and Skia/Wpf (Windows 7 and 10), using the UWP API set.

A basic example:

```dotnetcli
dotnet new unoapp-uwp -o MyApp
```

A more advanced example that will not generate the Mobile head:

```dotnetcli
dotnet new unoapp-uwp -o MyApp -mobile=false
```

## Uno Platform Blank library

This template can be used to create a blank library. With this type of library, Skia and WebAssembly do not need to be built separately, but cannot be differentiated.

A basic example:

```dotnetcli
dotnet new unolib -o MyUnoLib
```

## Uno Platform MAUI Embedding Class Library

This template can be used to create a .NET MAUI Controls library to embed within your Uno Platform app.

A basic example:

```dotnetcli
dotnet new unomauilib -o MyMauiEmbeddingLibrary
```

## Uno Platform Uno.UITest library

This template creates a project for creating and running UI Tests based on [Uno.UITest](https://github.com/unoplatform/Uno.UITest).

Considering you've created an application with `dotnet new unoapp -o MyApp`, you can then create a UI Tests library with these steps:

- Create a folder name `MyApp\MyApp.UITests`
- In that folder, run `dotnet new unoapp-uitest`

This will automatically add the new project to the existing solution.

For additional information about UI Tests creation, visit the [Uno.UITest](https://github.com/unoplatform/Uno.UITest) documentation.

### Uninstalling the templates

Using a command line or terminal, run the following command:

```dotnetcli
dotnet new uninstall Uno.Templates
```

[!include[getting-help](includes/getting-help.md)]
