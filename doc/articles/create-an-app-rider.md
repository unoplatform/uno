---
uid: Uno.GettingStarted.CreateAnApp.rider
---
# Creating an app with Rider

> [!NOTE]
> Make sure to setup your environment by [following our instructions](xref:Uno.GetStarted.Rider).

Creating an Uno Platform project is done [using dotnet new](get-started-dotnet-new.md) by following these steps:

1. In your terminal, navigate to the folder that will contains your new app.
1. Create a new project:  
    ```bash
    dotnet new unoapp --preset=blank -o MyApp
    ```

    You should now have a folder structure that looks like this:  
    ![rider-folder-structure](Assets/quick-start/rider-folder-structure.JPG)

## Building and debugging your app

# [**Android**](#tab/android)

Set Android as your startup project. Run.
![run-android-rider](Assets/quick-start/run-android-rider.JPG)

> [!NOTE]
> Whether you're using a physical device or the emulator, the app will install but will not automatically open. You will have to manually open it.

# [**WebAssembly**](#tab/wasm)

Select Wasm as your startup project. Run.
![run-wasm-rider](Assets/quick-start/run-wasm-rider.JPG)  
A new browser window will automatically run your application.  

Note: There is no debugging for Wasm within Rider, but you debug using the built in Chrome tools. 

# [**Catalyst**](#tab/catalyst)
You will be able to build the macOS project.

![run-ios-rider](Assets/quick-start/run-ios-rider.JPG)

Alternatively, you can use a tool like VNC to run the simulator on a mac.  

# [**WinUI/WinAppSDK**](#tab/winui)
You will be able to build the Windows project.

![run-uwp-rider](Assets/quick-start/run-uwp-rider.JPG)  

# [**Skia Gtk**](#tab/gtk)
Select the Skia.Gtk project, then Run.

# [**Skia WPF**](#tab/gtk)
Select the Skia.WPF project, then Run.

> [!NOTE] 
> The WPF project can only be run under Windows.

***

## Explore

Next, [explore the Uno Platform App solution structure](xref:Uno.Development.About).

## Troubleshoot Issues

You may encounter issues while developing your app. Please see [Common Issues](xref:Uno.UI.CommonIssues) we have documented.

## Next Steps

Now that you have built your first application, you can head to [our tutorials](xref:Uno.GettingStarted.Tutorial1) on how to work on your Uno Platform app.