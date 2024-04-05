---
uid: Uno.GettingStarted.CreateAnApp.VS2022
---
# Creating an app with Visual Studio 2022 for Windows

> [!NOTE]
> Make sure to setup your environment by [following our instructions](xref:Uno.GetStarted.vs2022).

## Create the App

To create an Uno Platform app:

1. Create a new C# solution using the **Uno Platform App** template, from Visual Studio's **Start Page**, then click the **Next** button

    ![Visual Studio - Get started - Selecting `create a new project` option](Assets/tutorial01/newproject1.PNG)  
    ![Visual Studio - Create a new project - Selecting `Uno Platform App` option](Assets/tutorial01/newproject2.PNG)

1. Configure your new project by providing a project name and a location, check the "**Place solution and project in the same directory**" option, then click the **Create** button

    ![Visual Studio - Configure project name and location](Assets/tutorial01/configure-new-unoplatform-app.PNG)

1. Choose a template preset to build your application

    ![Visual Studio - Configure your new project](getting-started/wizard/assets/intro.png)

    > [!TIP]
    > For a detailed overview of the Uno Platform project template wizard and all its options, see [this](xref:Uno.GettingStarted.UsingWizard).

1. Click the create button

1. Wait for the projects to be created, and their dependencies to be restored

1. A banner at the top of the editor may ask to reload projects, click **Reload projects**:  
    ![Visual Studio - A banner indicating to reload projects](Assets/quick-start/vs2022-project-reload.png)

## Debug the App

### [**Windows**](#tab/windows)

To run the **Windows** (WinAppSDK) head:

- In the debugger toolbar, click the down arrow to select **MyApp (WinAppSDK Unpackaged)**

    ![Visual Studio - Select the WinappSDK profile](getting-started/wizard/assets/winappsdk-debugger-dropdown.png)

- Press the `MyApp (WinAppSDK Unpackaged)` button to deploy the app
- If you've not enabled Developer Mode, the Settings app should open to the appropriate page. Turn on Developer Mode and accept the disclaimer.

### [**WebAssembly**](#tab/wasm)

To run the **WebAssembly** (Wasm) head:

- In the debugger toolbar, click the down arrow to select **MyApp (WebAssembly)**

    ![Visual Studio - Select the Wasm profile](getting-started/wizard/assets/wasm-debugger-dropdown.png)

- Press the `MyApp (WebAssembly)` button to deploy the app
- To run/debug your WebAssembly app on a mobile device, you can utilize the Dev Tunnels feature of Visual Studio 2022 (see [Microsoft Learn documentation](https://learn.microsoft.com/aspnet/core/test/dev-tunnels) to get started)

### [**ASP.NET Hosted WebAssembly**](#tab/wasm-hosted)

To run the ASP.NET Hosted **WebAssembly** (Server) head, if you've selected the option in the Wizard:

- Right-click on the `MyApp.Server` project, select **Set as startup project**
- Press the `MyApp.Server` button to deploy the app

### [**iOS**](#tab/iOS)

To debug for **iOS**:

> [!NOTE]
> For information about connecting Visual Studio to a Mac build host to build iOS apps, see [Pairing to a Mac for .NET iOS development](https://learn.microsoft.com/xamarin/ios/get-started/installation/windows/connecting-to-mac/). Catalyst apps are not supported in Visual Studio 2022 on Windows, you can use [VS Code Remote SSH](xref:Uno.GetStarted.vscode) to enable this scenario.

- In the "Debug toolbar" drop-down, select framework `net8.0-ios`:

    ![Visual Studio - "Debug toolbar" drop-down selecting the "net8.0-ios" framework](Assets/quick-start/net8-ios-debug.png)

- Select:
  - An active device, if your IDE is connected to a macOS Host
  - A [local device using Hot Restart](https://learn.microsoft.com/xamarin/xamarin-forms/deploy-test/hot-restart), to debug your application without connecting to a mac

    > [!NOTE]
    > If no iOS devices are available, a Visual Studio 17.7+ issue may require unloading/reloading the project. Right-click on the `MyApp` project and select **Unload Project** then **Load project**.

### [**Android**](#tab/Android)

To debug the **Android** platform:

- In the **Debug toolbar** drop-down, select framework `net8.0-android`

    ![Visual Studio - "Debug toolbar" drop-down selecting the "net8.0-android" framework](Assets/quick-start/net8-android-debug.png)

- Select an active device in the "Device" sub-menu
    > [!NOTE]
    > If no android devices are available, a Visual Studio 17.7+ issue may require unloading/reloading the project. Right-click on the `MyApp` project and select **Unload Project** then **Load project**.

### [**Desktop**](#tab/desktop)

To debug your Uno Platform **Desktop** target:

- Directly on Windows:
  - In the **Debug toolbar** drop-down, select the profile `MyApp (Desktop)`

        ![Visual Studio - "Debug toolbar" drop-down selecting the "net8.0-desktop" framework](Assets/quick-start/net8-desktop-debug-windows.png)

  - Press `F5` to debug.

- Using WSL with X11:
  - In the **Debug toolbar** drop-down, select the profile `MyApp (Desktop WSL2)`

        ![Visual Studio - "Debug toolbar" drop-down selecting the "net8.0-desktop" framework](Assets/quick-start/net8-desktop-wsl2-debug-windows.png)

  - Then press `F5` or the green arrow to start debugging the app

***

You're all set!

## Next Steps

Now that you're Created and Debug the App.

Learn more about:

- [Uno Platform features and architecture](xref:Uno.GetStarted.Explore)
- [Hot Reload feature](xref:Uno.Features.HotReload)
- [Uno Platform App solution structure](xref:Uno.Development.AppStructure)
- [Troubleshooting](xref:Uno.UI.CommonIssues)
- [How-tos and Tutorials](xref:Uno.Tutorials.Intro) See real-world examples with working code.
- [List of views implemented in Uno](implemented-views.md) for the set of available controls and their properties.
