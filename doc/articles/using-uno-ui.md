---
uid: Uno.Development.Overview
---

# Developing with Uno Platform

Write applications once in XAML and/or C# and deploy them to [any target platform](getting-started/requirements.md).

## About Uno

* [What is Uno Platform?](what-is-uno.md)
* [How does Uno Platform work?](how-uno-works.md)
* [Supported target platforms](getting-started/requirements.md)
* [Supported .NET versions by platform](net-version-support.md)

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

* [Migrating single-platform UWP code](howto-migrate-existing-code.md)
* [Creating a cross-targeted library](migrating-libraries.md)
* [Upgrading from older Uno.UI releases](migrating-from-previous-releases.md)

## Debugging Uno Platform applications

* [Troubleshooting build errors](uno-builds-troubleshooting.md)
* [Debugging C# on WASM](debugging-wasm.md)
* [XAML Hot Reload on non-Windows targets](features/working-with-xaml-hot-reload.md)
