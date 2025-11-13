---
uid: Uno.GetStarted.vscode.DevKit
---

# How to switch to C# Dev Kit Mode

> [!NOTE]
> Due to the [unavailability](https://github.com/microsoft/vscode-dotnettools/issues/765) of the **C# Dev Kit extension** on [OpenVSX](https://open-vsx.org), users of VSCodium, Ona... are always on the OmniSharp mode. Visit [this guide](xref:Uno.GetStarted.vscode.OmniSharp) if you need to use Omnisharp with VSCode.

## Ensure you are using either .NET 8 (or later) or Uno.WinUI 5.0.116 (or later)

C# Dev Kit requires Uno Platform extension 0.12 or later. The extension itself need .NET 8 (or later) or your projects need to use Uno Platform 5.0.116 (or later).

### .NET 8

From a terminal (external or inside VS Code) type:

```dotnetcli
dotnet --version
```

If the version listed is older than `8.0.100` then, inside VS Code, press `F1` and select `Run Uno-Check to setup this environment for Uno Platform` to update your system. Quit and restart VS Code afterward so the new version of `dotnet` can be used by the extensions.

If you have version `8.0.100` (or later) then there's no need to update your version of .NET or Uno.

### Uno.WinUI 5.0.116 or later

The latest versions of Uno.WinUI ship with an additional MSBuild task, so running .NET 8 is not required.

To see which version of Uno.WinUI you're currently using, open the `Directory.Packages.props` file at the root of your project and look for `Uno.WinUI`, e.g.

```xml
<PackageVersion Include="Uno.WinUI" Version="5.0.116" />
```

If you have version 5.0.116 (or later), then there's no need to update your version of .NET or Uno to use VS Code.

If you need to update you can use a tool like [`dotnet outdated`](https://github.com/dotnet-outdated/dotnet-outdated) to update your dependencies to the latest available. Also see [Migrating from previous releases](xref:Uno.Development.MigratingFromPreviousReleases) for additional information about updating your project(s).

## Disable OmniSharp

If you used the OmniSharp mode then you need to disable the `preferCSharpExtension` setting.

* Open the VS Code Settings using `Ctrl` + `,` (or `⌘` + `,` on a Mac)
* Search for `preferCSharpExtension`
    ![preferCSharpExtension](Assets/quick-start/vs-code-preferCSharpExtension.png)
* Disable it (checkbox)

> [!NOTE]
> You do not have to disable `useOmnisharp` as C# Dev Kit will ignore it if `preferCSharpExtension` is not set.

## Reload the window

Press `F1`, select `Developer: Reload Window` and select it. VS Code will reload itself and restart its extensions.

## Validation

You can verify that the **Uno Platform** extension is working by looking at the **Uno Platform** logs inside the **Output** pane by using `Ctrl` + `Shift` + `U` (`Shift` + `⌘` + `,` on a Mac). After reloading the window you should see a line with `[Info] Running in Dev Kit mode` inside the logs.
