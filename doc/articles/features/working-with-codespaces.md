---
uid: Uno.Features.Codespaces
---

# Using Codespaces

## Developing an Uno App using Codespaces

The easiest way to get started is to visit the [Uno.QuickStart](https://github.com/unoplatform/Uno.QuickStart) repository. It allows you to get started with minimal configuration or project creation steps.

To create a new Codespace, visit [GitHub Codespaces](https://github.com/codespaces).

### Developing for WebAssembly

1. Install the suggested [Uno Platform extension](https://marketplace.visualstudio.com/items?itemName=unoplatform.vscode)
1. Open the command palette (Ctrl+Shift+P) and execute the `Run uno-check` command to install the appropriate .NET SDK
1. Open the command palette (Ctrl+Shift+P) and run the `Install the dotnet new templates` command to install the dotnet new templates
1. Once the C# environment is setup, with the command palette use the command "Omnisharp: Select project" (or click on the project name in the status bar)
1. Select the `MyApp` project
1. Using a terminal, navigate to the `MyApp` folder
1. Type `dotnet run -f net9.0-browserwasm`
1. Once the compilation is done, a server will open on port 5000
1. In the **Ports** tab (next to the Terminal tab), right click to make both the port 5000 and the other dotnet opened port (with `uno.winui.devserver` or `uno.ui.remotecontrol` in the running process column) to "public".
   > Failure to make both ports public will prevent the app from starting properly.
1. Codespaces will suggest to open a new browser window or as a preview window

You can now use C# Hot Reload and XAML Hot Reload to develop your application.

See [the VS Code Getting started](../get-started-vscode.md) documentation for additional details about developing with VS Code.

### Creating your Codespace from scratch

If you want to start from an empty repository, follow these steps:

1. Create an empty repository
1. Install the [`unoplatform.vscode extension`](https://marketplace.visualstudio.com/items?itemName=unoplatform.vscode) from the Extensions activity
1. Open the command palette (Ctrl+Shift+P) and execute the `Run uno-check` command to install the appropriate .NET SDK
1. Open the command palette (Ctrl+Shift+P) and run the `Install the dotnet new templates` command to install the dotnet new templates
1. Open a terminal and create a new project using the following command:

    ```dotnetcli
    dotnet new unoapp -o MyApp -ios=false -android=false -macos=false -skia-tizen=false -skia-wpf=false -skia-linux-fb=false --vscode
    ```

1. Using the Codespaces top left menu, open the `MyApp` folder

You're ready to develop for WebAssembly.
