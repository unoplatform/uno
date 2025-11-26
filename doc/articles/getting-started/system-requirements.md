---
uid: Uno.GetStarted.SystemRequirements
---

# System Requirements

This page provides comprehensive information about the system requirements for developing applications with the Uno Platform. Understanding these requirements will help ensure a smooth development experience across all target platforms.

> [!TIP]
> For a quick setup experience, use [uno.check](https://aka.platform.uno/uno-check) to automatically verify and configure your development environment.

## Development Operating Systems

### Windows

- **Minimum**: Windows 10 version 1809 (build 17763) or later
- **Recommended**: Windows 11 with latest updates
- **Notes**:
  - Required for developing WinAppSDK/WinUI targets
  - Can develop for all Uno Platform target platforms, including iOS via [Pair to Mac](https://learn.microsoft.com/xamarin/ios/get-started/installation/windows/connecting-to-mac/)
  - Best IDE support with Visual Studio 2022/2026

### macOS

#### For iOS/Mac Catalyst Development

- **Minimum**: macOS Sonoma (14.0) or later
- **Required**: Xcode 15.2 or later
- **Notes**:
  - macOS Sonoma is required for Xcode 15.2+
  - Requirements are tied to .NET iOS and Xcode versions
  - Apple Silicon or Intel Mac capable of running macOS Sonoma

#### For Skia Desktop (Mac)

- **Minimum**: macOS 10.15 (Catalina) or later
- **Notes**:
  - Can develop cross-platform applications without iOS-specific requirements
  - Works with VS Code, Rider, or AI-powered development tools

### Linux

- **Supported distributions**: Debian-based distributions
  - Ubuntu 20.04 LTS or later
  - Ubuntu 22.04 LTS (recommended)
- **Notes**:
  - Testing is primarily done on Debian-based distributions
  - Works with VS Code, Rider, or AI-powered development tools
  - Building for Windows-specific targets (Skia.Wpf) requires Windows
  - Supports X11 and Wayland display servers
  - [WSL (Windows Subsystem for Linux)](xref:Uno.GetStarted.vs2022#additional-setup-for-windows-subsystem-for-linux-wsl) is supported on Windows

## Development Environments (IDEs)

### Visual Studio 2022/2026 (Windows)

- **Minimum Version**: Visual Studio 2022 17.9 or later
- **Recommended**: Latest stable version
- **Required Workloads**:
  - **ASP.NET and web development** (for WebAssembly development)
  - **.NET Multi-platform App UI development** (for iOS/Android)
  - **.NET desktop development** (for Skia-based targets)
- **Optional Workloads**:
  - **Universal Windows Platform development** (if targeting UWP)
- **Individual Components** (if needed):
  - **.NET Debugging with WSL** (for Linux development with WSL)
  - **Xamarin** and **Xamarin Remoted Simulator** (only for legacy Xamarin projects)

> [!NOTE]
> Uno Platform 5.0 and later does not support Xamarin projects. Legacy projects need [migration to .NET](xref:Uno.Development.MigratingFromXamarinToNet6).

### Visual Studio Code

- **Platforms**: Windows, macOS, Linux
- **Minimum Version**: Latest stable version
- **Required Extensions**:
  - **Uno Platform extension** from the marketplace
  - **C# Dev Kit** (or legacy OmniSharp extension)
- **Required SDK**: .NET 9.0 SDK or later

### JetBrains Rider

- **Minimum Version**: Rider 2023.2 or later
- **Recommended**: Latest stable version
- **Platforms**: Windows, macOS, Linux
- **Required SDK**: .NET 9.0 SDK or later
- **Notes**: Excellent cross-platform development experience

### AI-Powered Development Tools

Uno Platform supports development with AI-powered coding assistants:

- **[GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI)** - Command-line AI assistant
- **[Cursor](xref:Uno.GetStarted.AI.Cursor)** - AI-first code editor
- **[Claude Code](xref:Uno.GetStarted.AI.Claude)** - Anthropic's AI coding assistant
- **[Codex](xref:Uno.GetStarted.AI.Codex)** - OpenAI's code generation model

These tools can significantly accelerate Uno Platform development with context-aware code suggestions and generation. See individual setup guides for configuration details.

## Software Dependencies

### .NET SDK

- **Current Stable**: .NET 9.0 SDK
- **In Development**: .NET 10.0 SDK support
- **Minimum**: .NET 9.0 SDK or later
- **Download**: [https://dot.net](https://dot.net)
- **Notes**:
  - Multiple SDK versions can be installed side-by-side
  - Use `dotnet --list-sdks` to view installed versions
  - Uno Platform supports the latest stable .NET releases
  - See [Upgrading NuGet Packages](xref:Uno.Development.UpgradeUnoNuget) when updating to new Uno Platform versions

### Platform-Specific SDKs

#### Android Development

- **Android SDK**: API Level 21 (Android 5.0 Lollipop) or later
- **Android Build Tools**: Latest version
- **Java Development Kit (JDK)**: JDK 11 or later
- **Notes**:
  - Typically supports the latest two Android SDK versions (e.g., Android 15 and 14)
  - Apps compiled with newer SDKs run on older Android versions
  - See [Android requirements](xref:Uno.GettingStarted.Requirements#android) for runtime details

#### iOS/Mac Catalyst Development

- **Xcode**: 15.2 or later (requires macOS Sonoma)
- **iOS SDK**: Included with Xcode
- **Apple Developer Account**: Required for:
  - Device deployment and testing
  - App Store submission
- **Notes**:
  - Development and simulator testing is free
  - Device testing and distribution requires paid Apple Developer account ($99/year)
  - See [iOS requirements](xref:Uno.GettingStarted.Requirements#ios) for runtime details

#### WebAssembly Development

- **Modern Web Browser**: Chrome, Edge, Firefox, or Safari with WebAssembly support
- **.NET WebAssembly Build Tools**: Included in .NET SDK
- **Notes**:
  - Both desktop and mobile browsers are supported
  - See [WebAssembly requirements](xref:Uno.GettingStarted.Requirements#webassembly) for browser details

#### Linux Development (Skia)

- **GTK3 Dependencies**: Required for Skia.Gtk targets
- **libSkiaSharp**: Graphics rendering library
- **Display Server**: X11 or Wayland
- **Notes**:
  - See the [Linux additional setup](xref:Uno.GetStarted.vscode#additional-setup-for-linux) guide for installation instructions
  - Supported on distributions where .NET is supported

#### Windows Development

- **Windows 10 SDK**: Version 19041 or later (for WinAppSDK/WinUI targets)
- **Notes**:
  - Automatically included with Visual Studio workloads
  - Skia.Wpf targets require Windows but have lower version requirements

## Hardware Requirements

### Minimum Requirements

- **Processor**: x64 or ARM64 processor, 1.8 GHz or faster
- **RAM**: 8 GB
- **Storage**: 20 GB of available space
  - IDE installation: ~5-10 GB
  - SDKs and tools: ~5-10 GB
  - Android emulators: ~5 GB per emulator
- **Display**: 1366 x 768 screen resolution

### Recommended Requirements

- **Processor**: Multi-core processor, 2.4 GHz or faster (4+ cores recommended)
- **RAM**: 16 GB or more
  - 32 GB recommended for iOS development with simulators
  - 32 GB recommended for running multiple platform emulators
- **Storage**: SSD with 50+ GB available space
  - Faster build times with SSD
  - More space needed for multiple platform SDKs and emulators
- **Display**: 1920 x 1080 or higher resolution
  - Dual monitors recommended for productivity

### macOS Hardware (for iOS Development)

- **Mac Type**: Intel Mac or Apple Silicon Mac
- **Minimum**: Any Mac capable of running macOS Sonoma (14.0)
- **Notes**:
  - Apple Silicon Macs (M1/M2/M3/M4) provide excellent performance
  - Intel Macs from 2017 or later typically support macOS Sonoma
  - Check [Apple's compatibility list](https://support.apple.com/en-us/HT213264) for macOS Sonoma

## Target Platform Runtime Requirements

Applications built with Uno Platform have the following minimum runtime requirements on target devices:

| Platform | Minimum Version | Minimum Target Framework |
|----------|----------------|------------------|
| **iOS** | iOS 11.0+ | `net9.0-ios` |
| **Android** | Android 5.0 (API 21)+ | `net9.0-android` |
| **WebAssembly** | Modern browsers with WebAssembly | `net9.0-browserwasm` |
| **Windows (WinAppSDK)** | Windows 10 version 1809+ | `net9.0-windows10.0.19041` |
| **Windows (Skia)** | Windows 7+ | `net9.0-desktop` |
| **macOS** | macOS 10.15 (Catalina)+ | `net9.0-desktop` |
| **Linux** | GTK3-capable distributions | `net9.0-desktop` |

For detailed platform-specific requirements, see [Supported Platforms](xref:Uno.GettingStarted.Requirements).

## Network Requirements

An internet connection is required for:

- **NuGet Package Downloads**: Uno Platform packages and dependencies
- **.NET SDK Downloads**: Initial SDK installation and updates
- **IDE Extensions**: Installing and updating IDE extensions
- **Documentation Access**: Accessing online documentation and samples
- **Pair to Mac**: iOS development from Windows (requires Mac on same network)
- **Remote Testing**: Testing on physical devices remotely
- **Cloud Services**: Using Uno Platform cloud features (if applicable)

> [!NOTE]
> After initial setup, most development can be done offline. However, package restore and updates require internet connectivity.

## Optional Tools and Services

### Version Control

- **Git**: Industry-standard version control
  - Integrated with Visual Studio, VS Code, and Rider
  - Required for cloning Uno Platform samples and templates

### Cloud Development

- **GitPod**: Browser-based development environment
  - Pre-configured Uno Platform workspaces available
  - No local setup required
  - See [Working with Gitpod](xref:Uno.Features.Gitpod) for details
- **GitHub Codespaces**: Cloud-based development environment
  - Works with VS Code in the browser or locally
  - See [Working with Codespaces](xref:Uno.Features.Codespaces) for details
- **DevContainers**: Docker-based development environments
  - Consistent setup across team members
  - Works with VS Code and compatible IDEs

### Mobile Device Testing

- **Android Emulators**:
  - Android Emulator (included with Android SDK)
  - Genymotion (third-party alternative)
  - Physical Android devices via USB debugging
- **iOS Simulators**:
  - iOS Simulator (included with Xcode, macOS only)
  - Physical iOS devices (requires Apple Developer account)

For troubleshooting emulator issues, see the [Android & iOS Emulator Guide](xref:Uno.UI.CommonIssues.MobileDebugging).

### Additional Development Tools

- **Docker**: For containerized development and testing
- **uno.check**: Environment validation and setup tool (highly recommended)
- **Hot Reload**: Built into Uno Platform for rapid development iteration
  - See [Hot Reload](xref:Uno.Features.HotReload) for usage details

## Frequently Asked Questions

### Do I need a Mac to develop iOS applications?

Yes, iOS development requires a Mac with Xcode installed. You can develop on Windows using Visual Studio and "Pair to Mac" to connect to a Mac for building and deploying iOS applications.

### Can I develop on Linux?

Yes, you can develop Uno Platform applications on Linux using VS Code or Rider. You can target WebAssembly, Skia Desktop (Linux), and Android. iOS development requires a Mac, and Windows targets require Windows.

### What if I only want to develop for WebAssembly?

You can use any supported OS (Windows, macOS, or Linux) with any supported IDE. The setup is much simpler as you only need the .NET SDK and a modern web browser.

### How do I verify my environment is set up correctly?

Run [uno.check](https://aka.platform.uno/uno-check) to automatically verify and fix common configuration issues. It checks all dependencies and provides guidance on what needs to be installed or updated.

### Can I use Visual Studio 2019?

No, Uno Platform 5.0 and later require Visual Studio 2022 (17.9+) or later. For older versions of Uno Platform, refer to the documentation for that specific version.

### Where can I get help if I encounter issues?

See [Getting Help](xref:Uno.Development.GettingHelp) for information on community support channels, or check the [Troubleshooting Guide](xref:Uno.Development.Troubleshooting) for common build issues.

## Related Resources

- [Get Started Guide](xref:Uno.GetStarted)
- [Supported Platforms](xref:Uno.GettingStarted.Requirements)
- [Visual Studio 2022 Setup](xref:Uno.GetStarted.vs2022)
- [VS Code Setup](xref:Uno.GetStarted.vscode)
- [Rider Setup](xref:Uno.GetStarted.Rider)
- [App Structure](xref:Uno.Development.AppStructure)
- [Hot Reload](xref:Uno.Features.HotReload)
- [Common Issues & Troubleshooting](xref:Uno.UI.CommonIssues)
- [.NET Version Support](xref:Uno.Development.NetVersionSupport)
- [Upgrading Uno Platform Packages](xref:Uno.Development.UpgradeUnoNuget)
- [FAQ](xref:Uno.Development.FAQ)
- [uno.check Tool](https://aka.platform.uno/uno-check)

## Community Discussions

This documentation was informed by community questions and discussions:

- [Development Environment Requirements Discussion](https://github.com/unoplatform/uno/discussions/16782)
- [macOS and Xcode Requirements](https://github.com/unoplatform/uno/discussions/15991)
- [Linux Development Requirements](https://github.com/unoplatform/uno/discussions/10027)

Have questions about system requirements? Join the [Uno Platform Discord community](https://aka.platform.uno/discord) for assistance.

---

> [!TIP]
> **Getting Started**: After verifying your system meets these requirements, follow the appropriate setup guide for your IDE:
>
> - [Visual Studio 2022/2026](xref:Uno.GetStarted.vs2022)
> - [VS Code](xref:Uno.GetStarted.vscode)
> - [Rider](xref:Uno.GetStarted.Rider)
