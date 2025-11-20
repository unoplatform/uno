---
uid: Uno.Development.Overview
---

# Developing with Uno Platform

Write applications once in XAML and/or C# and deploy them to [any target platform](getting-started/requirements.md).

## About Uno

* [What is Uno Platform?](xref:Uno.Documentation.Intro)
* [How does Uno Platform work?](xref:Uno.Development.HowItWorks)
* [Supported target platforms](xref:Uno.GettingStarted.Requirements)
* [Supported .NET versions by platform](xref:Uno.Development.NetVersionSupport)

## Productivity Tools

**[Uno Platform Studio](xref:Uno.Platform.Studio.Overview)** is a suite of productivity tools designed to accelerate your development workflow:

* **[Hot Design<sup>®</sup>](xref:Uno.HotDesign.Overview)** - The industry-first runtime visual designer for cross-platform .NET applications. Design and modify your UI directly in your running app from any IDE, on any OS.
* **[Hot Design<sup>®</sup> Agent](xref:Uno.HotDesign.Agent)** - AI-powered assistant for rapid UX/UI creation and enhancement, leveraging data contexts and live previews.
* **[Hot Reload](xref:Uno.Features.HotReload)** - See your XAML, C# Markup, and C# code changes instantly without recompiling or losing app state.
* **[Design-to-Code](xref:Uno.Figma.GetStarted)** - Generate production-ready XAML or C# Markup directly from Figma designs with one click.
* **[Uno MCPs](xref:Uno.Features.Uno.MCPs)** - Structured semantic access to Uno Platform's knowledge base and intelligent interaction with live applications.

[Sign in to get started with Uno Platform Studio](xref:Uno.GetStarted.Licensing)

## Mastering the basics

The WinUI application API which Uno Platform uses is extensively documented by Microsoft. We've [selected some articles from Microsoft](winui-doc-links.md) which will jump-start your Uno Platform development.

Here are some articles which cover the basics of cross-platform Uno development:

* [The structure of an Uno Platform codebase](uno-app-solution-structure.md)
* Platform-specific code: [C#](platform-specific-csharp.md) and [XAML](platform-specific-xaml.md)
* [Best practices for developing Uno Platform applications](best-practices-uno.md)

## WinUI features

These articles cover general WinUI features in Uno Platform, discussing what's supported and which platform-specific constraints exist.

* [Accessibility](features/working-with-accessibility.md)
* [Assets in shared code](features/working-with-assets.md)
* [Custom fonts](features/custom-fonts.md)
* [Shapes & Brushes](features/shapes-and-brushes.md)
* [Handling user input](features/pointers-keyboard-and-other-user-inputs.md)
* [Using Fluent styles in legacy apps](features/using-winui2.md)

## WinUI controls

Uno Platform provides support for a large number of WinUI controls, panels, and visual primitives - see [a complete list](implemented-views.md) in the Reference documentation. The 'WinUI controls' section in the Table of Contents on the left covers platform-specific constraints and extensions for selected controls.

## WinRT features (non-visual APIs)

Uno Platform supports a number of non-visual APIs from Windows Runtime namespaces on non-Windows platforms, like [clipboard management](features/windows-applicationmodel-datatransfer.md) and [sensor access](features/windows-devices-sensors.md).

[!include[Inline table of contents](includes/winrt-features-inline-toc.md)]

## Features unique to Uno

* [VisibleBoundsPadding - manage 'safe' area on notched devices](features/VisibleBoundsPadding.md)
* [ElevatedView - apply a shadow effect on all platforms](features/ElevatedView.md)
* [Uno.Material - Material Design on all platforms](external/uno.themes/doc/material-getting-started.md)
* [Uno.Cupertino](external/uno.themes/doc/cupertino-getting-started.md)

## Core functionality

* [Enable and configure logging](logging.md)
* [Configure build telemetry](uno-toolchain-telemetry.md)
* [Add native views to the visual tree](native-views.md)
* [Enable native control styles (Android and iOS)](native-styles.md)

## Common development tasks

* [Migrating single-platform WinUI/UWP code](howto-migrate-existing-code.md)
* [Creating a cross-targeted library](migrating-libraries.md)
* [Upgrading from an older Uno.UI releases](migrating-from-previous-releases.md)

## Debugging Uno Platform applications

* [Hot Reload](xref:Uno.Features.HotReload)
* [Troubleshooting build errors](uno-builds-troubleshooting.md)
* [Debugging C# on WASM](debugging-wasm.md)
