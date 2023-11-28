---
uid: Uno.GetStarted.vscode
---

## Get Started on VS Code

This guide will walk you through the set-up process for building apps with Uno under Windows, Linux or macOS.

See these sections for information about using Uno Platform with:

- [Codespaces](features/working-with-codespaces.md)
- [Gitpod](features/working-with-gitpod.md)

## Prerequisites

- [**Visual Studio Code**](https://code.visualstudio.com/)
- The [Uno Platform Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=unoplatform.vscode) Extension
- For Windows, install the [GTK+ 3 runtime](https://github.com/tschoonj/GTK-for-Windows-Runtime-Environment-Installer/releases) (See [this uno-check issue](https://github.com/unoplatform/uno.check/issues/12))
- For Linux, install [OpenJDK 11](https://learn.microsoft.com/en-us/java/openjdk/install#install-on-ubuntu) for Android development.

## Check your environment

[!include[getting-help](use-uno-check-inline-noheader.md)]

## Configure VS Code

If you are new to VS Code or to developing C# applications with VS Code take the time to follow the next steps.

1. Open VS Code
1. If this is not a new installation then try to update it. Press `F1` and type `Code: Check for Updates...` and select it. A notification will tell you if an update is available.
1. Configure VS Code to start from the command-line using the `code` command. This can be configured by following [these instructions](https://code.visualstudio.com/docs/editor/command-line#_launching-from-command-line).
1. Install the **C#** extension. Press `F1` and type `Extensions: Install Extensions`, search the marketplace for **C#** and click the **Install** button.
1. Install the **Uno Platform** extension. Press `F1` and type `Extensions: Install Extensions`, search the marketplace for **Uno Platform** and click the **Install** button.

No other extensions are needed to complete this guide.

## C# Dev Kit Compatibility

At this time, the preview version of the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) `ms-dotnettools.csdevkit` is not compatible with the Uno Platform extension. It requires a preview version of the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) `ms-dotnettools.csharp` that contains major breaking changes.

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

## Platform specific setup

You may need to follow additional directions, depending on your development environment.

# [**Windows**](#tab/windows)

[!include[windows-setup](additional-windows-setup-inline.md)]

# [**Linux**](#tab/linux)

[!include[linux-setup](additional-linux-setup-inline.md)]

# [**macOS**](#tab/macos)

[!include[macos-setup](additional-macos-setup-inline.md)]

***

## Next Steps
You're all set! You can now [create your first app](xref:Uno.GettingStarted.CreateAnApp.vscode) with Uno Platform.
