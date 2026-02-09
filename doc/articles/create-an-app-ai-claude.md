---
uid: Uno.GettingStarted.CreateAnApp.AI.Claude
---

# Creating an app with Claude Code

> [!IMPORTANT]
> Before creating an app, make sure you have completed the [Claude Code setup](xref:Uno.GetStarted.AI.Claude) to install and configure the Uno Platform MCPs.

1. Create a new project using the [Uno Platform Live Wizard](https://aka.platform.uno/app-wizard), or [`dotnet new`](xref:Uno.GetStarted.dotnet-new)

    ```bash
    dotnet new unoapp --tfm net10.0 -o MyNewApp
    ```

1. Change your current directory to be in the folder containing the new project (e.g., `cd MyNewApp`)
1. Run the following command, which will launch the Uno Studio app:

    ```bash
    dnx -y uno.devserver login
    ```

    The Uno Studio app will allow you to [sign in or create an account](xref:Uno.GetStarted.Licensing) and get access the [Uno App MCP](xref:Uno.Features.Uno.MCPs).
    
1. Still in the app folder, open Claude Code:

    ```bash
    claude
    ```

## Next Steps

You can start developing with [**Claude Code**](xref:Uno.BuildYourApp.AI.Agents).
