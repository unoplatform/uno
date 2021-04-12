# Introduction

Create Uno Platform Projects with Hot Reload and XAML Preview using VS Code 😎.
Extension features:

- Skia Gtk Project Template
- WASM Project Template
- Shared Skia Gtk/WASM Project
    - Multiple debug targets
- Create`.xaml` file and automatically create `.xaml.cs`
	- Files are automatically fill with a default `Page` "Hello World" template code
- XAML Code completion
- XAML Preview
- XAML Hot Reload server

For complete usage and functionality information continue reading this document.

> ⚠️Uno Platform is a trademark of **nventive Inc.** The content of this extension have not been reviewed or approved by **nventive Inc.** The publisher of this extension do not speak for **nventive Inc.**

> ⚠️ This is a BETA version! Problems can occur. If so, please open an issue in the [Github Repository](https://github.com/microhobby/vs-code-uno-platform/issues).

# Requirements

## Linux (same for WSL)

- .NET Core 3.1
- .NET 5
- [mono-complete](https://platform.uno/docs/articles/get-started-vscode.html#prerequisites) (required for WASM projects)
- [libgtk-3-dev](https://platform.uno/docs/articles/get-started-with-linux.html#setting-up-for-linux) (required for Skia GTK projects)

##  Windows

- .NET Core 3.1
- .NET 5
- [Mono](https://platform.uno/docs/articles/get-started-vscode.html#prerequisites) (required for WASM projects)
- [GTK 3 runtime](https://platform.uno/docs/articles/get-started-with-linux.html#setting-for-windows-and-wsl) (required for Skia GTK projects)

# Features

## Solution Creation

Click on the Uno Platform logo in the activity bar and wait for the extension activation:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/UnoLogoOnActivityBar.gif?raw=true)

Select the type of project:

> ⚠️ For now only `Skia GTK`, `WASM` or a shared project of both are supported.

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/SelectNewProject.jpg?raw=true)

Input solution name and select a folder for the project to be saved:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/SelectProjectAndFolder.gif?raw=true)

Before the folder with the solution is opened for development, the extension execute a first build to ensure that we have complete IntelliSence working inside the `Shared` project. Wait for the first build to finish:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/CreationFirstBuild.gif?raw=true)

After the build the folder will open automatically and is ready for the development of your application.

> ⚠️ The C# extension may take some time to load the solution. Only after the correct loading of the solution from C# extension is that the IntelliSence will work.

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/WaitingOmnisharp.gif?raw=true)

## XAML

### XAML Preview

With a `XAML` file opened, and selected, in the editor the `Preview Uno Platform XAML` button will be shown:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/XAMLPreviewButton.jpg?raw=true)

Click on the button to open the `XAML Preview` panel. Wait for the load and you can edit `XAML` and check the preview in the opened panel:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/XAMLPreviewWorking.gif?raw=true)

> ⚠️ This is an extremely experimental feature, we recommend use Hot Reload as it is more reliable.

### XAML Completion

With a `XAML` file opened you can edit the file, or type the shortcut `ctrl+space` as  usual, to get code completion for:

#### Controls

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/XAMLCompletionControls.gif?raw=true)

#### Properties Inside a Control

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/XAMLCompletionProperties.gif?raw=true)

#### Events Inside a Control

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/XAMLCompletionEvents.gif?raw=true)

#### Properties with Enums

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/XAMLCompletionEnums.gif?raw=true)

### Automatically Creation of Code Behind

Adding a new `.xaml` add automatically the respective `.xaml.cs` to the folder with a basic `Hello World` template:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/XAMLAddingCodeBehind.gif?raw=true)

### XAML Hot Reload

Start the debug process, press `F5`. Edit the `.xaml` file been presented and check the changes on the window running the application:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/XAMLHotReload.gif?raw=true)

> ✅ Hot Reload works for both, Skia GTK and WASM projects.

### XAML Open Code Behind

Open the respective `.xaml.cs` from a` .xaml` file, click on the `Open Code Behind`:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/OpenCodeBehindFromXAML.gif?raw=true)

### Sync XAML Symbols

To have access to the new symbols added to a `.xaml` file click on the `Update XAML Symbols` option:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/SyncXAMLSymbols.gif?raw=true)

This will build the solution to create the code behind symbols, so you will have access to the intelissence for these symbols.

## Debug

### Skia GTK

Add breakpoints to the code and simply press F5 to build the project and start the debugging session:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/SkiaGtkDebugSession.gif?raw=true&date=23123)

During a debugging session on a Skia GTK project you can inspect values using the `Run and Debug` activity bar or the` Debug Console` panel:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/SkiaGtkDebugInspectVariables.gif?raw=true)

### WASM

Add breakpoints to the code and simply press F5 to build the project and start the debugging session:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/WASMDebug.gif?raw=true&date=477737)

During a debugging session on a WASM project you can inspect values only using the `Run and Debug` `VARIABLES` panel. The `WATCH`and `DEBUG CONSOLE` does not work in the current version:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/WASMEvaluateVariables.gif?raw=true)

### Shared Skia.GTK and WASM

For shared project you must select the debug target before press F5:

![](https://github.com/microhobby/vs-code-uno-platform/blob/docs/Documentation/img/SelectSharedDebugTarget.gif?raw=true)
