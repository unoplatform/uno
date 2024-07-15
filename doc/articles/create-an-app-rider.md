---
uid: Uno.GettingStarted.CreateAnApp.Rider
---

# Create an app with Rider

> [!NOTE]
> Make sure to setup your environment first by [following our instructions](xref:Uno.GetStarted.Rider).

## Create the App

Creating an Uno Platform project is done [using dotnet new](xref:Uno.GetStarted.dotnet-new) and the Uno Platform Live Wizard by following these steps:

1. Open a browser and navigate to the <a target="_blank" href="https://aka.platform.uno/app-wizard">Live Wizard</a>
1. Configure your new project by providing a project name, then click **Start**

    ![A screen showing how to choose a solution name](Assets/quick-start/live-wizard-01-choose-name.png)

1. Choose a template to build your application

    ![A screen showing how to choose template for the new app](Assets/quick-start/live-wizard-02-select-preset.png)

    > [!TIP]
    > For a detailed overview of the Uno Platform project wizard and all its options, see the [Wizard guide](xref:Uno.GettingStarted.UsingWizard).

1. Click the **Create** button on the top right corner, then click the **Copy** button

    ![A screen showing the dotnet new command to create the new app](Assets/quick-start/live-wizard-03-create-app.png)

1. In your terminal, navigate to the folder that will contain your new app.

1. Create a new project by pasting the command that was previously generated in the Live Wizard.

1. Open the solution in Rider, you should now have a folder structure that looks like this:

    ![A screen showing the structure of the solution in Rider](Assets/quick-start/rider-folder-structure.png)

> [!TIP]
> If you are not able to run the online Live Wizard, you can explore the [`dotnet new` template](xref:Uno.GetStarted.dotnet-new) directly in the CLI.

### Considerations for macOS and Linux

When using macOS or Linux for developing your application and you have selected the WinAppSDK target, you may get the UNOB0014 error which mentions building on macOS or Linux is not supported. While Uno Platform is able to filter out unsupported targets from the command line and other IDE, Rider currently does not support this automatic filtering.

To correct this, you'll need to modify your `csproj` file in order to make the project compatible.

You can change this line:

```xml
<TargetFrameworks>net8.0-android;net8.0-ios;net8.0-maccatalyst;net8.0-windows10.0.19041;net8.0-browserwasm;net8.0-desktop</TargetFrameworks>
```

To be:

```xml
<TargetFrameworks>net8.0-android;net8.0-browserwasm;net8.0-desktop</TargetFrameworks>
<TargetFrameworks Condition=" $([MSBuild]::IsOSPlatform('windows')) ">$(TargetFrameworks);net8.0-windows10.0.19041</TargetFrameworks>
<TargetFrameworks Condition=" !$([MSBuild]::IsOSPlatform('linux')) ">$(TargetFrameworks);net8.0-ios;net8.0-maccatalyst</TargetFrameworks>
```

Make sure to adjust the list of target frameworks based on platforms you have in your original list.

## Debug the App

### [**Desktop**](#tab/desktop)

Select the **MyUnoApp (Desktop)** debug profile then click the green arrow or the debug button.

![A view of the Rider taskbar for Desktop](Assets/quick-start/run-desktop-rider.png)

### [**Android**](#tab/android)

Set the Android debug profile in the debugger toolbar, then click the green arrow or the debug button.
![A view of the Rider taskbar for Android](Assets/quick-start/run-android-rider.png)

> [!NOTE]
> Whether you're using a physical device or the emulator, the app will install but will not automatically open. You will have to manually open it.

### [**WebAssembly**](#tab/wasm)

Select the **MyUnoApp (WebAssembly)** debug profile then click the green arrow or the debug button.

![A view of the Rider taskbar for WebAssembly](Assets/quick-start/run-wasm-rider.png)

A new browser window will automatically run your application.

> [!NOTE]
> There is no debugging for WebAssembly within Rider for Uno Platform, but you can debug using the [built-in Chrome tools](external/uno.wasm.bootstrap/doc/debugger-support.md#how-to-use-the-browser-debugger).

### [**iOS**](#tab/ios)

Select the **MyUnoApp** debug profile with the mobile Apple logo then click the green arrow or the debug button.

![A view of the Rider taskbar for iOS](Assets/quick-start/run-ios-rider.png)

> [!NOTE]
> Debugging iOS apps is only supported on macOS

### [**Catalyst**](#tab/catalyst)

Select the **MyUnoApp** debug profile with the desktop Apple logo then click the green arrow or the debug button.

![A view of the Rider taskbar for Catalyst](Assets/quick-start/run-catalyst-rider.png)

> [!NOTE]
> Debugging Mac Catalyst apps is only supported on macOS

### [**WinUI/WinAppSDK**](#tab/winui)

Select the **MyUnoApp (WinAppSDK Unpackaged)** debug profile then click the green arrow or the debug button.

![A view of the Rider taskbar for WinAppSDK](Assets/quick-start/run-winappsdk-rider.png)

> [!NOTE]
> Debugging Windows App SDK profile is only supported on Windows.

---

## Next Steps

Now that you're Created and Debug the App.

Learn more about:

- [Uno Platform features and architecture](xref:Uno.GetStarted.Explore)
- [Hot Reload feature](xref:Uno.Features.HotReload)
- [Uno Platform App solution structure](xref:Uno.Development.AppStructure)
- [Troubleshooting](xref:Uno.UI.CommonIssues)
- [How-tos and Tutorials](xref:Uno.Tutorials.Intro) See real-world examples with working code.
- <a href="implemented-views.md">Use the API Reference to Browse the set of available controls and their properties.</a>
