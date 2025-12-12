---
uid: Uno.GetStarted.vscode
---

# Get Started on VS Code

This guide will walk you through the setup process for building apps with Uno Platform under Windows, Linux, or macOS.

See these sections for information about using Uno Platform with:

- [Codespaces](features/working-with-codespaces.md)
- [Ona](features/working-with-gitpod.md)

## Prerequisites

- [**Visual Studio Code**](https://code.visualstudio.com/)
- The [Uno Platform Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=unoplatform.vscode) Extension
- For Linux, install [OpenJDK 17](https://learn.microsoft.com/java/openjdk/install#install-on-ubuntu) for Android development.

## Check your environment

[!include[getting-help](includes/use-uno-check-inline-noheader.md)]

## Configure VS Code

If you are new to VS Code or to developing C# applications with VS Code take the time to follow the next steps.

1. Open VS Code
1. If this is not a new installation, then try to update it. Press `F1` and type `Code: Check for Updates...` and select it. A notification will tell you if an update is available.
1. Configure VS Code to start from the command-line using the `code` command. This can be configured by following [these instructions](https://code.visualstudio.com/docs/editor/command-line#_launching-from-command-line).
1. Install the **Uno Platform** extension. Press `F1` and type `Extensions: Install Extensions`, search the marketplace for **Uno Platform** and click the **Install** button.

> [!NOTE]
> The Uno Platform extension automatically sets up AI development capabilities with MCP (Model Context Protocol) servers. These tools enable AI agents like GitHub Copilot to intelligently interact with your Uno Platform applications. For more information about the MCP tools, see [Using the Uno Platform MCPs](xref:Uno.Features.Uno.MCPs).

## Platform-specific setup

You may need to follow additional directions, depending on your development environment.

### Android & iOS

For assistance configuring Android or iOS emulators, see the [Android & iOS emulator troubleshooting guide](xref:Uno.UI.CommonIssues.MobileDebugging).

### Linux

[!include[linux-setup](includes/additional-linux-setup-inline.md)]

## OmniSharp Legacy Mode

Starting **Uno Platform extension** version 0.12, running in VS Code automatically uses the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) `ms-dotnettools.csdevkit`.

If you are using Ona, any [Open VSX environment](https://open-vsx.org) or earlier versions of the Uno Platform extension, you will be automatically using the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) `ms-dotnettools.csharp` with OmniSharp enabled.

To switch between C# Dev Kit and OmniSharp:

- [Switch to C# Dev Kit Mode](xref:Uno.GetStarted.vscode.DevKit)
- [Switch to OmniSharp Mode](xref:Uno.GetStarted.vscode.OmniSharp)

---

## Next Steps

You're all set! You can create your [first Uno Platform app](xref:Uno.GettingStarted.CreateAnApp.VSCode).
