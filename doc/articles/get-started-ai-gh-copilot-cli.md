---
uid: Uno.GetStarted.AI.CopilotCLI
---

# Get Started with GitHub Copilot CLI

This guide will walk you through the setup process for getting started with GitHub Copilot CLI.

## Check your environment

[!include[getting-help](includes/use-uno-check-inline-noheader.md)]

## Setting up Uno Platform MCPs

To get started with GitHub Copilot CLI:

1. Install [Copilot CLI](https://github.com/features/copilot/cli)
1. Start Copilot CLI
1. Type `/mcp` and register the following:
    1. Unique Name: `uno`
    1. Server Type: HTTP
    1. URL: `https://mcp.platform.uno/v1`
    1. Skip HTTP Headers and leave tools to `*`
1. Type `/mcp` again and register the following (app-specific MCP):
    1. Unique Name: `uno-app`
    1. Server Type: Local
    1. Command: `dotnet dnx -y uno.devserver --mcp-app`
    1. Skip Environment Variables and leave tools with `*`

> [!IMPORTANT]
> The uno-app MCP may fail to load unless Copilot is opened in a folder containing an Uno Platform app.

## Setting up the Uno Platform Skills

Uno Platform ships a catalog of agent skills — covering areas such as MVUX, navigation, Uno Toolkit controls, theming, and UI testing — bundled in the `uno-platform-studio` plugin. Once installed, Copilot CLI automatically selects the relevant skills as it works on your prompts.

From a terminal, install the plugin:

```text
copilot plugin marketplace add unoplatform/studio
copilot plugin install uno-platform-studio@uno-platform
```

For the full skill catalog, update instructions, and other installation options, see [Skills & Plugins](xref:Uno.PlatformStudio.Skills).

## Next Steps

Now that you are set up, let's [create your first app](xref:Uno.GettingStarted.CreateAnApp.AI.CopilotCli).
