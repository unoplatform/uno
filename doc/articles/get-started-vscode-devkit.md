---
uid: Uno.GetStarted.vscode.DevKit
---

## How to switch to C# DevKit Mode

Note: Due to the [unavailability](https://github.com/microsoft/vscode-dotnettools/issues/765) of the **C# DevKit extension** on [OpenVSX](https://open-vsx.org) users of VSCodium, Gitpod... are always on the OmniSharp mode.

### Ensure that dotnet 8 (or later) is available for running the extension

From a terminal (external or inside VS Code) type:

```bash
dotnet --version
```

If the version listed is older than `8.0.100` then, inside VS Code, press `F1` and select `Run Uno-Check to setup this environment for Uno Platform` to update your system. Quit and restart VS Code afterward so the new version of `dotnet` can be used by the extensions.

### Install the **C# DevKit** extension

Press `F1`, select `Extensions: Install Extensions`, search the marketplace for **C# DevKit** and click the **Install** button.

> [!TIP]
> There is no need to disable `useOmniSharp` inside the settings. It is ignored when the **C# DevKit** extension is loaded by VS Code.

### Reload the window

Press `F1`, select `Developer: Reload Window` and select it. VS Code will reload itself and restart its extensions.

### Validation

You can verify that the **Uno Platform** extension is working by looking at the **Uno Platform** logs inside the **Output** pane by using `Ctrl` + `Shift` + `U` (`Shift` + `⌘` + `,` on a Mac). After reloading the window you should see a line with `[Info] Running in DevKit mode` inside the logs.
