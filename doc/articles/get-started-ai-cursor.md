---
uid: Uno.GetStarted.AI.Cursor
---

# Get Started with Cursor

This guide will walk you through the setup process for getting started with Cursor.

## Check your environment

[!include[getting-help](includes/use-uno-check-inline-noheader.md)]

## Setting up Uno Platform MCPs

1. Install [Cursor](https://cursor.com/docs).

	> [!NOTE]
	> The Uno Platform extension is not functional in Cursor at this time.

1. In Cursor, open the Uno project for which you want to enable MCP support.

1. Open a terminal in the project folder and run the following command:

	```bash
	dnx -y uno.devserver login
	```

	The Uno Studio app will allow you to [sign in or create an account](xref:Uno.GetStarted.Licensing) and get access the [Uno App MCP](xref:Uno.Features.Uno.MCPs).

1. Create a file named `.cursor/mcp.json` in the project folder and place the following content:

	```json
	{
	  "mcpServers": {
			"uno": {
				 "url": "https://mcp.platform.uno/v1"
			},
			"uno-app": {
				 "command": "dnx",
				 "args": ["-y","uno.devserver","--mcp-app"]
			}
	  }
	}
	```

## Next Steps

Now that you are set up, let's [create your first app](xref:Uno.GettingStarted.CreateAnApp.AI.Cursor).
