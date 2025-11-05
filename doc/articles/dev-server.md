---
uid: Uno.DevServer
---

# Dev Server

## Why
- Communication between the application and the IDE
- Hot-Reload
- Hot-Design
- App MCP Server

## How
- On solution opened we select an availabale port to start the Dev Server
- Write the selected port in the `<MyProject>.csproj.user` file
- When application is compiled in DEBUG, port number is embedded
- When application starts, it tries to connect back to the port

> Security:
> - Random port, hard to guess
> - Only on LAN, as safe as your local network

## Troubleshooting

### [**Common issues**](#tab/common-issues)

- The TCP port number used by the app to connect back to the IDE is located in the `<UnoRemoteControlPort>` property of the `[ProjectName].csproj.user` file. If the port number does not match with the one found in the `Uno Platform - Hot Reload` output window, restart your IDE.

### [**Visual Studio 2022**](#tab/vswints)

- The Output window in Visual Studio includes an output category named `Uno Platform` in its drop-down menu. Diagnostic messages from the Uno Platform VS extension appear there. To enable logging, you need to set **MSBuild project build output verbosity** to **at least "Normal"** (above "Minimal"). These changes should take effect immediately without requiring a Visual Studio restart. However, if you do not see additional logs, try restarting Visual Studio. For more details on build log verbosity, refer to the [official Visual Studio documentation](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-view-save-and-configure-build-log-files?view=vs-2022#to-change-the-amount-of-information-included-in-the-build-log).  

    If you need to share logs when opening an issue on the GitHub [Uno Platform repository](https://github.com/unoplatform/uno), it is recommended to set verbosity to **Diagnostic** to provide the most detailed logs for investigation.

    **Steps to change MSBuild output verbosity:**  
    1. Open **Tools > Options > Projects and Solutions > Build and Run**, then set **MSBuild output verbosity** to **Diagnostic** or the required level.

       ![MSBuild output verbosity drop-down](../Assets/features/hotreload/vs-msbuild-output-verbosity.png)
    2. Restart Visual Studio, re-open your solution, and wait a few seconds.
    3. Go to **View > Output**.
    4. In the Output window, select `Uno Platform` from the drop-down.

       ![`Uno Platform` output drop-down](../Assets/features/hotreload/vs-uno-platform-logs.png)

### [**Visual Studio Code**](#tab/vscodets)

- The Output window in Visual Studio Code includes an output category named `Uno Platform - Hot Reload` in its drop-down menu. Diagnostic messages from the Uno Platform VS Code extension appear there.

    **Steps to see the `Uno Platform - Hot Reload` output:**  
    1. In the status bar at the bottom left of VS Code, ensure `NameOfYourProject.csproj` is selected (by default `NameOfYourProject.sln` is selected).

       ![.csproj selection in Visual Studio Code](../Assets/features/hotreload/vscode-csproj-selection.png)
    2. Wait a few seconds.
    3. Go to **View > Output**.
    4. In the Output window, select `Uno Platform - Hot Reload` from the drop-down.

       ![`Uno Platform` output drop-down](../Assets/features/hotreload/vs-code-uno-platform-hr-output.png)

### [**Rider**](#tab/riderts)

- The Output window in Rider includes an output category named `Uno Platform` in its sidebar. Diagnostic messages from the Uno Platform Rider plugin appear there.

    **Steps to see the `Dev Server` output:**  
    1. In the sidebar at the bottom left of Rider, click on the Uno Platform logo.

       ![Uno Platform output logo](../Assets/features/hotreload/rider-uno-platform-output.png)
    2. In the Output window, select **LEVEL: Trace** from the drop-down.

       ![Level output drop-down](../Assets/features/hotreload/rider-output-level-trace.png)

