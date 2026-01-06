---
uid: Uno.GetStarted.AI.CopilotVS
---

# Get Started with GitHub Copilot in Visual Studio

This guide will walk you through the setup process for getting started with GitHub Copilot in Visual Studio.

## Prerequisites

- [Visual Studio 2022 (17.14+) or Visual Studio 2026](https://visualstudio.microsoft.com/vs/)
- [GitHub Copilot subscription](https://github.com/features/copilot)

## Check your environment

[!include[getting-help](includes/use-uno-check-inline-noheader.md)]

## Setting up GitHub Copilot

1. Install or update Visual Studio 2022 (17.14+) or Visual Studio 2026
1. Install the GitHub Copilot extension from Visual Studio if not already installed:
   - Go to **Extensions** > **Manage Extensions**
   - Search for "GitHub Copilot"
   - Install the extension and restart Visual Studio
1. Sign in to GitHub Copilot:
   - Click on your profile in the top-right corner
   - Select **Sign in to GitHub Copilot**
   - Follow the prompts to authenticate with your GitHub account

## Setting up Uno Platform MCPs

To enable GitHub Copilot to use Uno Platform's Model Context Protocol (MCP) servers:

1. Open Visual Studio
1. Open or create an Uno Platform project
1. Open the GitHub Copilot chat window by clicking the Copilot icon in the toolbar or pressing **Ctrl+/** (Windows/Linux) or **Cmd+/** (macOS)
1. Click the **tools** icon in the chat window to configure MCP servers

   ![Visual Studio Copilot MCP Tools](Assets/vs-copilot-mcp-tools.png)

1. Add the Uno Platform Remote MCP:
   - Click **Add MCP Server**
   - Server Name: `uno`
   - Server Type: **HTTP**
   - URL: `https://mcp.platform.uno/v1`
   - Leave HTTP Headers empty
   - Set Tools to `*` (all tools)
   - Click **Save**

1. Add the Uno Platform App MCP:
   - Click **Add MCP Server**
   - Server Name: `uno-app`
   - Server Type: **Local**
   - Command: `dotnet dnx -y uno.devserver --mcp-app`
   - Leave Environment Variables empty
   - Set Tools to `*` (all tools)
   - Click **Save**

> [!IMPORTANT]
> The `uno-app` MCP may fail to load unless Visual Studio is opened in a folder containing an Uno Platform app. This MCP requires you to [sign in to your Uno Platform account](xref:Uno.GetStarted.Licensing) to access the App MCP features.

> [!NOTE]
> In Visual Studio 2022/2026, MCPs might not be enabled automatically. Make sure to click the "tools" icon in the chat window to verify both Uno Platform MCPs are configured and enabled.

## Next Steps

Now that you are set up, let's [create your first app](xref:Uno.GettingStarted.CreateAnApp.AI.CopilotVS).
