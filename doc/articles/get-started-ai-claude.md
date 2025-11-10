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
    claude mcp add --transport http uno https://mcp.platform.uno/v1
    claude mcp add --transport stdio "uno-app" -- dnx -y uno.devserver --mcp-app
    ```

1. Open Claude and run:

    ```bash
    /mcp
    ```

    This will show the Uno Platform MCPs available to the agent.

## Next Steps

Now that you are set up, let's [create your first app](xref:Uno.GettingStarted.CreateAnApp.AI.Claude).
