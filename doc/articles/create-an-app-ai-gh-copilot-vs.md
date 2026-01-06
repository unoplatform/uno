---
uid: Uno.GettingStarted.CreateAnApp.AI.CopilotVS
---

# Creating an app with GitHub Copilot in Visual Studio

> [!IMPORTANT]
> Before creating an app, make sure you have completed the [GitHub Copilot in Visual Studio setup](xref:Uno.GetStarted.AI.CopilotVS) to install and configure GitHub Copilot and the Uno Platform MCPs.

## Create the App

To create an Uno Platform app with GitHub Copilot in Visual Studio:

1. Create a new project using the [Uno Platform project template](xref:Uno.GettingStarted.CreateAnApp.VS2022) or use the Live Wizard:

   ```bash
   dotnet new unoapp --tfm net10.0 -o MyNewApp
   ```

1. Open the project in Visual Studio
1. Sign in to your Uno Platform account when prompted to get access to [Uno App MCP](xref:Uno.Features.Uno.MCPs):
   - Click on the Uno Platform notification in Visual Studio
   - Or run the following command in the terminal:

     ```bash
     dotnet dnx -y uno.devserver login
     ```

   The Uno Studio app will allow you to [sign in or create an account](xref:Uno.GetStarted.Licensing).

1. Open the GitHub Copilot chat window by clicking the Copilot icon or pressing **Ctrl+/** (Windows/Linux) or **Cmd+/** (macOS)

1. Verify that the Uno Platform MCPs are enabled by clicking the **tools** icon in the chat window

   ![Visual Studio Copilot chat window](Assets/vs-copilot-chat.png)

## Next Steps

You can start developing with [**GitHub Copilot in Visual Studio**](xref:Uno.BuildYourApp.AI.Agents).
