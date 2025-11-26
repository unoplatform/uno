---
uid: Uno.GettingStarted.CreateAnApp.AI.CopilotCli
---

# Creating an app with GitHub Copilot CLI

To get started with Copilot CLI:

1. Create a new project using the [Uno Platform Live Wizard](https://aka.platform.uno/app-wizard), or [`dotnet new`](xref:Uno.GetStarted.dotnet-new)

    ```bash
    dotnet new unoapp --tfm net10.0 -o MyNewApp
    ```

1. Change your directory to be in the folder containing the new project (e.g., `cd MyNewApp`)
1. Run the following command:

    ```bash
    dnx -y uno.devserver login
    ```

    The Uno Studio app will allow you to [sign in or create an account](xref:Uno.GetStarted.Licensing) and get access the [Uno App MCP](xref:Uno.Features.Uno.MCPs).

1. Start Copilot CLI in the folder of the app:

    ```bash
    copilot
    ```

## Next Steps

You can start developing with [**GitHub Copilot CLI**](xref:Uno.BuildYourApp.AI.Agents).
