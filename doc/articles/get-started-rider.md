---
uid: Uno.GetStarted.Rider
---

# Get Started on JetBrains Rider

> [!IMPORTANT]
> Project templates from Uno Platform 5.3 or later are needed to use Rider. See our [migration guide](xref:Uno.Development.MigratingFromPreviousReleases) to upgrade.

## Prerequisites

* [**Rider Version 2024.1+**](https://www.jetbrains.com/rider/download/)
* [**Rider Xamarin Android Support**](https://plugins.jetbrains.com/plugin/12056-rider-xamarin-android-support/) plugin from Rider in **Settings** / **Plugins**

## Check your environment

[!include[use-uno-check](includes/use-uno-check-inline-noheader.md)]

## Supported Platforms

|                       | **Rider for Windows** | **Rider for Mac**  | **Rider for Linux** |
|-----------------------|-----------------------|--------------------|---------------------|
| Windows (UWP/WinUI)   | ✔️                   | ❌                 | ❌                 |
| Android               | ✔️                   | ✔️                 | ✔️                |
| iOS                   | ❌                   | ✔️                 | ❌                 |
| Wasm                  | ✔️†                  | ✔️†                | ✔️†                |
| Catalyst              | ❌                   | ✔️                 | ❌                 |
| Skia Desktop          | ✔️                   | ✔️                 | ✔️                |

<details>
    <summary>† Notes (Click to expand)</summary>

* **WebAssembly**: debugging from the IDE is not available yet on Rider. You can use the [Chromium in-browser debugger](xref:UnoWasmBootstrap.Features.Debugger#how-to-use-the-browser-debugger) instead.

</details>

## Install the Uno Platform plugin

In Rider, in the **Configure**, **Plugins** menu, open the **Marketplace** tab, then search for **Uno Platform**:

![Visual Studio Installer - .NET desktop development workload](Assets/ide-rider-plugin-search.png)

Then click the install button.

## Platform specific setup

You may need to follow additional directions, depending on your development environment.

### Linux

[!include[linux-setup](includes/additional-linux-setup-inline.md)]

---

## Next Steps

You're all set! You can create your [first Uno Platform app](xref:Uno.GettingStarted.CreateAnApp.Rider).
