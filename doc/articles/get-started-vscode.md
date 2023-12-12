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
1. Install the **Uno Platform** extension. Press `F1` and type `Extensions: Install Extensions`, search the marketplace for **Uno Platform** and click the **Install** button.
1. Open the VS Code Settings using `Ctrl` + `,` (or `⌘` + `,` on a Mac), then search for `useOmnisharp` and enable it (checkbox)
    ![useOmnisharp](Assets/quick-start/vs-code-useOmniSharp.png)

No other extensions are needed to complete this guide.

## C# Dev Kit (Experimental) or OmniSharp (Legacy) Modes

New versions of the **Uno Platform extension** works with the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) `ms-dotnettools.csharp` (with OmniSharp enabled) **or** the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) `ms-dotnettools.csdevkit`. Depending on your configuration some manual changes might be needed.

Note: Microsoft has [not completed nor formalized](https://github.com/dotnet/vscode-csharp/issues/5805) the APIs required for integration with C# DevKit. As such our support is, like the APIs, **experimental** and might break if/when breaking changes are introduced in the Microsoft extensions.

- [How to switch to C# DevKit Mode](doc/articles/get-started-vscode-devkit.md)
- [How to switch to OmniSharp Mode](doc/articles/get-started-vscode-omnisharp.md)

### Detecting the current mode of operation

Inside the **Output** pane using `Ctrl` + `Shift` + `U` (`Shift` + `⌘` + `,` on a Mac), select the **Uno Platform** logs from the combobox. Near the top of the logs you should see a line with either `[Info] Running in OmniSharp mode` or `[Info] Running in DevKit mode`.

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

You're all set! You can create your [first Uno Platform app](xref:Uno.GettingStarted.CreateAnApp.VSCode).