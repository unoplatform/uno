---
uid: Uno.GettingStarted.CreateAnApp.AI.GoogleAntigravity
---

# Creating an app with Google Antigravity

## Setup

1. Create a new project using the [Uno Platform Live Wizard](https://aka.platform.uno/app-wizard), or [`dotnet new`](xref:Uno.GetStarted.dotnet-new). For example:

    ```bash
    dotnet new unoapp --tfm net10.0 -o MyApp
    ```

1. Open the project you just created inside Google Antigravity (for example, `MyApp`).
1. Open a terminal in the project folder and sign in with the Uno Dev Server:

    ```bash
    dnx -y uno.devserver login
    ```

1. Launch Google Antigravity from the same folder (or reload the window).
1. In the Agent chat window, if you're running Antigravity and Uno for the first time, ask the following:

    ```text
    Set the uno platform mcp roots to initialize app support.
    ```

1. When asked, approve the execution of the `uno_app_set_roots` tool.
1. Restart or reload Antigravity to ensure the Uno Platform App MCP is fully initialized.

## Next Steps

You can start iterating with [Uno Platform AI agents](xref:Uno.BuildYourApp.AI.Agents) using Google Antigravity as your assistant.
