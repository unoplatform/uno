---
uid: Uno.GetStarted.AI.GoogleAntigravity
---

# Get Started with Google Antigravity

This guide walks you through configuring the Uno Platform MCPs for Google Antigravity so you can use the agent with the Uno Dev Server.

## Check your environment

[!include[getting-help](includes/use-uno-check-inline-noheader.md)]

## Setting up Uno Platform MCPs

> [!NOTE]
> The Uno Platform extension is not functional in Antigravity at this time.

1. Install [Google Antigravity](https://antigravity.google/) by following Google's official instructions for your operating system.
1. Configure [Antigravity MCPs](https://antigravity.google/docs/mcp):

    1. Open the MCP store via the "..." dropdown at the top of the editor's agent panel.
    1. Click on "Manage MCP Servers"
    1. Click on "View raw config"
    1. Modify the mcp_config.json with your custom MCP server configuration:

        ```json
        {
            "mcpServers": {
                "uno": {
                    "url": "https://mcp.platform.uno/v1"
                },
                "uno-app": {
                    "command": "dotnet",
                    "args": [
                        "dnx",
                        "-y",
                        "uno.devserver",
                        "--mcp-app",
                        "--force-roots-fallback"
                    ]
                }
            }
        }
        ```

    > [!NOTE]
    > `--force-roots-fallback` exposes the `uno_app_set_roots` tool so Antigravity, which does not yet provide [MCP roots](https://modelcontextprotocol.io/specification/2025-06-18/client/roots), can initialize.

## Next Steps

You are now ready to [create your first app with Google Antigravity](xref:Uno.GettingStarted.CreateAnApp.AI.GoogleAntigravity).
