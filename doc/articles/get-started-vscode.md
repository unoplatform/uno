---
uid: Uno.GetStarted.vscode
---

# Get Started on VS Code

This guide will walk you through the set-up process for building apps with Uno under Windows, Linux or macOS.

See these sections for information about using Uno Platform with:

- [Codespaces](features/working-with-codespaces.md)
- [Gitpod](features/working-with-gitpod.md)

## Prerequisites

- [**Visual Studio Code**](https://code.visualstudio.com/)
- **.NET SDK**
  - [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/7.0) (**version 7.0 (SDK 7.0.102)** or later)
  > Use `dotnet --version` from the terminal to get the version installed.
- The [Uno Platform Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=unoplatform.vscode) Extension
- For Windows, install the [GTK+ 3 runtime](https://github.com/tschoonj/GTK-for-Windows-Runtime-Environment-Installer/releases) (See [this uno-check issue](https://github.com/unoplatform/uno.check/issues/12))

[!include[getting-help](use-uno-check-inline.md)]

## Developing an Uno Platform project

### Create the project

Let's start by installing the Uno project templates. In a terminal, type the following command:

```bash
dotnet new install Uno.Templates
```

Then inside the same terminal, type the following to create a new project:

```bash
dotnet new unoapp -o MyApp -preset=blank -platforms android ios maccatalyst wasm gtk --vscode
```

> [!TIP]
> `MyApp` is the name you want to give to your project.

This will create a blank template app that only contains the WebAssembly, Skia+GTK and Mobile platforms support.

> [!IMPORTANT]
> Mobile targets cannot be built under Linux. If you are using Linux, you'll need to remove `android ios maccatalyst` from the `platforms` list. The previous command would become `dotnet new unoapp -o MyApp -preset=blank -platforms wasm gtk --vscode`.

## Configuring VS Code

If you are new to VS Code or to developing C# applications with VS Code take the time to follow the next steps.

1. Open VS Code
1. If this is not a new installation then try to update it. Press `F1` and type `Code: Check for Updates...` and select it. A notification will tell you if an update is available.
1. Configure VS Code to start from the command-line using the `code` command. This can be configured by following [these instructions](https://code.visualstudio.com/docs/editor/command-line#_launching-from-command-line).
1. Install the **C#** extension. Press `F1` and type `Extensions: Install Extensions`, search the marketplace for **C#** and click the **Install** button.
1. Install the **Uno Platform** extension. Press `F1` and type `Extensions: Install Extensions`, search the marketplace for **Uno Platform** and click the **Install** button.

No other extensions are needed to complete this guide.

## Prepare the application

1. Open the project using Visual Studio Code. In the terminal type

    ```bash
    code ./MyApp
    ```

1. Visual Studio Code might ask to restore the NuGet packages. Allow it to restore them if asked.
1. Once the project has been loaded, in the status bar at the bottom left of VS Code, `MyApp.sln` is selected by default. Select `MyApp.Wasm.csproj`, `MyApp.Skia.Gtk.csproj` or `MyApp.Mobile.csproj` instead.

## Modify the main page

1. In `MainPage.xaml`, replace the Grid's content with the following:

    ```xml
    <StackPanel>
        <TextBlock x:Name="txt"
                    Text="Hello, world!"
                    Margin="20"
                    FontSize="30" />
        <Button Content="click"
                Click="{x:Bind OnClick}" />
    </StackPanel>
    ```

2. In your `MainPage.xaml.cs`, add the following method:

    ```csharp
    private void OnClick()
    {
        var dt = DateTime.Now.ToString();
        txt.Text = dt;
    }
    ```

## Run and Debug application

### WebAssembly

1. In the debugger section of the [activity bar](https://code.visualstudio.com/docs/getstarted/userinterface) select `Debug (Chrome, WebAssembly)`
1. In the status bar, ensure the `MyApp.Wasm.csproj` project is selected - by default `MyApp.sln` is selected.
1. Press `F5` to start the debugging session
1. Place a breakpoint inside the `OnClick` method
1. Click the button in the app, and the breakpoint will hit

### Skia GTK

1. In the debugger section of the [activity bar](https://code.visualstudio.com/docs/getstarted/userinterface) select `Skia.GTK (Debug)`
1. In the status bar, ensure the `MyApp.Skia.Gtk.csproj` project is selected - by default `MyApp.sln` is selected.
1. Press `F5` to start the debugging session
1. Place a breakpoint inside the `OnClick` method
1. Click the button in the app, and the breakpoint will hit

Note that C# Hot Reload is not available when running with the debugger. In order to use C# Hot Reload, run the app using the following:

- On Windows, type the following:

    ```bash
    $env:DOTNET_MODIFIABLE_ASSEMBLIES="debug"
    dotnet run
    ```

- On Linux or macOS:

    ```bash
    export DOTNET_MODIFIABLE_ASSEMBLIES=debug
    dotnet run
    ```

### Mobile Targets (iOS, Android, Mac Catalyst)

The Uno Platform extension provides support for debugging:

- The Android target on Windows
- The iOS, Android and Mac Catalyst targets on macOS

It is also possible to use [Remote - SSH](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-ssh) addin to connect to a macOS machine from a Windows or Linux machine to debug iOS and Mac Catalyst apps remotely.

# [**Android**](#tab/androiddebug)

#### Debugging for Android

- In the status bar, select the `MyApp.Mobile` project - by default `MyApp.sln` is selected.

  ![mobile project name](Assets/quick-start/vs-code-debug-project.png)
- To the right of `MyApp.Mobile`, click on the target framework to select `net7.0-android | Debug`

  ![android target framework](Assets/quick-start/vs-code-debug-tf-android.png)
- Then, to the right of the target framework, select the device to debug with. You will need to connect an android device, or create an Android simulator.

  ![android device name](Assets/quick-start/vs-code-debug-device-android.png)
- Finally, in the debugger side menu, select the `Uno Plaform Mobile` profile
- Either press `F5` or press the green arrow to start the debugging session.

# [**iOS**](#tab/iosdebug)

> [!NOTE]
> Debugging for iOS is only possible when running locally (or remotely through SSH) on a macOS machine.

- In the status bar, select the `MyApp.Mobile` project - by default `MyApp.sln` is selected.

  ![mobile project name](Assets/quick-start/vs-code-debug-project.png)
- To the right of `MyApp.Mobile`, click on the target framework to select `net7.0-ios | Debug`

  ![ios target framework](Assets/quick-start/vs-code-debug-tf-ios.png)
- Then, to the right of the target framework, select the device to debug with. You will need to connect an iOS device, or use an existing iOS simulator.

  ![ios device](Assets/quick-start/vs-code-debug-device-ios.png)
- Finally, in the debugger side menu, select the `Uno Plaform Mobile` profile
- Either press `F5` or press the green arrow

> [!TIP]
> When deploying to an iOS device, you may encounter the following error: `errSecInternalComponent`. In such case, you'll need to unlock your keychain from a terminal inside VS Code by running the following command: `security unlock-keychain`

# [**Mac Catalyst**](#tab/catalystdebug)

> [!NOTE]
> Debugging for Mac Catalyst is only possible when running locally (or remotely through SSH) on a macOS machine.

- In the status bar, select the `MyApp.Mobile` project - by default `MyApp.sln` is selected.

  ![mobile project name](Assets/quick-start/vs-code-debug-project.png)
- To the right of `MyApp.Mobile`, click on the target framework to select `net7.0-maccatalyst | Debug`

  ![catalyst target framework](Assets/quick-start/vs-code-debug-tf-catalyst.png)
- Finally, in the debugger side menu, select the `Uno Plaform Mobile` profile
- Either press `F5` or press the green arrow to start the debugging session.

***

Once your app is running, place a breakpoint in the `OnClick` method, the breakpoint will be hit when clicking the button in the app.

You can find [advanced Code debugging topic here](xref:uno.vscode.mobile.advanced.debugging).

## Using code snippets

### Adding a new Page

1. In the MyApp folder, create a new file named `Page2.xaml`
2. Type `page` then press the `tab` key to add the page markup
3. Adjust the name and namespaces as needed
4. In the MyApp folder, create a new file named `Page2.xaml.cs`
5. Type `page` then press the `tab` key to add the page code behind C#
6. Adjust the name and namespaces as needed

### Adding a new UserControl

1. In the MyApp folder, create a new file named `UserControl1.xaml`
2. Type `usercontrol` then press the `tab` key to add the page markup
3. Adjust the name and namespaces as needed
4. In the MyApp folder, create a new file named `UserControl1.xaml.cs`
5. Type `usercontrol` then press the `tab` key to add the page code behind C#
6. Adjust the name and namespaces as needed

### Adding a new ResourceDictionary

1. In the MyApp folder, create a new file named `ResourceDictionary1.xaml`
2. Type `resourcedict` then press the `tab` key to add the page markup

### Other snippets

- `rd` creates a new `RowDefinition`
- `cd` creates a new `ColumnDefinition`
- `tag` creates a new XAML tag
- `set` creates a new `Style` setter
- `ctag` creates a new `TextBlock` close XAML tag

## Updating an existing application to work with VS Code

An existing application needs additional changes to be debugged properly.

1. At the root of the workspace, create a folder named `.vscode`
2. Inside this folder, create a file named `launch.json` and copy the [contents of this file](https://github.com/unoplatform/uno.templates/blob/main/src/Uno.Templates/content/unoapp/.vscode/launch.json).
3. Replace all instances of `MyExtensionsApp._1` with your application's name in `launch.json`.
4. Inside this folder, create a file named `tasks.json` and copy the [contents of this file](https://github.com/unoplatform/uno.templates/blob/main/src/Uno.Templates/content/unoapp/.vscode/tasks.json).

### Known limitations for VS Code support

- C# Debugging is not supported when running in a remote Linux Container, Code Spaces or GitPod.
- C# Hot Reload for WebAssembly only supports modifying method bodies. Any other modification is rejected by the compiler.
- C# Hot Reload for Skia supports modifying method bodies, adding properties, adding methods, adding classes. A more accurate list is provided here in Microsoft's documentation.

## Troubleshooting Uno Platform VS Code issues

If you're not sure whether your environment is correctly configured for Uno Platform development, running the [`uno-check` command-line tool](external/uno.check/doc/using-uno-check.md) should be your first step.

The Uno Platform extension provides multiple output windows to troubleshoot its activities:

- **Uno Platform**, which indicates general messages about the extension
- **Uno Platform - Debugger**, which provides activity messages about the debugger feature
- **Uno Platform - Hot Reload**, which provides activity messages about the Hot Reload feature
- **Uno Platform - XAML**, which provides activity messages about the XAML Code Completion feature

If the extension is not behaving properly, try using the `Developer: Reload Window` (or `Ctrl+R`) command in the palette.

## C# Dev Kit Compatibility

At this time, the preview version of the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) `ms-dotnettools.csdevkit` is not compatible with the Uno Platform extension. It requires a preview version of the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) `ms-dotnettools.csharp` that contains major breaking changes.

### Workaround

You can use both the Uno Platform and C# Dev Kit extensions but not simultaneously. The easiest way to accomplish this is to [create profiles](https://code.visualstudio.com/docs/editor/profiles) inside VSCode. Using this method, you can:

1. Create one profile for **Uno Platform**
2. Disable, if installed, C# Dev Kit extension
3. Enable `useOmnisharp` inside the configuration
![useOmnisharp](Assets/quick-start/vs-code-useOmniSharp.png)

4. Create another profile for **C# Dev Kit**
5. Enable (or install) the C# Dev Kit extension
6. Ensure that `useOmnisharp` is disabled inside the configuration
7. Disable the Uno Platform extension

You can then switch between both profiles according to the type of dotnet project you are developing.

You're all set! You can now head to [our tutorials](getting-started-tutorial-1.md) on how to work on your Uno Platform app.

[!include[getting-help](getting-help.md)]
