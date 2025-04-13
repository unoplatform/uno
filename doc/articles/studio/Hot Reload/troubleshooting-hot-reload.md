---
uid: Uno.Studio.HotReload.Troubleshooting
---

# Troubleshooting Hot Reload

## [**Common issues**](#tab/common-issues)

- If the Hot Reload Indicator is red and shows a connection failure, ensure that you have the [latest stable version](https://www.nuget.org/packages/Uno.Sdk/latest) of [Uno.SDK](xref:Uno.Features.Uno.Sdk) and the latest version of your IDE’s extension ([Visual Studio](https://aka.platform.uno/vs-extension-marketplace), [Visual Studio Code](https://aka.platform.uno/vscode-extension-marketplace), or [Rider](https://aka.platform.uno/rider-extension-marketplace)). Additionally, [re-run Uno.Check](xref:UnoCheck.UsingUnoCheck) to update dependencies, then relaunch your IDE, [sign in with your Uno Platform account](xref:Uno.GetStarted.Licensing), and rebuild your application.

  For existing applications, refer to our [migration guide](xref:Uno.Development.MigratingFromPreviousReleases) for upgrade steps.
  > [!IMPORTANT]
  > When upgrading to **Uno.Sdk 5.5 or higher**, the `EnableHotReload()` method in `App.xaml.cs` is deprecated and should be replaced with `UseStudio()`.
- Observe the application logs, you should see diagnostics messages in the app when a XAML file is reloaded.
- WinAppSDK on Windows-specific issues
  - Grid Succinct syntax [is not supported](https://github.com/microsoft/microsoft-ui-xaml/issues/7043#issuecomment-1120061686)
- You can troubleshoot **Hot Reload** further by adjusting the **logging level** in your app.

  - **For Blank App Preset** (in `App.xaml.cs`, inside `InitializeLogging`):
  
    ```csharp
    // Adjust logging level
    builder.SetMinimumLevel(LogLevel.Debug); // or LogLevel.Trace

    // Uncomment and adjust logging level
    builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Debug); // or LogLevel.Trace
    ```

  - **For Recommended App Preset** (in `App.xaml.cs`, inside `UseLogging`):

    ```csharp
    // Adjust logging level
    logBuilder.SetMinimumLevel(LogLevel.Debug); // or LogLevel.Trace

    // Uncomment and adjust logging level
    logBuilder.HotReloadCoreLogLevel(LogLevel.Debug); // or LogLevel.Trace
    ```

  The diagnostic messages will appear in the app's **Debug Output**.

  If you need to share logs when opening an issue on the GitHub [Uno Platform repository](https://github.com/unoplatform/uno), it is recommended to set `LogLevel` to **Trace** to provide the most detailed logs for investigation.

- If you're getting `ENC0003: Updating 'attribute' requires restarting the application`, add the following in the `Directory.Build.props` (or in each .csproj project head):

  ```xml
  <PropertyGroup>
    <!-- Required for Hot Reload (See https://github.com/unoplatform/uno.templates/issues/376) -->
    <GenerateAssemblyInfo Condition="'$(Configuration)'=='Debug'">false</GenerateAssemblyInfo>
  </PropertyGroup>
  ```
  
  Also [make sure](https://github.com/dotnet/sdk/issues/36666#issuecomment-2162173453) that you're not referencing `Microsoft.SourceLink.*` packages.

- If you're getting the `Unable to access Dispatcher/DispatcherQueue` error, you'll need to update your app to Uno.Sdk 5.6 or later, and update your `App.cs` file:

    ```csharp
    using Uno.UI;

    //... in the OnLaunched method

    #if DEBUG
            MainWindow.UseStudio();
    #endif
    ```

## [**Visual Studio 2022**](#tab/vswints)

- Ensure that **C# Hot Reload** is enabled in Visual Studio by going to **Tools > Options**, searching for **.NET / C++ Hot Reload**, and making sure the following checkboxes are checked:
  - ✅ **Enable Hot Reload when debugging**
  - ✅ **Enable Hot Reload without debugging**
  - ✅ **Apply Hot Reload on File Save**
- The Output window in Visual Studio includes an output category named `Uno Platform` in its drop-down menu. Diagnostic messages from the Uno Platform VS extension appear there. To enable logging, you need to set **MSBuild project build output verbosity** to **at least "Normal"** (above "Minimal"). These changes should take effect immediately without requiring a Visual Studio restart. However, if you do not see additional logs, try restarting Visual Studio. For more details on build log verbosity, refer to the [official Visual Studio documentation](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-view-save-and-configure-build-log-files?view=vs-2022#to-change-the-amount-of-information-included-in-the-build-log).  

    If you need to share logs when opening an issue on the GitHub [Uno Platform repository](https://github.com/unoplatform/uno), it is recommended to set verbosity to **Diagnostic** to provide the most detailed logs for investigation.

    **Steps to change MSBuild output verbosity:**  
    1. Open **Tools > Options > Projects and Solutions > Build and Run**, then set **MSBuild output verbosity** to **Diagnostic** or the required level.

       ![MSBuild output verbosity drop-down](../Assets/vs-msbuild-output-verbosity.png)
    2. Restart Visual Studio, re-open your solution, and wait a few seconds.
    3. Go to **View > Output**.
    4. In the Output window, select `Uno Platform` from the drop-down.

       ![`Uno Platform` output drop-down](../Assets/vs-uno-platform-logs.png)
- When a file is reloaded, XAML parsing errors will appear in the application's logs, on the device or in the browser.
- If there are multiple versions of the Uno.WinUI Package present in the solution, the newest will be used, regardless of the started application
- For `net9.0-windows10.xx`:
  - Ensure that the `net9.0-windows10.xxx` target framework **is selected in the top-left dropdown list of the XAML editor**. Selecting any other platform will break Hot Reload.
  - [A VS issue for WinUI may be hit](https://developercommunity.visualstudio.com/t/net80-windows10-needs-to-be-first-for-W/10643724). If XAML Hot Reload does not work, ensure that the `Uno Platform` output window exists, and that it mentions that the extension has successfully loaded. To do so, try closing and reopening the solution, and make sure that the [Visual Studio extension is installed](xref:Uno.GetStarted.vs2022).
  - [A known VS issue for WinUI](https://github.com/microsoft/microsoft-ui-xaml/issues/5944) breaks Hot Reload when using "simplified" `RowDefinitions`/`ColumnDefinitions`.

## [**Visual Studio Code**](#tab/vscodets)

- Hot Reload **is not supported** when using the debugger. Start your app using `Ctrl+F5`.
- The Output window in Visual Studio Code includes an output category named `Uno Platform - Hot Reload` in its drop-down menu. Diagnostic messages from the Uno Platform VS Code extension appear there.

    **Steps to see the `Uno Platform - Hot Reload` output:**  
    1. In the status bar at the bottom left of VS Code, ensure `NameOfYourProject.csproj` is selected (by default `NameOfYourProject.sln` is selected).

       ![.csproj selection in Visual Studio Code](../Assets/vscode-csproj-selection.png)
    2. Wait a few seconds.
    3. Go to **View > Output**.
    4. In the Output window, select `Uno Platform - Hot Reload` from the drop-down.

       ![`Uno Platform` output drop-down](../Assets/vs-code-uno-platform-hr-output.png)
- Depending on your machine's performance, the Hot Reload engine may take a few moments to initialize and take your project modifications into account.
- Make sure that the selected project in the status bar (or using the "Uno Platform: Select Active Project" in the command palette) is not the solution file, but rather the project file (i.e. ending by `.csproj`).
- Align the "Debug profile" (at the top of the "Run and Debug" pane) with the platform you chose to debug within the status bar (or using the "Uno Platform: Select the Target Platform Moniker (TFM)")
  - "Uno Platform Desktop Debug" profile for `net9.0-desktop`
  - "Uno Platform Mobile Debug" profile for `net9.0-ios` and `net9.0-android`
  - "Uno Platform WebAssembly Debug" profile for `net9.0-browserwasm`
- If Hot Reload does not function properly, you can try using the `Developer: Reload Window` command in the palette (using `Ctrl+Shift+P`)
- The TCP port number used by the app to connect back to the IDE is located in the `<UnoRemoteControlPort>` property of the `[ProjectName].csproj.user` file. If the port number does not match with the one found in the `Uno Platform - Hot Reload` output window, restart Code or use `Developer: Reload Window` in the command palette.

## [**Rider**](#tab/riderts)

- Hot Reload **is not supported** when using the debugger. Start your app without the debugger.
- The Output window in Rider includes an output category named `Uno Platform` in its sidebar. Diagnostic messages from the Uno Platform Rider plugin appear there.

    **Steps to see the `Hot Reload` output:**  
    1. In the sidebar at the bottom left of Rider, click on the Uno Platform logo.

       ![Uno Platform output logo](../Assets/rider-uno-platform-output.png)
    2. In the Output window, select **LEVEL: Trace** from the drop-down.

       ![Level output drop-down](../Assets/rider-output-level-trace.png)
- Depending on your machine's performance, the Hot Reload engine may take a few moments to initialize and take your project modifications into account.
- If Hot Reload does not function properly, you can try closing and reopening the solution.
- The TCP port number used by the app to connect back to the IDE is located in the `<UnoRemoteControlPort>` property of the `[ProjectName].csproj.user` file. If the port number does not match the one found in the **Uno Platform** output window, close and reopen the solution.

---

[!INCLUDES [learn-more-about-hot-reload](includes/learn-more-about-hot-reload-inline.md)]
