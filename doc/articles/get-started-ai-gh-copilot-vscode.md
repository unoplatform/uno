---
uid: Uno.GetStarted.AI.CopilotVSCode
---

# Get Started with GitHub Copilot in VS Code

This guide will walk you through the setup process for getting started with GitHub Copilot in Visual Studio Code.

## Prerequisites

- [Visual Studio Code](https://code.visualstudio.com/)
- [GitHub Copilot subscription](https://github.com/features/copilot)

## Check your environment

[!include[getting-help](includes/use-uno-check-inline-noheader.md)]

## Setting up GitHub Copilot

1. Install or update Visual Studio Code
1. Install the GitHub Copilot extension:
   - Press **F1** and type `Extensions: Install Extensions`
   - Search for "GitHub Copilot"
   - Install the extension
1. Sign in to GitHub Copilot:
   - Click on the Accounts icon in the bottom-left corner
   - Select **Sign in to use GitHub Copilot**
   - Follow the prompts to authenticate with your GitHub account

## Setting up Uno Platform MCPs

To enable GitHub Copilot to use Uno Platform's Model Context Protocol (MCP) servers:

1. Open Visual Studio Code
1. Open or create an Uno Platform project
1. Open the GitHub Copilot chat by clicking the chat icon in the activity bar or pressing **Ctrl+Alt+I** (Windows/Linux) or **Cmd+Option+I** (macOS)

   ![VS Code Copilot icon](Assets/vscode-copilot-icon.png)

1. Create or edit the MCP configuration file. The location depends on your operating system:
   - **Windows**: `%APPDATA%\Code\User\globalStorage\github.copilot-chat\mcpServers.json`
   - **macOS**: `~/Library/Application Support/Code/User/globalStorage/github.copilot-chat/mcpServers.json`
   - **Linux**: `~/.config/Code/User/globalStorage/github.copilot-chat/mcpServers.json`

1. Add the following configuration to `mcpServers.json`:

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

1. Restart Visual Studio Code to load the MCP servers

> [!IMPORTANT]
> The `uno-app` MCP may fail to load unless VS Code is opened in a folder containing an Uno Platform app. This MCP requires you to [sign in to your Uno Platform account](xref:Uno.GetStarted.Licensing) to access the App MCP features.

> [!NOTE]
> You can verify the MCPs are loaded by opening the Copilot chat and checking if Uno Platform tools are available. You can also check the Output panel (View > Output) and select "GitHub Copilot Chat" from the dropdown to see MCP connection logs.

## Next Steps

Now that you are set up, let's [create your first app](xref:Uno.GettingStarted.CreateAnApp.AI.CopilotVSCode).
