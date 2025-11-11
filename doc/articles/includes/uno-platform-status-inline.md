# Uno Platform Status

The Uno Platform Status surfaces what Uno Platform is doing behind the scenes when you open a solution in your IDE. It provides clear, real-time feedback so you know when everything is ready to run, and highlights issues early to avoid blocking your development flow.

## [**Visual Studio 2022**](#tab/vswints)

![Uno Platform Status](../Assets/uno-platform-status.png)

## [**Visual Studio Code**](#tab/vscodets)

![Uno Platform Status](../Assets/uno-platform-status-code.png)

## [**Rider**](#tab/riderts)

![Uno Platform Status](../Assets/uno-platform-status-rider.png)

---

## What it does

- Communicates environment checks and setup steps (e.g. detecting Uno packages, waiting for restore)
- Shows readiness so you can confidently start your app
- Reports problems with actionable guidance so you can fix them quickly

## When it appears

The Uno Platform Status appears as soon as you install the Uno extension in your IDE and open a solution:

- Visual Studio: https://platform.uno/visual-studio/
- Rider: https://platform.uno/rider/
- Visual Studio Code: https://platform.uno/vscode/

> [!NOTE]
> The Uno Platform Status can also appear for non-Uno solutions. This helps you troubleshoot when you expected Uno packages to be present but they were not detected.

## NuGet

To ensure consistency, the Uno tooling aligns itself with the package versions referenced by your solution. The IDE extension waits for NuGet restore to complete before enabling Uno features.

- If NuGet restore is still running or fails, Uno features will remain disabled until restore succeeds.
- Resolve any restore errors first; then the panel updates automatically when ready.

> [!TIP]
> If restore takes longer than expected, check your network connectivity and any corporate proxy settings, then retry restore from your IDE.

## Typical statuses you may see

- Checking environment: scanning the solution and active projects
- Waiting for NuGet restore: enabling features after packages are restored
- Ready: all checks passed, you can start the app
- Warning or Error: details and suggested actions are provided

## Troubleshooting

### [**Visual Studio 2022**](#tab/vswints)

- The Output window in Visual Studio includes an output category named `Uno Platform - Dev Environment`. Diagnostic messages from the Uno Platform VS extension appear there. To enable logging, set MSBuild project build output verbosity to at least "Normal" (above "Minimal"). These changes should take effect immediately without a restart; if you do not see additional logs, try restarting Visual Studio. For more details on build log verbosity, refer to the [official Visual Studio documentation](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-view-save-and-configure-build-log-files?view=vs-2022#to-change-the-amount-of-information-included-in-the-build-log).  

    If you need to share logs when opening an issue on the GitHub [Uno Platform repository](https://github.com/unoplatform/uno), set verbosity to **Diagnostic** to provide the most detailed logs for investigation.

    **Steps to change MSBuild output verbosity:**
    1. Open **Tools > Options > Projects and Solutions > Build and Run**, then set **MSBuild output verbosity** to **Diagnostic** or the required level.

       ![MSBuild output verbosity drop-down](../Assets/features/hotreload/vs-msbuild-output-verbosity.png)
    2. Restart Visual Studio, re-open your solution, and wait a few seconds.
    3. Go to **View > Output**.
    4. In the Output window, select `Uno Platform - Dev Environment` from the drop-down.

       ![Uno Platform output drop-down](../Assets/features/hotreload/vs-uno-platform-logs.png)

### [**Visual Studio Code**](#tab/vscodets)

- The Output window in Visual Studio Code includes an output category named `Uno Platform` in its drop-down menu. Diagnostic messages from the Uno Platform VS Code extension appear there.

    **Steps to see the `Uno Platform` output:**  
    1. In the status bar at the bottom left of VS Code, ensure `NameOfYourProject.csproj` is selected (by default `NameOfYourProject.sln` is selected).

       ![.csproj selection in Visual Studio Code](../Assets/features/hotreload/vscode-csproj-selection.png)
    2. Wait a few seconds.
    3. Go to **View > Output**.
    4. In the Output window, select `Uno Platform` from the drop-down.

       ![`Uno Platform` output drop-down](../Assets/features/hotreload/vs-code-uno-platform-hr-output.png)

### [**Rider**](#tab/riderts)

- The Output window in Rider includes an output category named `Uno Platform` in its sidebar. Diagnostic messages from the Uno Platform Rider plugin appear there.

    **Steps to see the `Hot Reload` output:**  
    1. In the sidebar at the bottom left of Rider, click on the Uno Platform logo.

       ![Uno Platform output logo](../Assets/features/hotreload/rider-uno-platform-output.png)
    2. In the Output window, select **LEVEL: Trace** from the drop-down.

       ![Level output drop-down](../Assets/features/hotreload/rider-output-level-trace.png)

---

## Quick checks

- NuGet restore completed successfully (no errors in Package Manager or Build output)
- Uno Platform packages referenced by the projects you expect to use Uno
- Visual Studio/MSBuild output verbosity set high enough to see diagnostic logs when needed
- If the panel reports an action (e.g., retry restore or open logs), follow it and re-check the status
