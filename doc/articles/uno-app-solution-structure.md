---
uid: Uno.Development.AppStructure
---

# Solution Structure

This guide briefly explains the structure of an app created with either the [`dotnet new unoapp` template](xref:Uno.GetStarted.dotnet-new) or the [Uno Platform Template Wizard](xref:Uno.GettingStarted.UsingWizard). It's particularly aimed at developers who have not worked with cross-platform codebases before.

## The project files in an Uno Platform app

After creating a new solution called `MyApp`, it will contain the following projects:

![Uno Platform solution structure](Assets/solution-structure.png)

1. A `MyApp.[Platform].csproj` file for each platform that Uno Platform supports: Mobile (iOS/Android/Mac Catalyst), Skia.Gtk, Wasm and Windows. These projects are known as **heads** for their respective platform.

    Head projects typically contain information like settings, metadata, dependencies, and also a list of files included in the project. Each of the head projects has a reference to the main class library of the application.

    The head projects are the projects that generate the executable binaries for the platform. They are also the projects that are used to debug the application on the platform. Right-click on the project in the **Solution Explorer** tool window and select `Set as Startup Project` to debug the application on the platform.

2. The `MyApp.csproj` file is the **Application Class Library** for the application and contains most of the code for the application.
3. The `MyApp.Shared.csproj` is a placeholder project used to edit the `AppHead.xaml` and `base.props` files. These files are automatically included in all other heads. This project is present to support the `.Windows` head and WinAppSDK. This project is not intended to be built and produces no output.
    The **Application Class Library** will contain most of the classes, XAML files, [String resources](features/working-with-strings.md) and assets ([images](features/working-with-assets.md), [fonts](features/custom-fonts.md) etc) for the application.

> [!NOTE]
> In an Uno Platform solution, the commonly known `App.xaml` and `App.xaml.cs` files are named `AppResources.xaml` and `App.cs`, respectively. Both are automatically included as part of each head's `AppHead.xaml` and `AppHead.xaml.cs` in order to create a cross-platform experience. It is recommended to use `AppResources.xaml` and `App.cs` for editing the application's startup.

## Handling dependencies

Dependencies (ie NuGet Package References) should be added to the  **Application Class Library**. This ensures that the dependencies are available to all the heads of the application. Platform-specific dependencies can be conditionally included in the **Application Class Library** by setting an appropriate `Condition` on the `PackageReference` element in the project file.

## Further information

See additional guides on handling platform-specific [C# code](platform-specific-csharp.md) and [XAML markup](platform-specific-xaml.md) in an Uno Platform project.

The Uno Platform solution also [can be further optimized](xref:Build.Solution.TargetFramework-override) to build larger projects with Visual Studio 2022.

## Next Steps

Learn more about:

- [Uno Platform features and architecture](xref:Uno.GetStarted.Explore)
- [Hot Reload feature](xref:Uno.Features.HotReload)
- [Troubleshooting](xref:Uno.UI.CommonIssues)
<<<<<<< HEAD
- <a href="implemented-views.md">Use the API Reference to Browse the set of available controls and their properties.</a>
- You can head to [our tutorials](xref:Uno.GettingStarted.Tutorial1) on how to work on your Uno Platform app.
=======
- [List of views implemented in Uno](implemented-views.md) for the set of available controls and their properties.
- You can head to [How-tos and tutorials](xref:Uno.Tutorials.Intro) on how to work on your Uno Platform app.
>>>>>>> 3a1efa8378 (docs: Update features docs (#15122))
