---
uid: Uno.GettingStarted.CreateAnApp.AI.Cursor
---

# Creating an app with Cursor

1. Create a new project using the [Uno Platform Live Wizard](https://aka.platform.uno/app-wizard), or [`dotnet new`](xref:Uno.GetStarted.dotnet-new) command and change `-o MyApp` to be `-n MyApp -o .`.

    This will create the solution the folder you've already created in vscode. For example:

    ```bash
    dotnet new unoapp --tfm net10.0 -n MyApp -o .
    ```

1. In Cursor, open the project that was just created (e.g., `MyApp`).
1. Open a terminal in the project folder and run the following command, which will launch the Uno Studio app that will allow you to [sign in or create an account](xref:Uno.GetStarted.Licensing) and get access the [Uno App MCP](xref:Uno.Features.Uno.MCPs).

    ```bash
    dnx -y uno.devserver login
    ```

1. Create a file named `.cursor/mcp.json` and place the following content:

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

You can start developing with [**Cursor**](xref:Uno.BuildYourApp.AI.Agents).
