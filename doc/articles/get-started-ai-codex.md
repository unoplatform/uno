---
uid: Uno.GetStarted.AI.Codex
---

# Get Started with Codex CLI

This guide will walk you through the setup process for getting started with Codex.

## Check your environment

[!include[getting-help](includes/use-uno-check-inline-noheader.md)]

## Setting up Uno Platform MCPs

1. Install [Codex CLI](https://developers.openai.com/codex/cli) using the official commands, for example:

    ```bash
    npm i -g @openai/codex
    # or on macOS
    brew install --cask codex
    ```

1. Register the Uno Platform MCPs:

    ```bash
    codex mcp add "uno" --url "https://mcp.platform.uno/v1"
    codex mcp add "uno-app" -- dotnet dnx -y uno.devserver --mcp-app
    ```

1. Start Codex CLI and type the following:

    ```text
    /mcp
    ```

    This will show the Uno Platform tools available to the agent.

    > [!IMPORTANT]
    > The uno-app MCP may fail to load unless Codex is opened in a folder containing an Uno Platform app.

## Next Steps

Now that you are set up, let's [create your first app](xref:Uno.GettingStarted.CreateAnApp.AI.Codex).
