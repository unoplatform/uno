---
uid: Uno.GetStarted.AI.Claude
---

# Get Started with Claude Code

This guide will walk you through the setup process for getting started with Claude Code.

## Check your environment

[!include[getting-help](includes/use-uno-check-inline-noheader.md)]

## Setting up Uno Platform MCPs

1. Install [Claude Code](https://code.claude.com/docs/en/overview) from the CLI
1. Register the Uno Platform MCPs:

    ```bash
    claude mcp add --scope user --transport http uno https://mcp.platform.uno/v1
    claude mcp add --scope user --transport stdio "uno-app" -- dotnet dnx -y uno.devserver --mcp-app
    ```

1. Start Claude Code in your terminal and then run:

    ```bash
    /mcp
    ```

    This will show the Uno Platform MCPs available to the agent.

    > [!IMPORTANT]
    > The `uno-app` MCP [may fail to load](https://github.com/anthropics/claude-code/issues/4384) unless Claude is opened in a folder containing an Uno Platform app.

## Setting up the Uno Platform Skills

Uno Platform ships a catalog of agent skills — covering areas such as MVUX, navigation, Uno Toolkit controls, theming, and UI testing — bundled in the `uno-platform-studio` plugin. Once installed, Claude Code automatically selects the relevant skills as it works on your prompts.

1. In Claude Code, install the plugin:

    ```text
    /plugin marketplace add unoplatform/studio
    /plugin install uno-platform-studio@uno-platform
    ```

1. If Claude Code prompts you to run `/reload-plugins`, do so to apply the new plugin.

For the full skill catalog, update instructions, and other installation options, see [Skills & Plugins](xref:Uno.PlatformStudio.Skills).

## Next Steps

Now that you are set up, let's [create your first app](xref:Uno.GettingStarted.CreateAnApp.AI.Claude).

## Uno TechBite

Getting Started with Uno Platform & Claude Code - Complete Setup Guide:
> [!Video https://www.youtube-nocookie.com/embed/19CLmH6kkvE]
