---
uid: Uno.DevServer
---

# Dev Server

The Dev Server is the local development companion that enables productive inner-loop features in Uno Platform, such as Hot Reload, design-time updates, and IDE integration. It runs as a lightweight HTTP process and maintains a bidirectional channel with the IDE and the running application.

The latest version of the Uno.DevServer is [![NuGet](https://img.shields.io/nuget/v/uno.devserver.svg)](https://www.nuget.org/packages/Uno.DevServer/).

## Overview

- Provides a transport between the IDE and the running application to exchange development-time messages
- Powers Hot Reload and Hot Design experiences by delivering code and XAML updates
- Starts automatically and stays out of the way; you usually do not have to configure it

## Prerequisites

- Requires .NET SDK, [same version as uno](xref:Uno.Development.NetVersionSupport)
- Works in Debug builds, where connection information is embedded so the app can reach the Dev Server

## When and how the Dev Server starts

> [!NOTE]
> The Dev Server starts only when Uno Platform packages are referenced by your project(s).
>
> [!NOTE]
> The Dev Server won't start until NuGet package restore has completed successfully (a failed or pending restore prevents startup).

1. Open the solution: the IDE reserves a free TCP port and writes it to each project's .csproj.user file under the UnoRemoteControlPort property.
2. Build/run in Debug: the app is built with the Dev Server connection information.
3. Launch the app (Debug): the app connects back to the Dev Server.
4. Develop: the IDE and app exchange development-time messages (e.g., Hot Reload updates).

## Command-line (advanced usage for specific scenarios)

You can manage the Dev Server from the command line using the dotnet tool [`Uno.DevServer`](https://www.nuget.org/packages/Uno.DevServer/).

### Installing the dotnet Dev Server tool

The dotnet Dev Server tool is not installed by default, since the IDE Extensions manages its regular tasks for you already.

If you want or need to install or update it, e.g. for [Troubleshooting Dev Server in VS Code](#vscodets), you can do so, by following the steps below.

*This is assuming you already have installed the [**dotnet CLI**](https://dotnet.microsoft.com/download)*

1. Open a Terminal

2. Enter the following command:

   ```pwsh
   dotnet tool install --global Uno.DevServer
   ```

   Now the `dotnet CLI` should respond:

   ```pwsh
   PS C:\Users\YourName\source\YourUnoApp> dotnet tool install --global Uno.DevServer
   You can invoke the tool using the following command: uno-devserver
   Tool 'uno.devserver' (version '6.4.185') was successfully installed.
   ```

   >[!TIP]
   > If you already installed it before, you can use the `dotnet tool update --global Uno.DevServer` command or re-use the installation command. The end result will be the same.

3. You can now use the Dev Server command line name to print out the tool default command `--help`:

   ```pwsh
   uno-devserver
   ```

### Supported Commands for Dev Server

| Command                  | Description                                                  |
|:------------------------:|:-------------------------------------------------------------|
| `uno-devserver start`    | Start the Dev Server for the current solution directory      |
| `uno-devserver stop`     | Stop the Dev Server attached to the current directory        |
| `uno-devserver list`     | List running Dev Server instances                            |
| `uno-devserver cleanup`  | Terminate stale Dev Server processes                         |
| `uno-devserver login`    | Open the Uno Platform settings application                   |
| `--mcp`                  | Run an MCP proxy mode for integration with MCP-based tooling |
<!-- TODO: Validate this command is still available and correct named like this! The actual source code doesn't seem to expect this argument name: https://github.com/unoplatform/uno/blob/f5ea9ec0df2476a14463d28dc37e729ef917b8b2/src/Uno.UI.RemoteControl.Host/Program.cs#L52-L86 and in case this should be "httpPort" instead, we should tell, that this argument is only Recognized alongside the "start" command

| `--port, -p <int>`       | Optional port value for MCP proxy mode                       |
-> Suggested Change:
| `uno-devserver start --httpPort, -p <int>`       | Optional port value for MCP proxy mode <br/> Required: start command                       |
-->
| `--mcp-wait-tools-list`  | Start in MCP STDIO mode                                      |
| `--file-log, -fl <path>` | Enable file logging to the provided file path (supports {Date} token). <br/> Required: path argument. |

## Hot Reload

The Dev Server enables Hot Reload for a faster inner loop:

- C# Hot Reload for managed code changes
- XAML and resource updates without restarting the app
- Asset updates in supported scenarios

## Security

- Uses a random local port, making it hard to guess
- Intended for local development only and relies on your local network security
- Do not expose the Dev Server to untrusted networks

> [!IMPORTANT]
> The Dev Server is a development-time facility. It is not required nor recommended for production deployments.

## Troubleshooting

### [**Common issues**](#tab/common-issues)

- The TCP port number used by the app to connect back to the IDE is located in the <UnoRemoteControlPort> property of the [ProjectName].csproj.user file. If the port number does not match with the one found in the Uno Platform - Hot Reload output window, restart your IDE.
- If the Dev Server does not start, ensure NuGet restore has completed successfully and Uno Platform packages are referenced by your project(s).

### [**Visual Studio**](#tab/vswints)

- The Output window in Visual Studio includes an output category named `Uno Platform`. Diagnostic messages from the Uno Platform VS extension appear there. To enable logging, set MSBuild project build output verbosity to at least "Normal" (above "Minimal"). These changes should take effect immediately without a restart; if you do not see additional logs, try restarting Visual Studio. For more details on build log verbosity, refer to the [official Visual Studio documentation](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-view-save-and-configure-build-log-files?view=vs-2022#to-change-the-amount-of-information-included-in-the-build-log).  

    If you need to share logs when opening an issue on the GitHub [Uno Platform repository](https://github.com/unoplatform/uno), set verbosity to **Diagnostic** to provide the most detailed logs for investigation.

    **Steps to change MSBuild output verbosity:**
    1. Open **Tools > Options > Projects and Solutions > Build and Run**, then set **MSBuild output verbosity** to **Diagnostic** or the required level.

       ![MSBuild output verbosity drop-down](Assets/features/hotreload/vs-msbuild-output-verbosity.png)
    2. Restart Visual Studio, re-open your solution, and wait a few seconds.
    3. Go to **View > Output**.
    4. In the Output window, select `Uno Platform` from the drop-down.

       ![Uno Platform output drop-down](Assets/features/hotreload/vs-uno-platform-logs.png)

### [**Visual Studio Code**](#tab/vscodets)

- The Output window in Visual Studio Code includes an output category named `Uno Platform` in its drop-down menu. Diagnostic messages from the Uno Platform VS Code extension appear there.

    **Steps to see the `Uno Platform` output:**  
    1. In the status bar at the bottom left of VS Code, ensure `NameOfYourProject.csproj` is selected (by default `NameOfYourProject.sln` is selected).

       ![.csproj selection in Visual Studio Code](Assets/features/hotreload/vscode-csproj-selection.png)
    2. Wait a few seconds.
    3. Go to **View > Output**.
    4. In the Output window, select `Uno Platform` from the drop-down.

       ![`Uno Platform` output drop-down](Assets/features/hotreload/vs-code-uno-platform-hr-output.png)

- The **Uno Platform Status** Page (available via Bottomline Extension Bar) shows everywhere `... not found` and any `uno-devserver` command, except from eventually the  toolname only (shows the tool `--help`), fails to find `global.json` containing the SDK Version:

   ```
   PS C:\Users\YourUserName\repos\YourUnoApp> uno-devserver start
   fail: No global.json found in current directory or parent directories. Please run this command from within a project that uses Uno SDK.
   fail: Could not determine SDK version from global.json.
   ```

   **To solve this, you can follow these steps:**

   1. Make sure your open workspace folder is including `.vscode/` with at least:

      - `launch.json`
      - `tasks.json`

   2. If your repository is maybe structured with `./src/` and `./docs/` at the root level, but this folder is still nested like `./src/.vscode/launch.json` you have the following options to solve this:

     1. move the `./src/.vscode/` folder containing the required files to the workspace folder in your repository root, adjust the paths to your project(s) by adding the `src/` prefix and restart vs code, to make sure all extensions are catching up the changes correctly
     2. Open vs code instead in the `./src` Folder, without having to move those files

     > [!TIP]
     > It's up to you, which Option you want to choose, but most of the cases you might want to prefer choosing the first one, in case you are using **Source Control Management** with e.g. **GIT**

   3. Now check the **Uno Platform Status** and enter your `uno-devserver [command] [option]` again.

   If the problem persists, please make sure to [open an issue](https://www.github.com/unoplatform/uno/issues/new/choose).

### [**Rider**](#tab/riderts)

- The Output window in Rider includes an output category named `Uno Platform` in its sidebar. Diagnostic messages from the Uno Platform Rider plugin appear there.

    **Steps to see the Uno Platform output:**  
    1. In the sidebar at the bottom left of Rider, click on the Uno Platform logo.

       ![Uno Platform output logo](Assets/features/hotreload/rider-uno-platform-output.png)
    2. In the Output window, select **LEVEL: Trace** from the drop-down.

       ![Level output drop-down](Assets/features/hotreload/rider-output-level-trace.png)

---
