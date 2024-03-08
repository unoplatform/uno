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
- The [Uno Platform Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=unoplatform.vscode) Extension
- For Windows, install the [GTK+ 3 runtime](https://github.com/tschoonj/GTK-for-Windows-Runtime-Environment-Installer/releases) (See [this uno-check issue](https://github.com/unoplatform/uno.check/issues/12))
- For Linux, install [OpenJDK 11](https://learn.microsoft.com/java/openjdk/install#install-on-ubuntu) for Android development.

## Check your environment

[!include[getting-help](includes/use-uno-check-inline-noheader.md)]

## Configure VS Code

If you are new to VS Code or to developing C# applications with VS Code take the time to follow the next steps.

1. Open VS Code
1. If this is not a new installation then try to update it. Press `F1` and type `Code: Check for Updates...` and select it. A notification will tell you if an update is available.
1. Configure VS Code to start from the command-line using the `code` command. This can be configured by following [these instructions](https://code.visualstudio.com/docs/editor/command-line#_launching-from-command-line).
1. Install the **Uno Platform** extension. Press `F1` and type `Extensions: Install Extensions`, search the marketplace for **Uno Platform** and click the **Install** button.

## OmniSharp Legacy Mode

Starting **Uno Platform extension** version 0.12, running in VS Code automatically uses the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) `ms-dotnettools.csdevkit`.

If you are using GitPod, any [Open VSX environment](https://open-vsx.org) or earlier versions of the Uno Platform extension, you will be automatically using the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) `ms-dotnettools.csharp` with OmniSharp enabled.

To switch between C# Dev Kit and OmniSharp:

- [Switch to C# Dev Kit Mode](xref:Uno.GetStarted.vscode.DevKit)
- [Switch to OmniSharp Mode](xref:Uno.GetStarted.vscode.OmniSharp)

## Platform specific setup

You may need to follow additional directions, depending on your development environment.

### [**Windows**](#tab/windows)

[!include[windows-setup](includes/additional-windows-setup-inline.md)]

### [**macOS**](#tab/macos)

[!include[macos-setup](includes/additional-macos-setup-inline.md)]

### [**Linux**](#tab/linux)

[!include[linux-setup](includes/additional-linux-setup-inline.md)]

***

## Next Steps

You're all set! You can create your [first Uno Platform app](xref:Uno.GettingStarted.CreateAnApp.VSCode).
