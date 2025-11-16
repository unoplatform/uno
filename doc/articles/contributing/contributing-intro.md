---
uid: Uno.Contributing.Intro
---
<!-- TODO: Improve Page Structure, currently it is well worded, but a kind of Table layout with bullet list points like a Table of contents could ease the readability for starters to not get overwhelmed that easily-->
# Contributing to Uno

Everyone is welcome to contribute to the Uno Platform. Here you'll find useful information for new and returning contributors.

For starters, please read our [Code of Conduct](https://github.com/unoplatform/uno/blob/master/CODE_OF_CONDUCT.md), which sets out our commitment to an open, welcoming, harassment-free community.

If you're wondering where to start, [read about ways to contribute to Uno](xref:Uno.Contributing.WaysToContribute). Or, you can peruse the list of [first-timer-friendly open issues](https://github.com/unoplatform/Uno/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22).

## Understanding the Uno codebase

For a refresher on what Uno is and what it does and does not do, read [What is the Uno Platform?](xref:Uno.Documentation.Intro)

Get an [in-depth introduction to how Uno works](xref:Uno.Contributing.Overview), or jump straight to platform-specific details on how Uno works on [Android](xref:Uno.Contributing.Android), [iOS](xref:Uno.Contributing.iOS), [WebAssembly](xref:Uno.Contributing.Wasm), or [macOS](xref:Uno.Contributing.macOS).

## Building and debugging Uno

For the prerequisites you'll need, as well as useful tips like using [solution filters](https://learn.microsoft.com/visualstudio/ide/filtered-solutions) and cross-targeting overrides to quickly load and build Uno for a single platform, start with the guide to [Building Uno.UI](xref:Uno.Contributing.BuildingUno). The guide to [Debugging Uno.UI](xref:Uno.Contributing.DebuggingUno) will show you how to debug Uno.UI code either in the included UI samples or in an application outside the Uno.UI solution.

You can contribute to Uno directly from your browser using Ona, [find out how](xref:Uno.Features.Gitpod).

Whether you're fixing a bug or working on a new feature, [inspecting the visual tree of a running app](xref:Uno.Contributing.InspectVisualTree) is often a key step.

## Writing code in Uno

See [Uno's code conventions and common patterns here](xref:Uno.Contributing.CodeStyle).

## Implementing a new feature

See how to implement a new [feature here](xref:Uno.Contributing.ImplementWinUIWinRT-API).

## Experimenting with Samples App

The [Samples App](xref:Uno.Contributing.SamplesApp) is the development app contained in the Uno.UI solution. It serves as a UI and Runtime Tests host, as well as a playground for validating other API scenarios.

This app is available live at these locations, built from the default branch:

- [WebAssembly: `https://aka.platform.uno/wasm-samples-app`](https://aka.platform.uno/wasm-samples-app)

## Adding tests

Uno's stability rests upon a comprehensive testing suite. A code contribution usually isn't complete without a test.

See the [Guidelines for creating tests](xref:Uno.Contributing.Tests.CreatingTests) for an overview of the different types of tests used by Uno, and how to add one.

[Working with the Samples Applications](xref:Uno.Contributing.SamplesApp) provides instructions on adding a new UI sample to Uno, and authoring a UI Test to verify that the sample works.

## Creating a Pull Request

Uno uses [Git](https://git-scm.com/) for version control, and GitHub to host the [main repository](https://github.com/unoplatform/uno). You'll need to know the basics of Git to submit changes to Uno, but never fear, there are plenty of great guides available:
[Pro Git book | git-scm](https://git-scm.com/book/en/v2)
[About Git | GitHub](https://guides.github.com/introduction/git-handbook/)
[Tutorials | Atlassian](https://www.atlassian.com/git/tutorials)

> [!IMPORTANT]
> Before you commit your code, take a minute to familiarize yourself with the [Conventional Commits format](xref:Uno.Contributing.ConventionalCommits) Uno uses.

Read the [Guidelines for pull requests](xref:Uno.Contributing.PullRequests) in Uno.

Uno's CI process [runs a tool to guard against inadvertent binary breaking changes](xref:Uno.Contributing.BreakingChanges).

## Info for the core team

This section covers practices and utilities used by core maintainers.

Uno uses [Dependabot to automatically update external dependencies](xref:Uno.Contributing.UpdatingDependencies).

Read the [guidelines for issue triage](xref:Uno.Contributing.UpdatingDependencies).

- [Information about Tools and procedures for creating stable releases](xref:Uno.Contributing.ReleaseProcedure).

- [Information about Build artifacts produced by the CI](xref:Uno.Contributing.Artifacts).

### More questions?

To discuss an issue you're working on or would like to work on, join us in Uno's [Discord Server](https://platform.uno/discord).
