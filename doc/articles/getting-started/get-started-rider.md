---
uid: Uno.GetStarted.Rider
---

# Get Started on JetBrains Rider

## Prerequisites

- [**Rider Version 2024.2+**](https://aka.platform.uno/rider-version-2024-2) or [**Previous Rider Version 2024.1**](https://aka.platform.uno/rider-version-2024-1)
- [**Rider Xamarin Android Support**](https://plugins.jetbrains.com/plugin/12056-rider-xamarin-android-support/) plugin from Rider in **Settings** / **Plugins**

## Check your environment

[!include[use-uno-check](../includes/use-uno-check-inline-noheader.md)]

## Supported Platforms

|                       | **Rider for Windows** | **Rider for Mac**  | **Rider for Linux** |
|-----------------------|-----------------------|--------------------|---------------------|
| Windows (WinUI)       | ✔️                   | ❌                 | ❌                 |
| Android               | ✔️                   | ✔️                 | ✔️                |
| iOS                   | ❌                   | ✔️                 | ❌                 |
| Wasm                  | ✔️†                  | ✔️†                | ✔️†                |
| Skia Desktop          | ✔️                   | ✔️                 | ✔️                |

<details>
    <summary>† Notes (Click to expand)</summary>

- **WebAssembly**: debugging from the IDE is not available yet on Rider. You can use the [Chromium in-browser debugger](xref:UnoWasmBootstrap.Features.Debugger#how-to-use-the-browser-debugger) instead.

</details>

## Install the Uno Platform plugin

In Rider, in the **Settings**, **Plugins** menu, open the **Marketplace** tab, then search for **Uno Platform**:

![Visual Studio Installer - .NET desktop development workload](Assets/ide-rider-plugin-search.png)

Then click the install button.

## Platform-specific setup

You may need to follow additional directions, depending on your development environment.

### Android & iOS

For assistance configuring Android or iOS emulators, see the [Android & iOS emulator troubleshooting guide](xref:Uno.UI.CommonIssues.MobileDebugging).

### Linux

[!include[linux-setup](../includes/additional-linux-setup-inline.md)]

---

## Next Steps

You're all set! You can create your [first Uno Platform app](xref:Uno.GettingStarted.CreateAnApp.Rider).

> [!IMPORTANT]
> Project templates from Uno Platform 5.3 or later are needed to use Rider. See our [migration guide](xref:Uno.Development.MigratingFromPreviousReleases) to upgrade.
>
> [!IMPORTANT]
> Depending on the version of Rider you will want to use, here are some additional information that you will want to know:
>
> - **Current Rider (2024.2 and above)**: For the current Rider version, the Uno Platform plugin supports creating Uno Platform projects using the "New Solution" dialog.
>
> - **Previous Rider (2024.1)**: The Uno Plugin for previous versions of Rider (2024.1) does not support creating Uno Platform projects using the "New Solution" dialog, even if the Uno Platform project template appears. In this case, creating an Uno Platform project is done [using dotnet new](xref:Uno.GetStarted.dotnet-new) and the <a target="_blank" href="https://aka.platform.uno/app-wizard">Uno Platform Live Wizard</a>.
