---
uid: Uno.GetStarted.vscode.OmniSharp
---

## How to switch to OmniSharp Mode

### Disable (or uninstall) the **C# DevKit** extension

The **C# extension** can only work on the legacy OmniSharp mode when the **C# DevKit extension** is not loaded in memory.

* Press `Ctrl` + `Shift` + `X` (or `Shift` + `⌘` + `X` on a Mac) to bring the **Extensions** side bar.
* Search for `C# DevKit`
* Select it and click on the **Uninstall** button

> [!TIP]
> You might have to uninstall other extensions (e.g. MAUI) that depends on C# DevKit before being able to uninstall it.

### Enable OmniSharp mode

* Open the VS Code Settings using `Ctrl` + `,` (or `⌘` + `,` on a Mac)
* Search for `useOmnisharp`
* Enable it (checkbox)

![useOmnisharp](Assets/quick-start/vs-code-useOmniSharp.png)

### Reload the window

The **C# extension** should detect the change and ask you to reload the window. Please do so.

If not asked then press `F1` and select `Developer: Reload Window`. VS Code will reload itself and restart its extensions.

### Validation

You can verify that the **Uno Platform** extension is working by looking at the **Uno Platform** logs inside the **Output** pane by using `Ctrl` + `Shift` + `U` (`Shift` + `⌘` + `,` on a Mac). After reloading the window you should see a line with `[Info] Running in OmniSharp mode` inside the logs.
