---
uid: Uno.GetStarted
---

> [!TIP]
> Before you begin, review the [System Requirements](xref:Uno.GetStarted.SystemRequirements) to ensure your development environment is properly configured.

## Quick Start

1. Install and run <a href="https://aka.platform.uno/uno-check#install-and-run-uno-check" target="_blank">Uno.Check</a> to set up all the required pre-requisites
2. Download the Uno Platform extension for your IDE:

   <!-- markdownlint-disable MD001 MD009 -->

   <div class="row">

   <!-- Visual Studio -->
   <div class="col-md-4 col-xs-12 ">
   <a href="https://aka.platform.uno/vs-extension-marketplace" target="_blank">
   <div class="alert alert-info alert-hover">

   #### Visual Studio 2022/2026

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

The following sections will guide you through your development environment (including AI agents), a simple Hello World app, and more advanced tutorials.

To set up your development environment, first select the operating system you're developing on.

**I am developing on...**

# [**Windows**](#tab/windows)

**Choose the IDE or Agent you want to use:**

- [Visual Studio 2022/2026 with Copilot](xref:Uno.GetStarted.vs2022)
- [VS Code with Copilot, Codespaces and Ona](xref:Uno.GetStarted.vscode)
- [Rider](xref:Uno.GetStarted.Rider)
- [Claude Code](xref:Uno.GetStarted.AI.Claude)
- [Codex CLI](xref:Uno.GetStarted.AI.Codex)
- [GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI)
- [Cursor](xref:Uno.GetStarted.AI.Cursor)

To help you choose the appropriate IDE or Agent, the following table shows the compatibility of different development environments with various target platforms:

|                                    | [**Visual Studio**](xref:Uno.GetStarted.vs2022) | [**VS Code**](xref:Uno.GetStarted.vscode) | [**Codespaces**](xref:Uno.GetStarted.vscode) | [**Rider**](xref:Uno.GetStarted.Rider) | [**Claude Code**](xref:Uno.GetStarted.AI.Claude) | [**Codex**](xref:Uno.GetStarted.AI.Codex) | [**GitHub Copilot CLI**](xref:Uno.GetStarted.AI.CopilotCLI) | [**Cursor**](xref:Uno.GetStarted.AI.Cursor) |
|------------------------------------|-------------------------------------------------|--------------------------------------------|-------------------------------------------------------|--------------------------------------------------|--------------------------------------------------|-------------------------------------------|-------------------------------------------------------------|---------------------------------------------|
| Desktop (Skia)¹                    | ✔️                                              | ✔️                                         | ✔️                                                   | ✔️                                              | ✔️                                               | ✔️                                        | ✔️                                                         | ✔️                                          |
| Android                            | ✔️                                              | ✔️⁵                                         | ❌                                                   | ✔️                                              | ⏳⁵                                              | ⏳⁵                                       | ⏳⁵                                                        | ⏳⁵                                         |
| iOS                                | ✔️²                                             | ✔️³                                        | ❌                                                   | ❌                                              | ❌                                               | ❌                                        | ❌                                                         | ❌                                          |
| Web (WebAssembly)                  | ✔️                                              | ✔️                                         | ✔️                                                   | ✔️⁴                                             | ✔️                                               | ✔️                                        | ✔️                                                         | ✔️                                          |
| WinAppSDK                          | ✔️                                              | ⏳⁵                                         | ❌                                                   | ✔️                                              | ⏳⁵                                              | ⏳⁵                                       | ⏳⁵                                                        | ⏳⁵                                         |

**Notes:**

