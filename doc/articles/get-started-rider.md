---
uid: Uno.GetStarted.Rider
---

## Get Started on JetBrains Rider

## Prerequisites
* [**Rider Version 2023.2+**](https://www.jetbrains.com/rider/download/)
* [**Rider Xamarin Android Support**](https://plugins.jetbrains.com/plugin/12056-rider-xamarin-android-support/) plugin from Rider in **Settings** / **Plugins**

## Check your environment
[!include[getting-help](use-uno-check-inline-noheader.md)]

## Supported Platforms

|                       | **Rider for Linux** | **Rider for Mac** | **Rider for Windows** |
|-----------------------|---------------------|-------------------|-----------------------|
| Windows (UWP/WinUI)   | ❌                 | ❌                | ✔️                   |
| Android               | ❌†                | ✔️                | ✔️                   |
| iOS                   | ❌                 | ✔️                | ✔️†                  |
| Wasm                  | ✔️†                | ✔️†               | ✔️†                  |
| Catalyst              | ❌                 | ✔️                | ❌                   |
| Skia-GTK (Linux)      | ✔️                 | ✔️†               | ✔️                   |
| Skia-WPF              | ❌                 | ❌                | ✔️                   |

<details>
    <summary>† Notes (Click to expand)</summary>

  * **WebAssembly**: debugging from the IDE is not available yet on Rider.  But you can use the [Chromium in-browser debugger](external/uno.wasm.boostrap/doc/debugger-support.md#how-to-use-the-browser-debugger) instead.

  * **iOS** on Windows: An attached Mac is needed, the iOS simulator will open on the Mac.

  * **Android** on Linux: Xamarin.Android does not natively support Linux development. Rider has been capable of Android development on Linux in the past, but [previous directions are considered obsolete.](https://rider-support.jetbrains.com/hc/en-us/articles/360000557259--Obsolete-How-to-develop-Xamarin-Android-applications-on-Linux-with-Rider) As of this comment (3 Nov 2021) [Xamarin Android builds on Linux fail](https://github.com/xamarin/xamarin-android).
</details>

## Platform specific setup

You may need to follow additional directions, depending on your development environment.

# [**Windows**](#tab/windows)

[!include[windows-setup](additional-windows-setup-inline.md)]

# [**Linux**](#tab/linux)

[!include[linux-setup](additional-linux-setup-inline.md)]

# [**macOS**](#tab/macos)

[!include[macos-setup](additional-macos-setup-inline.md)]

***

## Next Steps

You're all set! You can create your [first Uno Platform app](xref:Uno.GettingStarted.CreateAnApp.Rider).
