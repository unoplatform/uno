---
uid: Uno.Contributing.Intro
---

# Contributing to Uno

Everyone is welcome to contribute to the Uno Platform. Here you'll find useful information for new and returning contributors.

For starters, please read our [Code of Conduct](https://github.com/unoplatform/uno/blob/master/CODE_OF_CONDUCT.md), which sets out our commitment to an open, welcoming, harassment-free community.

If you're wondering where to start, [read about ways to contribute to Uno](ways-to-contribute.md). Or, you can peruse the list of [first-timer-friendly open issues](https://github.com/unoplatform/Uno/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22).

## Understanding the Uno codebase

For a refresher on what Uno is and what it does and does not do, read [What is the Uno Platform?](../intro.md)

Get an [in-depth introduction to how Uno works](uno-internals-overview.md), or jump straight to platform-specific details on how Uno works on [Android](uno-internals-android.md), [iOS](uno-internals-ios.md), [WebAssembly](uno-internals-wasm.md), or [macOS](uno-internals-macos.md).

### Understanding Skia vs. Native Rendering

Uno Platform provides two rendering modes that determine how your application's UI is drawn on the screen:

- **Skia Rendering** - Uses the [Skia](https://skia.org) drawing library to render all UI elements on a hardware-accelerated canvas. This provides a unified, pixel-perfect rendering experience across all platforms (iOS, Android, macOS, Windows, Linux, and WebAssembly). The Skia renderer is the default in the **Blank** and **Recommended** project templates and offers very efficient performance for large UIs.

- **Native Rendering** - Uses the native UI components and APIs of each platform (e.g., `UIView` on iOS, `ViewGroup` on Android, `div` on WebAssembly). This provides deeper integration with platform-specific features like accessibility and input methods, but may result in slight visual differences across platforms.

When contributing to Uno, you'll work with different solution filters depending on which rendering mode and platform you're targeting:

- `Uno.UI-Skia-only.slnf` - For all Skia-based implementations (including Skia variants on iOS, Android, macOS, and WebAssembly)
- `Uno.UI-Wasm-only.slnf` - For native WebAssembly rendering
- `Uno.UI-netcore-mobile-only.slnf` - For native mobile platforms (iOS and Android)
- `Uno.UI-Windows-only.slnf` - For Windows (uses WinAppSDK, not Uno rendering)

For more details, see [How Uno Platform Works](../how-uno-works.md), particularly the sections on [Skia Rendering](../how-uno-works.md#skia-rendering) and [Native Rendering](../how-uno-works.md#native-rendering).

## Building and debugging Uno

For the prerequisites you'll need, as well as useful tips like using [solution filters](https://learn.microsoft.com/visualstudio/ide/filtered-solutions) and cross-targeting overrides to quickly load and build Uno for a single platform, start with the guide to [Building Uno.UI](building-uno-ui.md). The guide to [Debugging Uno.UI](debugging-uno-ui.md) will show you how to debug Uno.UI code either in the included UI samples or in an application outside the Uno.UI solution.

You can contribute to Uno directly from your browser using Ona, [find out how](xref:Uno.Features.Gitpod).

Whether you're fixing a bug or working on a new feature, [inspecting the visual tree of a running app](xref:Uno.Contributing.InspectVisualTree) is often a key step.

## Writing code in Uno

See [Uno's code conventions and common patterns here](../contributing/guidelines/code-style.md).

## Implementing a new feature

See how to implement a new [feature here](xref:Uno.Contributing.ImplementWinUIWinRTAPI).

## Experimenting with Samples App

The [Samples App](xref:Uno.Contributing.SamplesApp) is the development app contained in the Uno.UI solution. It serves as a UI and Runtime Tests host, as well as a playground for validating other API scenarios.

This app is available live at these locations, built from the default branch:

- WebAssembly: https://aka.platform.uno/wasm-samples-app

## Adding tests

Uno's stability rests upon a comprehensive testing suite. A code contribution usually isn't complete without a test.

See the [Guidelines for creating tests](../contributing/guidelines/creating-tests.md) for an overview of the different types of tests used by Uno, and how to add one.

[Working with the Samples Applications](working-with-the-samples-apps.md) provides instructions on adding a new UI sample to Uno, and authoring a UI Test to verify that the sample works.

## Creating a Pull Request

Uno uses [Git](https://git-scm.com/) for version control, and GitHub to host the [main repository](https://github.com/unoplatform/uno). You'll need to know the basics of Git to submit changes to Uno, but never fear, there are plenty of great guides available:
[Pro Git book | git-scm](https://git-scm.com/book/en/v2)
[About Git | GitHub](https://guides.github.com/introduction/git-handbook/)
[Tutorials | Atlassian](https://www.atlassian.com/git/tutorials)

> [!IMPORTANT]
> Before you commit your code, take a minute to familiarize yourself with the [Conventional Commits format](git-conventional-commits.md) Uno uses.

Read the [Guidelines for pull requests](../contributing/guidelines/pull-requests.md) in Uno.

Uno's CI process [runs a tool to guard against inadvertent binary breaking changes](../contributing/guidelines/breaking-changes.md).

## Info for the core team

This section covers practices and utilities used by core maintainers.

Uno uses [Dependabot to automatically update external dependencies](../contributing/guidelines/updating-dependencies.md).

Read the [guidelines for issue triage](../contributing/guidelines/issue-triage.md).

Tools and procedures for creating stable releases are described [here](release-procedure.md).

Build artifacts produced by the CI are documented [here](../contributing/build-artifacts.md).

### More questions?

To discuss an issue you're working on or would like to work on, join us in Uno's [Discord Server](https://platform.uno/discord).
