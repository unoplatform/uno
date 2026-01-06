---
uid: Uno.GettingStarted.CreateAnApp.AI.CopilotVSCode
---

# Creating an app with GitHub Copilot in VS Code

> [!IMPORTANT]
> Before creating an app, make sure you have completed the [GitHub Copilot in VS Code setup](xref:Uno.GetStarted.AI.CopilotVSCode) to install and configure GitHub Copilot and the Uno Platform MCPs.

## Create the App

To create an Uno Platform app with GitHub Copilot in VS Code:

1. Create a new project using the [Uno Platform Live Wizard](https://aka.platform.uno/app-wizard), or [`dotnet new`](xref:Uno.GetStarted.dotnet-new):

   ```bash
   dotnet new unoapp --tfm net10.0 -o MyNewApp
   ```

1. Change your current directory to be in the folder containing the new project:

   ```bash
   cd MyNewApp
   ```

1. Sign in to your Uno Platform account to get access to the [Uno App MCP](xref:Uno.Features.Uno.MCPs):

   ```bash
   dotnet dnx -y uno.devserver login
   ```

   The Uno Studio app will allow you to [sign in or create an account](xref:Uno.GetStarted.Licensing).

1. Open the project in VS Code:

   ```bash
   code .
   ```

1. Open the GitHub Copilot chat by clicking the chat icon in the activity bar or pressing **Ctrl+Alt+I** (Windows/Linux) or **Cmd+Option+I** (macOS)

1. Verify that the Uno Platform MCPs are loaded by checking the chat interface or the Output panel (View > Output, select "GitHub Copilot Chat")

## Next Steps

You can start developing with [**GitHub Copilot in VS Code**](xref:Uno.BuildYourApp.AI.Agents).
