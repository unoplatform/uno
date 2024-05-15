---
uid: Uno.GetStarted.Rider
---

# Get Started on JetBrains Rider

> [!IMPORTANT]
> As of Rider 2023.3, the Uno Platform 5.2 `net8.0-browserwasm` and `net8.0-desktop` TargetFrameworks are not supported. See [this JetBrains article](https://aka.platform.uno/rider-desktop-wasm-support) for more details. In the meantime, use our [VS Code support](xref:Uno.GetStarted.vscode) or use Uno Platform 5.1 templates as described later in this tutorial.

## Prerequisites

* [**Rider Version 2023.3+**](https://www.jetbrains.com/rider/download/)
* [**Rider Xamarin Android Support**](https://plugins.jetbrains.com/plugin/12056-rider-xamarin-android-support/) plugin from Rider in **Settings** / **Plugins**

## Check your environment

[!include[use-uno-check](includes/use-uno-check-inline-noheader.md)]

## Supported Platforms

|                       | **Rider for Windows** | **Rider for Mac**  | **Rider for Linux** |
|-----------------------|-----------------------|--------------------|---------------------|
| Windows (UWP/WinUI)   | ✔️                   | ❌                 | ❌                 |
| Android               | ✔️                   | ✔️                 | ❌†                |
| iOS                   | ✔️†                  | ✔️                 | ❌                 |
| Wasm                  | ✔️†                  | ✔️†                | ✔️†                |
| Catalyst              | ❌                   | ✔️                 | ❌                 |
| Skia Desktop          | ❌†                  | ❌†                | ❌†                |

<details>
    <summary>† Notes (Click to expand)</summary>

* **WebAssembly**: debugging from the IDE is not available yet on Rider. You can use the [Chromium in-browser debugger](external/uno.wasm.boostrap/doc/debugger-support.md#how-to-use-the-browser-debugger) instead.

* **iOS** on Windows: An attached Mac is needed, the iOS simulator will open on the Mac.

* **Android** on Linux: Xamarin.Android does not natively support Linux development. Rider has been capable of Android development on Linux in the past, but [previous directions are considered obsolete.](https://rider-support.jetbrains.com/hc/en-us/articles/360000557259--Obsolete-How-to-develop-Xamarin-Android-applications-on-Linux-with-Rider) As of this comment (3 Nov 2021) [Xamarin Android builds on Linux fail](https://github.com/xamarin/xamarin-android).

* **Skia Desktop** on all platforms: As of Rider 2023.3, the Uno Platform 5.2 `net8.0-browserwasm` and `net8.0-desktop` TargetFrameworks are not supported. See [this JetBrains article](https://aka.platform.uno/rider-desktop-wasm-support) for more details. In the meantime, use our [VS Code support](xref:Uno.GetStarted.vscode) or use Uno Platform 5.1 templates as described later in this tutorial.

</details>

## Platform specific setup

You may need to follow additional directions, depending on your development environment.

### Linux

[!include[linux-setup](includes/additional-linux-setup-inline.md)]

***

## Next Steps

You're all set! You can create your [first Uno Platform app](xref:Uno.GettingStarted.CreateAnApp.Rider).
