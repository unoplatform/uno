---
uid: Uno.GetStarted
---

## Quick Start

1. Install and run <a href="https://aka.platform.uno/uno-check#install-and-run-uno-check" target="_blank">Uno.Check</a> to set up all the required pre-requisites
2. Download the Uno Platform extension for your IDE:

   <!-- markdownlint-disable MD001 MD009 -->

   <div class="row">

   <!-- Visual Studio -->
   <div class="col-md-4 col-xs-12 ">
   <a href="https://aka.platform.uno/vs-extension-marketplace" target="_blank">
   <div class="alert alert-info alert-hover">

   #### Visual Studio 2022

   Get the VSIX from the marketplace
   </div>
   </a>
   </div>

   <!-- Code -->
   <div class="col-md-4 col-xs-12 ">
   <a href="https://aka.platform.uno/vscode-extension-marketplace" target="_blank">
   <div class="alert alert-info alert-hover">

   #### Visual Studio Code

   Install the VSIX from the marketplace
   </div>
   </a>
   </div>

   <!-- Rider -->
   <div class="col-md-4 col-xs-12 ">
   <a href="https://aka.platform.uno/rider-extension-marketplace" target="_blank">
   <div class="alert alert-info alert-hover">

   #### JetBrains Rider

   Get the extension from the marketplace
   </div>
   </a>
   </div>

   </div> <!-- row -->

## Get Started

Uno Platform allows you to create single-codebase, cross-platform applications that run on iOS, Android, Web, macOS, Linux and Windows. You'll be creating cross-platform .NET applications with XAML and/or C# in no time.

The following sections will guide you through your development environment setup, a simple Hello World app, and more advanced tutorials.

To set up your development environment, first select the operating system you're developing on.

**I am developing on...**

# [**Windows**](#tab/windows)

If you're developing on Windows, we recommend you use [**Visual Studio 2022**](xref:Uno.GetStarted.vs2022), for the richest XAML development experience and broadest platform coverage.

If you already use and love **JetBrains Rider** or **Visual Studio Code**, you can also use them to develop Uno Platform applications. Check the support matrix below to see which target platforms they support.

**Choose the IDE you want to use:**

- [Visual Studio 2022](xref:Uno.GetStarted.vs2022)
- [VS Code, Codespaces and GitPod](xref:Uno.GetStarted.vscode)
- [Rider](xref:Uno.GetStarted.Rider)

To help you choose the appropriate IDE, the following table shows the compatibility of different development environments with various target platforms:

|                                    | [**Visual Studio**](xref:Uno.GetStarted.vs2022) | [**VS Code**](xref:Uno.GetStarted.vscode) | [**Codespaces / Gitpod**](xref:Uno.GetStarted.vscode) | [**JetBrains Rider**](xref:Uno.GetStarted.Rider) |
|------------------------------------|-------------------------------------------------|--------------------------------------------|-------------------------------------------------------|--------------------------------------------------|
| Windows 10/11 (WinUI)              | ✔️                                              | ❌                                         | ❌                                                   | ✔️                                              |
| Android                            | ✔️                                              | ✔️                                         | ❌                                                   | ✔️                                              |
| iOS                                | ✔️†                                             | ✔️††                                       | ❌                                                   | ❌                                              |
| Web (WebAssembly)                  | ✔️                                              | ✔️                                         | ✔️                                                   | ✔️†††                                           |
| Skia Desktop (Windows/Linux/macOS) | ✔️                                              | ✔️                                         | ✔️                                                   | ✔️                                              |

- † You will need to be connected to a Mac to run and debug iOS apps from Windows.
- †† You will need to be connected to a Mac using [Remote - SSH](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-ssh)
- ††† [WebAssembly debugging](https://youtrack.jetbrains.com/issue/RIDER-103346/Uno-Platform-for-WebAssembly-debugger-support) is not yet supported

# [**macOS**](#tab/macos)

You can use **Visual Studio Code** or **JetBrains Rider**, to build Uno Platform applications on macOS. See the support matrix below for supported target platforms.

**Choose the IDE you want to use:**

- [Get started with VS Code, Codespaces and GitPod](xref:Uno.GetStarted.vscode)
- [Get started with Rider](xref:Uno.GetStarted.Rider)

To help you choose the appropriate IDE, the following table shows the compatibility of different development environments with various target platforms:

|                                    | [**VS Code**](xref:Uno.GetStarted.vscode) | [**Codespaces / Gitpod**](xref:Uno.GetStarted.vscode) | [**JetBrains Rider**](xref:Uno.GetStarted.Rider) |
|------------------------------------|------------------------------------------|-------------------------------------------------------|--------------------------------------------------|
| Windows 10/11 (WinUI)              | ❌                                       | ❌                                                   | ❌                                               |
| Android                            | ✔️                                       | ❌                                                   | ✔️                                               |
| iOS                                | ✔️                                       | ❌                                                   | ✔️                                               |
| Web (WebAssembly)                  | ✔️                                       | ✔️                                                   | ✔️                                               |
| Skia Desktop (Windows/Linux/macOS) | ✔️                                       | ✔️                                                   | ✔️                                               |

The latest macOS release and Xcode version are required to develop with Uno Platform for iOS targets. If you have older Mac hardware that does not support the latest release of macOS, see the section for [Developing on older Mac hardware](xref:Uno.UI.CommonIssues.Ios#developing-on-older-mac-hardware).

# [**Linux**](#tab/linux)

 You can use either **JetBrains Rider** or **Visual Studio Code** to build Uno Platform applications on Linux. See the support matrix below for supported target platforms.

**Choose the IDE you want to use:**

- [Get started with Visual Studio Code, Codespaces and GitPod](xref:Uno.GetStarted.vscode)
- [Get started with Rider](xref:Uno.GetStarted.Rider)

To help you choose the appropriate IDE, the following table shows the compatibility of different development environments with various target platforms:

|                                    | [**VS Code**](xref:Uno.GetStarted.vscode) | [**Codespaces / Gitpod**](xref:Uno.GetStarted.vscode) | [**JetBrains Rider**](xref:Uno.GetStarted.Rider) |
|------------------------------------|------------------------------------------|-------------------------------------------------------|--------------------------------------------------|
| Windows 10/11 (WinUI)              | ❌                                        | ❌                                                     | ❌                                            |
| Android                            | ✔️                                        | ❌                                                     | ✔️                                            |
| iOS                                | ❌                                        | ❌                                                     | ❌                                            |
| Web (WebAssembly)                  | ✔️                                        | ✔️                                                     | ✔️†                                           |
| Skia Desktop (Windows/Linux/macOS) | ✔️                                        | ✔️                                                     | ✔️                                            |

**Notes:**

- † [WebAssembly debugging](https://youtrack.jetbrains.com/issue/RIDER-103346/Uno-Platform-for-WebAssembly-debugger-support) is not yet supported

---

## Questions

For questions about Uno Platform, refer to the [general FAQ](xref:Uno.Development.FAQ) or see the [troubleshooting section](xref:Uno.UI.CommonIssues) for common issues and their solutions.

## Next Steps

Choose the IDE to Learn more about:

- [Visual Studio 2022](xref:Uno.GetStarted.vs2022)
- [VS Code, Codespaces and GitPod](xref:Uno.GetStarted.vscode)
- [Rider](xref:Uno.GetStarted.Rider)
