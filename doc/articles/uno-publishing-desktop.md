---
uid: uno.publishing.desktop
---

# Publishing Your App for Desktop

## Preparing For Publish

- [Profile your app with VS 2022](https://learn.microsoft.com/en-us/visualstudio/profiling/profiling-feature-tour?view=vs-2022)
- [Profile using dotnet-trace and SpeedScope](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)

## Publish your app

The recommended way to publish your `netX.0-desktop` app is to use the Command Line with `dotnet publish`. This provides a familiar mechanism you may already be used to along with a few additional properties. While it is technically possible to publish from Visual Studio 2022, it is not recommended and this will only produce an app for Windows.

When publishing your desktop application you should be using Uno.Sdk 5.4 or later. Publishing the application will only produce an application for the platform of the host OS. You cannot for instance publish a Mac app on Windows or a Windows app on Linux.

> [!NOTE]
> For the purposes of the documented commands we are using `net8.0-desktop` if you are using a later version of .NET such as .NET 9.0 be sure to update to use the appropriate version such as `net9.0-desktop`.

# [**Windows**](#tab/windows)

- [ClickOnce](https://learn.microsoft.com/visualstudio/deployment/quickstart-deploy-using-clickonce-folder?view=vs-2022) on Windows
- Using a Zip file, then running the app using `dotnet [yourapp].dll`

# [**MacOS**](#tab/macos)

The Uno.Sdk supports publishing your app for MacOS in one of 3 ways.

1) Generate an App Bundle (MyApp.app)
2) Generate an Application Package Installer (MyApp.pkg)
3) Generate an Apple Disk Image (MyApp.dmg)

> [!NOTE]
> At this time we do not support publishing an app that can install on either Intel or Apple Silicon. By default the published app will produce an artifact for the runtime of the host you publish on. You may optionally specify the the desired runtime to produce an artifact that can be installed on a different target runtime than the host operating system.

> [!NOTE]
> When publishing your app, the Uno.Sdk will enable a Self Contained publish. This will result in your app being ready to run on your users Mac without any additional steps like installing .NET.

To get started with publishing you can simply run the following command to generate an App Bundle.

```bash
dotnet publish ./pathTo/Project.csproj -f net8.0-desktop
```

To specify the Runtime Identifier you must select one of the [available MacOS RID's](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#macos-rids).

```bash
dotnet publish ./pathTo/Project.csproj -f net8.0-desktop -r osx-arm64
```

To generate an Application Package Installer or Apple Disk Image you optionally provide a package format argument as follows:

```bash
# Generates an Application Package Installer
dotnet publish ./pathTo/Project.csproj -f net8.0-desktop /p:PackageFormat=pkg

# Generates an Apple Disk Image
dotnet publish ./pathTo/Project.csproj -f net8.0-desktop /p:PackageFormat=dmg
```

This is not needed to generate an App Bundle as this will always be provided and will be available in the publish folder.

# [**Linux**](#tab/linux)

Coming Soon

---

To build your app from the CLI, on Windows, Linux, or macOS:

- Open a terminal, command line, or powershell
- Navigate to your `csproj` folder
- Publish the app using:

  ```shell
  dotnet publish -f net8.0-desktop -c Release -o ./publish
  ```


