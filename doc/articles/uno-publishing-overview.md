---
uid: uno.publishing.overview
---

# Publishing Your App (App Packaging)

Uno Platform provides **integrated, automated packaging** for every supported platform, covering desktop, mobile, web, and embedded, with no third-party tools or extra setup required.
Packaging is part of the standard `.NET publish` workflow, so you can go from code to distributable packages in a single command.

## Why It Matters

App packaging is the bridge between a working build and an installable application.
Each OS has unique signing, metadata, and distribution requirements, which can make deployment complex for cross-platform projects.

Typical tasks include:

- Building and publishing binaries
- Generating assets and manifests
- Applying code signing and notarization
- Assembling installable formats (MSIX, .app, APK, IPA, etc.)
- Preparing for distribution or app store submission

Traditionally, .NET developers rely on custom scripts or third-party tools to manage this complexity.
Uno Platform automates it out of the box.

## The Broader Platform Packaging Ecosystem

| Platform | Formats | Signing | Distribution | Complexity |
|-----------|----------|----------|---------------|-------------|
| **Windows** | MSIX, MSI, ClickOnce | Certificates | Microsoft Store, Direct | Multiple formats; Store validation |
| **macOS** | .app, .pkg, .dmg | Apple certs + notarization | App Store, Direct | Mandatory notarization; complex signing |
| **Linux** | Snap, AppImage, DEB, RPM | Optional | Snap Store, Direct | Multiple package managers |
| **Android** | APK, AAB | Keystore | Google Play, Direct | AAB for Play; Keystore management |
| **iOS** | IPA | Provisioning profiles | App Store, TestFlight | Strict signing; Profile management |
| **WebAssembly** | Static files, PWA | HTTPS / CSP | Web hosting | Service workers; PWA manifest |

## What Uno Platform Provides Out of the Box

Uno Platform simplifies all of this with a unified, automated approach:

| Platform | Package Formats | Code Signing | Store Ready | Status |
|-----------|----------------|---------------|-------------|---------|
| **Windows** | MSIX, ClickOnce | ✅ | ✅ Microsoft Store | ✅ Available |
| **macOS** | .app, .pkg, .dmg | ✅ | ✅ App Store | ✅ Available |
| **Linux** | Snap | ✅ | ✅ Snap Store | ✅ Available |
| **Android** | APK, AAB | ✅ | ✅ Google Play | ✅ Available |
| **iOS** | IPA | ✅ | ✅ App Store | ✅ Available |
| **WebAssembly** | Static files, PWA | ✅ | ✅ Web hosting | ✅ Available |

## Key Features

- **Native `dotnet publish` Integration** – No separate tools required. Works with your CI/CD pipelines.
- **Cross-Platform Build Support** – Build Windows packages from Linux or macOS, and vice versa.
- **Advanced Publishing Options** – Self-contained deployments, single-file packaging, Native AOT (where supported).
- **Automated Everything** – Manifest generation, asset resizing, signing, and platform-specific optimization.

Example command for Android AAB:

```bash
dotnet publish -f net9.0-android -p:AndroidPackageFormat=aab
```

The same command pattern applies to Windows, macOS, Linux, iOS, and WebAssembly.

## Preparing

Before publishing, make sure your app is optimized:

- [Configure the IL Linker](xref:uno.articles.features.illinker)
- [Enable XAML and Resource Trimming](xref:Uno.Features.ResourcesTrimming)
- [Improve Performance](xref:Uno.Development.Performance)

## Platform-Specific Guides

- [Packaging for Desktop](xref:uno.publishing.desktop) (`netX.0-desktop`)
- [Packaging for WebAssembly](xref:uno.publishing.webassembly) (`netX.0-browserwasm`)
- [Packaging for iOS](xref:uno.publishing.ios) (`netX.0-ios`)
- [Packaging for Android](xref:uno.publishing.android) (`netX.0-android`)
- [Packaging for Windows App SDK](xref:uno.publishing.windows) (`netX.0-windows10.yyy`)

## Continuous Integration

Uno Platform provides built-in [CI integrations](xref:Uno.GettingStarted.UsingWizard#11-ci-pipeline) for Azure DevOps and GitHub Actions, included as part of the Uno Platform project templates.
These pipelines include ready-to-use packaging steps for all supported platforms.
