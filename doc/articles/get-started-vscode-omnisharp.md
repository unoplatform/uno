---
uid: Uno.GetStarted.vscode.OmniSharp
---

# How to switch to OmniSharp Mode

In order to enable OmniSharp you need to downgrade the Uno Platform extension to version 0.11.0. This is the last released version without a dependency on C# Dev Kit.

## Disable the **C# Dev Kit** extension

The **C# extension** can only work on the legacy OmniSharp mode when the **C# Dev Kit extension** is not loaded in memory.

* Press `Ctrl` + `Shift` + `X` (or `Shift` + `⌘` + `X` on a Mac) to bring the **Extensions** side bar.
* Search for `C# Dev Kit`
* Select it, a new tab will open with the extension page
* Click on the **Disable** button

> [!TIP]
> You might have to disable other extensions, including the Uno Platform extension, that depend on C# Dev Kit before being able to disable it.

* Click on the **Disable** button again (if you had to disable other extensions)
* Click on the **Reload Required** button

## Enable OmniSharp mode

* Open the VS Code Settings using `Ctrl` + `,` (or `⌘` + `,` on a Mac)
* Search for `useOmnisharp`
    ![useOmnisharp](Assets/quick-start/vs-code-useOmniSharp.png)
* Enable it (checkbox)
* Press `Ctrl` + `Shift` + `X` (or `Shift` + `⌘` + `X` on a Mac) to bring the **Extensions** side bar.
* Search for `Uno Platform`
* Select it, a new tab will open with the extension page
* Click on the **Uninstall** drop-down button and select **Install Another Version...**
* Select **0.11.0**
* Click on the **Enable** button

## Reload the window

Press `F1` and select `Developer: Reload Window`. VS Code will reload itself and restart its extensions.

## Validation

You can verify that the **Uno Platform** extension is working by looking at the **Uno Platform** logs inside the **Output** pane by using `Ctrl` + `Shift` + `U` (`Shift` + `⌘` + `,` on a Mac). After reloading the window you should see a line with `[Info] Requesting available projects from OmniSharp` inside the logs.