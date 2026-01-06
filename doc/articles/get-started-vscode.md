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
> The Uno Platform extension provides all the tooling needed to enable AI agents from Visual Studio, VS Code, Claude Code, GitHub Copilot CLI, Codex, and more.
>
> **Uno MCP** provides structured, semantic access to Uno Platform’s complete knowledge base—covering documentation, APIs, and best practices—empowering AI agents and developers with the intelligence they need to build better experiences. Meanwhile, **App MCP** brings intelligent automation to life by enabling AI agents to interact directly with live Uno Platform applications, creating a seamless bridge between design, development, and execution.
>
> Uno Platform's MCP tools are available when you [sign in to your Uno Platform account](xref:Uno.GetStarted.Licensing). For more information, see [Using the Uno Platform MCPs](xref:Uno.Features.Uno.MCPs).

## Setting up GitHub Copilot (Optional)

If you want to use GitHub Copilot with Uno Platform's MCP servers:

1. Install the GitHub Copilot extension:
   - Press **F1** and type `Extensions: Install Extensions`
   - Search for "GitHub Copilot"
   - Install the extension
   - You'll need a [GitHub Copilot subscription](https://github.com/features/copilot)

1. Sign in to GitHub Copilot:
   - Click on the Accounts icon in the bottom-left corner
   - Select **Sign in to use GitHub Copilot**
   - Follow the prompts to authenticate with your GitHub account

1. Configure Uno Platform MCPs:
   - Open the GitHub Copilot chat by clicking the chat icon in the activity bar or pressing **Ctrl+Alt+I** (Windows/Linux) or **Cmd+Option+I** (macOS)

     ![VS Code Copilot icon](Assets/vscode-copilot-icon.png)

   - Create or edit the MCP configuration file. The location depends on your operating system:
     - **Windows**: `%APPDATA%\Code\User\globalStorage\github.copilot-chat\mcpServers.json`
     - **macOS**: `~/Library/Application Support/Code/User/globalStorage/github.copilot-chat/mcpServers.json`
     - **Linux**: `~/.config/Code/User/globalStorage/github.copilot-chat/mcpServers.json`

   - Add the following configuration to `mcp.json`:

     ```json
     {
       "mcpServers": {
         "uno": {
           "type": "http",
           "url": "https://mcp.platform.uno/v1"
         },
         "uno-app": {
           "type": "stdio",
           "command": "dotnet",
           "args": ["dnx", "-y", "uno.devserver", "--mcp-app"]
         }
       }
     }
     ```

   - Restart Visual Studio Code to load the MCP servers

> [!IMPORTANT]
> The `uno-app` MCP may fail to load unless VS Code is opened in a folder containing an Uno Platform app. This MCP requires you to [sign in to your Uno Platform account](xref:Uno.GetStarted.Licensing) to access the App MCP features.

> [!NOTE]
> You can verify the MCPs are loaded by opening the Copilot chat and checking if Uno Platform tools are available. You can also check the Output panel (View > Output) and select "GitHub Copilot Chat" from the dropdown to see MCP connection logs.

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

You're all set to create your [first Uno Platform app](xref:Uno.GettingStarted.CreateAnApp.VSCode)!
