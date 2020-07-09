# Contributing to Uno

Everyone is welcome to contribute to the Uno Platform. Here you'll find useful information for new and returning contributors.

For starters, please read our [Code of Conduct](https://github.com/unoplatform/uno/blob/master/CODE_OF_CONDUCT.md), which sets out our commitment to an open, welcoming, harrassment-free community.

If you're wondering where to start, [read about ways to contribute to Uno](ways-to-contribute.md). Or, you can peruse the list of [first-timer-friendly open issues](https://github.com/unoplatform/Uno/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22).

## Understanding the Uno codebase

For a refresher on what Uno is and what it does and does not do, read [What is the Uno Platform?](../intro.md)

Get an [in-depth introduction to how Uno works](uno-internals-overview.md), or jump straight to platform-specific details on how Uno works on [Android](uno-internals-android.md), [iOS](uno-internals-ios.md), [WebAssembly](uno-internals-wasm.md), or [macOS](uno-internals-macos.md).

## Building and debugging Uno

For the prerequisites you'll need, as well as useful tips like using [solution filters](https://docs.microsoft.com/en-us/visualstudio/ide/filtered-solutions) and cross-targeting overrides to quickly load and build Uno for a single platform, start with the guide to [Building and Debugging Uno.UI](debugging-uno-ui.md).

If you're doing development for Uno's macOS support, you'll need to build and run Uno using Visual Studio for Mac. [There's a separate guide for that here](building-uno-macos.md).

You can contribute to Uno directly from your browser using GitPod. [Find out how.](../features/working-with-gitpod.md)

Whether you're fixing a bug or working on a new feature, [inspecting the visual tree of a running app](debugging-inspect-visual-tree.md) is often a key step. 

## Writing code in Uno

See [Uno's code conventions and common patterns here](../contributing/guidelines/code-style.md).

## Adding tests

Uno's stability rests upon a comprehensive testing suite. A code contribution usually isn't complete without a test.

See the [Guidelines for creating tests](../contributing/guidelines/creating-tests.md) for an overview of the different types of tests used by Uno, and how to add one.

[Working with the Samples Applications](working-with-the-samples-apps.md) provides instructions on adding a new UI sample to Uno, and authoring a UI Test to verify that the sample works.

## Creating a Pull Request

Uno uses [Git](https://git-scm.com/) for version control, and GitHub to host the [main repository](https://github.com/unoplatform/uno). You'll need to know the basics of Git to submit changes to Uno, but never fear, there are plenty of [great](https://git-scm.com/book/en/v2) [guides](https://guides.github.com/introduction/git-handbook/) [available](https://www.atlassian.com/git/tutorials).

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

To discuss an issue you're working on or would like to work on, join us in Uno's [Discord channel #uno-platform](https://discord.gg/eBHZSKG).