1. Desktop binaries do run on Windows, Linux, and macOS
2. You will need to be connected to a Mac to run and debug iOS apps from Windows
3. You will need to be connected to a Mac using [Remote - SSH](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-ssh)
4. [WebAssembly debugging](https://youtrack.jetbrains.com/issue/RIDER-103346/Uno-Platform-for-WebAssembly-debugger-support) is not yet supported
5. Agent support is coming soon

# [**macOS**](#tab/macos)

You can use **Visual Studio Code** or **JetBrains Rider**, to build Uno Platform applications on macOS. See the support matrix below for supported target platforms.

**Choose the IDE or Agent you want to use:**

- [VS Code with Copilot, Codespaces and Ona](xref:Uno.GetStarted.vscode)
- [Rider](xref:Uno.GetStarted.Rider)
- [Claude Code](xref:Uno.GetStarted.AI.Claude)
- [Codex CLI](xref:Uno.GetStarted.AI.Codex)
- [GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI)
- [Cursor](xref:Uno.GetStarted.AI.Cursor)

To help you choose the appropriate IDE or Agent, the following table shows the compatibility of different development environments with various target platforms:

|                                    | [**VS Code**](xref:Uno.GetStarted.vscode) | [**Codespaces**](xref:Uno.GetStarted.vscode) | [**Rider**](xref:Uno.GetStarted.Rider) | [**Claude Code**](xref:Uno.GetStarted.AI.Claude) | [**Codex**](xref:Uno.GetStarted.AI.Codex) | [**GitHub Copilot CLI**](xref:Uno.GetStarted.AI.CopilotCLI) | [**Cursor**](xref:Uno.GetStarted.AI.Cursor) |
|------------------------------------|------------------------------------------|-------------------------------------------------------|--------------------------------------------------|--------------------------------------------------|-------------------------------------------|-------------------------------------------------------------|---------------------------------------------|
| Desktop (Skia)¹                    | ✔️                                       | ✔️                                                   | ✔️                                               | ✔️                                               | ✔️                                        | ✔️                                                         | ✔️                                          |
| Android                            | ✔️²                                      | ❌                                                   | ✔️                                               | ⏳²                                              | ⏳²                                       | ⏳²                                                        | ⏳²                                         |
| iOS                                | ✔️                                       | ❌                                                   | ✔️                                               | ✔️                                               | ✔️                                        | ✔️                                                         | ✔️                                          |
| Web (WebAssembly)                  | ✔️                                       | ✔️                                                   | ✔️                                               | ✔️                                               | ✔️                                        | ✔️                                                         | ✔️                                          |
| WinAppSDK                          | ❌                                       | ❌                                                   | ❌                                               | ❌                                               | ❌                                        | ❌                                                         | ❌                                          |

**Notes:**

1. Desktop binaries do run on Windows, Linux, and macOS
2. Agent support is coming soon

The latest macOS release and Xcode version are required to develop with Uno Platform for iOS targets. If you have older Mac hardware that does not support the latest release of macOS, see the section for [Developing on older Mac hardware](xref:Uno.UI.CommonIssues.Ios#developing-on-older-mac-hardware).

# [**Linux**](#tab/linux)

 You can use either **JetBrains Rider** or **Visual Studio Code** to build Uno Platform applications on Linux. See the support matrix below for supported target platforms.

**Choose the IDE or Agent you want to use:**

- [Visual Studio Code with Copilot, Codespaces and Ona](xref:Uno.GetStarted.vscode)
- [Rider](xref:Uno.GetStarted.Rider)
- [Claude Code](xref:Uno.GetStarted.AI.Claude)
- [Codex CLI](xref:Uno.GetStarted.AI.Codex)
- [GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI)
- [Cursor](xref:Uno.GetStarted.AI.Cursor)

To help you choose the appropriate IDE or Agent, the following table shows the compatibility of different development environments with various target platforms:

|                                    | [**VS Code**](xref:Uno.GetStarted.vscode) | [**Codespaces**](xref:Uno.GetStarted.vscode) | [**Rider**](xref:Uno.GetStarted.Rider) | [**Claude Code**](xref:Uno.GetStarted.AI.Claude) | [**Codex**](xref:Uno.GetStarted.AI.Codex) | [**GitHub Copilot CLI**](xref:Uno.GetStarted.AI.CopilotCLI) | [**Cursor**](xref:Uno.GetStarted.AI.Cursor) |
|------------------------------------|------------------------------------------|-------------------------------------------------------|--------------------------------------------------|--------------------------------------------------|-------------------------------------------|-------------------------------------------------------------|---------------------------------------------|
| Desktop (Skia)¹                    | ✔️                                        | ✔️                                                     | ✔️                                            | ✔️                                               | ✔️                                        | ✔️                                                         | ✔️                                          |
| Web (WebAssembly)                  | ✔️                                        | ✔️                                                     | ✔️²                                           | ✔️                                               | ✔️                                        | ✔️                                                         | ✔️                                          |
| Android                            | ✔️³                                       | ❌                                                     | ✔️                                            | ⏳³                                              | ⏳³                                       | ⏳³                                                        | ⏳³                                         |
| iOS                                | ❌                                        | ❌                                                     | ❌                                            | ❌                                               | ❌                                        | ❌                                                         | ❌                                          |
| WinAppSDK                          | ❌                                        | ❌                                                     | ❌                                            | ❌                                               | ❌                                        | ❌                                                         | ❌                                          |

**Notes:**

1. Desktop binaries do run on Windows, Linux, and macOS
2. [WebAssembly debugging](https://youtrack.jetbrains.com/issue/RIDER-103346/Uno-Platform-for-WebAssembly-debugger-support) is not yet supported
3. Agent support is coming soon

---

## Supported Agents Features

Choosing the right agent for your development depends on your needs and environments. The following table summarizes the features supported by various agents for Uno Platform development:

| Agent                | Version      | Tools | Prompts | Hot Reload | mcp.json      | Platforms | Comments |
|----------------------|--------------|-------|---------|------------|----------------|-----------|----------|
| VS 2022 Copilot      | 17.14.16     | ✅    | ❌     | ⏳¹        | ✅        | iOS, Android, Desktop, Web ||
| VS 2026 Copilot      | 18.0.0 Pre 1 | ✅    | ❌     | ⏳¹        | ✅         | iOS, Android, Desktop, Web ||
| VS Code Copilot      | 1.105.1      | ✅    | ✅     | ✅         | ✅       | iOS, Android, Desktop, Web ||
| GitHub Copilot CLI   | 0.0.349      | ✅    | ❌     | ✅         | ❌         | Desktop, Web | |
| Cursor               | 2.0.34       | ✅    | ✅     | ✅         | ✅        | Desktop, Web ||
| Rider                | 2025.2       | ❌    | ❌     | ❌         | ❌         | ❌ |[Support coming soon](https://youtrack.jetbrains.com/issue/JUNIE-461/MCP-Remote-Server-Support)|
| Codex CLI            | 0.50.0       | ✅    | ❌     | ✅         | ❌      | Desktop, Web | [mcp.json](https://github.com/openai/codex/issues/2628)|
| Claude Code          | 2.0.25       | ✅    | ✅     | ✅         | ✅        | Desktop, Web | |

**Notes:**

1. VS Hot Reload support in agents is coming soon

## Questions

For questions about Uno Platform, refer to the [general FAQ](xref:Uno.Development.FAQ) or see the [troubleshooting section](xref:Uno.UI.CommonIssues) for common issues and their solutions.

## Next Steps

Choose the IDE or Agent to Learn more about:

- [Visual Studio](xref:Uno.GetStarted.vs2022)
- [VS Code, Codespaces and Ona](xref:Uno.GetStarted.vscode)
- [Rider](xref:Uno.GetStarted.Rider)
- [Claude Code](xref:Uno.GetStarted.AI.Claude)
- [Codex](xref:Uno.GetStarted.AI.Codex)
- [GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI)
- [Cursor](xref:Uno.GetStarted.AI.Cursor)
