---
uid: Uno.Development.BestPractices
---

# Best practices for developing Uno Platform applications

This article covers some basic best practices when developing cross-platform applications with Uno Platform.

## Questions to ask

1. Which [platforms](xref:Uno.GettingStarted.Requirements) do I plan to target?
2. What framework features do I plan to use? Are they supported on all of my planned target platforms?
3. Which major third-party dependencies will I use? Are they supported on all of my planned target platforms?

## Development workflow

Testing and debugging your application is easier and more rapid on some platforms and trickier and more time-consuming on others. Depending on where you're at in your development cycle, it may make sense to test all platforms, or it may make sense to focus on the 'easiest' platform.

1. **At the beginning of the development cycle,** you should identify key features from the Uno Platform framework and 3rd-party dependencies that you plan to use. Check that the framework controls you plan to use [are implemented](implemented-views.md). Consider creating a simple proof-of-concept (POC) app covering the 'riskiest' features and testing it on all platforms you're targeting.

2. **In the middle of the development cycle,** once the major pieces are in place, when you're iterating on the UI and business logic of your application, most of your day-to-day development should focus on the easiest platform to develop on. Most of the time, this will be Windows, where you can take advantage of Microsoft's excellent tooling (Live Visual Tree, XAML Hot Reload, etc) and where build times are often shortest. For this reason, it's recommended to keep the Windows (WinUI 3) head project in your solution, even if you don't plan to publish your application to Windows.

3. **At the end of the development cycle,** as your attention shifts to testing and fixing bugs, you'll again distribute your time more equally across all of the platforms you plan to target, ensuring that the application looks and behaves consistently everywhere.

## Platform-specific code

It's likely that some part of your application's code, be it C# code or XAML markup, will be specific to only one platform - perhaps because you want to access platform-specific APIs, implement a feature using native third-party libraries, or simply customize the experience to be more idiomatic to that particular platform.

You can read more on the mechanics of platform-specific code [here for C#](xref:Uno.Development.PlatformSpecificCSharp) and [here for XAML](xref:Uno.Development.PlatformSpecificXaml). You should also make sure you understand [an Uno Platform App solution structure](xref:Uno.Development.AppStructure).

The main goals where platform-specific code is concerned are to:

* **Maximize maintainability by keeping as much code shared as possible.**
* **Maximize readability by organizing your code in a consistent, legible way.**

Here are some tips to achieve that:

* **Use [partial class](platform-specific-csharp.md#partial-class-definitions) definitions to mix shared code and platform-specific code in a single class.** Separating all platform-specific code into a dedicated partial definition is usually more readable than interleaving platform-specific `#if` blocks with shared code, particularly if the amount of platform-specific code is significant.
* **Give partial definition files a platform-specific suffix.** Eg, for a `FormHighlighter` class, the shared partial definition would go in `FormHighlighter.cs`, the iOS-specific partial definition would go in `FormHighlighter.iOS.cs`, etc.
* **(Optional) Consider putting platform-agnostic application layers in a separate .NET Standard project.** 'Thinking multi-platform' adds to the cognitive burden of reading and writing code, as well as the testing effort of verifying that it runs the same way on every platform. For this reason, some people prefer to split out platform-agnostic parts of the application into a separate .NET Standard project, eg 'pure' business logic which doesn't interact with the UI or with non-visual platform APIs. This project builds once as a single binary used on all platforms, giving a stronger guarantee that it will behave consistently everywhere. Enforcing that platform-agnostic separation does impose an architectural burden too, so it's a matter of personal preference.

## Application architecture

You have a lot of choice when choosing an architecture for an Uno Platform application. Since it uses the WinUI contract, Uno Platform supports several features which lend themselves to a [model/view/view-model (MVVM) approach](https://learn.microsoft.com/windows/uwp/data-binding/data-binding-and-mvvm), like data binding and dependency properties; but you're perfectly free to use any approach you like best.

Sometimes so much freedom can be paralyzing. To help you get started, we've created several reference applications especially for Uno Platform. These are working, real-world applications utilizing simple but effective architectural patterns for cross-platform development.

* [**Ch9**](https://github.com/unoplatform/Uno.Ch9): browse content from Microsoft's publicly-available Channel 9 video feed.
* [**UADO**](https://github.com/unoplatform/uado): Universal Azure DevOps Organizer

## Performance

See a checklist of performance-related best practices in [Uno Platform Performance](Uno-UI-Performance.md).
