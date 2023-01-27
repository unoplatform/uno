### Select your development environment

Uno Platform allows you to create single-codebase, cross-platform applications which run on iOS, Android, Web, macOS, Linux and Windows. You'll be creating cross-platform .NET applications with XAML and C# in no time. 

The following sections will guide you through your development environment setup, a simple Hello World app, and more advanced tutorials. 

To set up your development environment, first select the operating system you're developing on.

**I am developing on...**

# [**Windows**](#tab/windows)

If you're developing on Windows, we recommend you use [**Visual Studio 2022**](get-started-vs-2022.md), for the richest XAML development experience and broadest platform coverage. 

If you already use and love **JetBrains Rider** or **Visual Studio Code**, you can also use them to develop Uno Platform applications. Check the support matrix below to see which target platforms they support.

**Choose the IDE you want to use:**

 - [Get started with VS Code, Codespaces and GitPod](get-started-vscode.md)
 - [Get started with Rider](get-started-rider.md)

 #### Target platform coverage by IDE on Windows

|                                                  | Windows 10/11 (UWP/WinUI)  | Android | iOS | Web (WebAssembly) | mac Catalyst | macOS (Skia-Gtk) | Linux (Skia-Gtk) | Windows 7+ (Skia-WPF) |
|--------------------------------------------------|----------------------------|---------|-----|-------------------|--------------|------------------|------------------|-----------------------|
| [**Visual Studio**](get-started-vs-2022.md)      | ✔️                         | ✔️     | ✔️† | ✔️               | ❌           | ✔️               | ✔️              | ✔️                   |
| [**VS Code**](get-started-vscode.md)             | ❌                         | ❌     | ❌  | ✔️               | ❌           | ✔️               | ✔️              | ✔️                   |
| [**Codespaces / Gitpod**](get-started-vscode.md) | ❌                         | ❌     | ❌  | ✔️               | ❌           | ✔️               | ✔️              | ✔️                   |
| [**JetBrains Rider**](get-started-rider.md)      | ✔️                         | ✔️     | ✔️† | ✔️               | ❌           | ✔️             | ✔️              | ✔️                   |


 † You'll need to be connected to a Mac to run and debug iOS apps from Windows.

# [**macOS**](#tab/macos)

You can use **Visual Studio for Mac**, **JetBrains Rider**, or **Visual Studio Code** to build Uno Platform applications on macOS. See the support matrix below for supported target platforms.

**Choose the IDE you want to use:**

 - [Get started with Visual Studio for Mac](get-started-vsmac.md)
 - [Get started with VS Code, Codespaces and GitPod](get-started-vscode.md)
 - [Get started with Rider](get-started-rider.md)

 #### Target platform coverage by IDE on macOS

|                                                   | Windows 10/11(UWP/WinUI)| Android | iOS | Web (WebAssembly) | mac Catalyst | macOS (Skia-Gtk) | Linux (Skia-Gtk) | Windows 7+ (Skia-WPF) |
|---------------------------------------------------|-------------------------|---------|-----|-------------------|-------|--------|-------------------|-----------------------|
| [**Visual Studio for Mac**](get-started-vsmac.md) | ❌                      | ✔️     | ✔️ | ❌                | ✔️    | ✔️    | ✔️               | ❌                   |
| [**VS Code**](get-started-vscode.md)              | ❌                      | ❌     | ❌ | ✔️                | ❌    | ✔️    | ✔️               | ❌                   |
| [**Codespaces / Gitpod**](get-started-vscode.md)  | ❌                      | ❌     | ❌ | ✔️                | ❌    | ✔️    | ✔️               | ✔️                   |
| [**JetBrains Rider**](get-started-rider.md)       | ❌                      | ✔️     | ✔️ | ✔️                | ✔️    | ✔️    | ✔️               | ❌                   |

# [**Linux**](#tab/linux)

 You can use either **JetBrains Rider** or **Visual Studio Code** to build Uno Platform applications on Linux. See the support matrix below for supported target platforms.
 
**Choose the IDE you want to use:**

 - [Get started with Visual Studio Code, Codespaces and GitPod](get-started-vscode.md)
  - [Get started with Rider](get-started-rider.md)

 There's [additional information here](get-started-with-linux.md) about developing from, and for, Linux with Uno Platform.

 #### Target platform coverage by IDE on Linux

|                                                   | Windows 10/11(UWP/WinUI)| Android | iOS | Web (WebAssembly) | mac Catalyst | macOS (Skia-Gtk) | Linux (Skia-Gtk) | Windows 7+ (Skia-WPF) |
|---------------------------------------------------|------------------------|---------|-----|--------------------|-------|-------|------------------|-----------------------|
| [**VS Code**](get-started-vscode.md)              | ❌                    | ❌      | ❌  | ✔️                | ❌  | ✔️   | ✔️              | ❌                    |
| [**Codespaces / Gitpod**](get-started-vscode.md)  | ❌                    | ❌      | ❌  | ✔️                | ❌  | ✔️   | ✔️              | ❌                    |
| [**JetBrains Rider**](get-started-rider.md)       | ❌                    | ❌†      | ❌  | ✔️                | ❌  | ✔️   | ✔️              | ❌                    |

† Notes:

* **Android** on Linux: Xamarin.Android does not natively support linux development. Rider has been capable of Android development on Linux in the past, but [previous directions are considered obsolete.](https://rider-support.jetbrains.com/hc/en-us/articles/360000557259--Obsolete-How-to-develop-Xamarin-Android-applications-on-Linux-with-Rider) As of this comment (3 Nov 2021) [Xamarin Android builds on linux fail](https://github.com/xamarin/xamarin-android).

***
