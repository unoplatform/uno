---
uid: Uno.Development.MigratingApps
---

# Migrating a WinUI/UWP-only application codebase to an Uno Platform application

This article describes how to migrate a C# and XAML application targeting only WinUI/UWP to one targeting multiple platforms using Uno Platform. The final application codebase will share 99% of the same code as the original, but the solution structure will be different.

It assumes you're using Visual Studio for Windows, but the steps are similar if you're using VS Code or another IDE. The basic principle is to create an empty Uno Platform app with the same name as your WinUI/UWP-only app and to copy the contents of the old app into the main project of the new app.

After you've migrated the files and dependencies, the general [migration guidance article](migrating-guidance.md) will take you through the final steps to adjust your code to be Uno-compatible.

## Prerequisites

Follow the instructions to [set up your development environment for Uno Platform with Visual Studio for Windows](xref:Uno.GetStarted.vs2022). You can also use another IDE such as [VS Code](xref:Uno.GetStarted.vscode), however some steps may be slightly different.

## Migrating files to the Uno solution structure

Note: these steps will **destructively modify** your existing WinUI/UWP-only solution. Make sure you have a copy in source control or in a backup folder.

1. In order to reuse the name of the existing WinUI/UWP project, we want to first rename it and its containing folder. (If you don't want to reuse the name, you can skip this step.)

    i. First, rename the project itself within the solution by right-clicking on the project and choosing 'Rename'. For example, if the project is called `BugTracker`, we can rename it to `BugTracker_OLD`.

    ii. Next the folder containing the project must be renamed (assuming it still has the default name from when the project was originally created). Navigate to the folder containing the project. (Eg, by right-clicking on the project in Visual Studio and choosing 'Open Folder in File Explorer'.) Close your Visual Studio instance. Rename the folder, so that the project path becomes `./BugTracker_OLD/BugTracker_OLD.csproj`.

    iii. Finally, before reopening Visual Studio, open the `.sln` file with your favorite text editor. Locate the line referencing your project, and update it to the new path. It should look something like:

 ```sln
 Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "BugTracker_OLD", "BugTracker_OLD\BugTracker_OLD.csproj", "{92E4C60C-336D-4DFB-B08D-80489617C1F2}"
 ```

1. Open the solution. Right-click on the solution and choose 'Add > New Project'. Search for the 'Uno Platform App' template. Enter the desired name (eg `BugTracker` in our fictional example). Hit 'Create'. You should now have a project named `BugTracker`.

1. Delete `MainPage.xaml` from the new project - you will replace these with your existing files.

1. Replace `App.cs` contents with everything from your existing `App.xaml.cs` except the `InitializeComponent` call in the constructor.

1. Transfer all code files (C# and XAML) and assets from your old WinUI/UWP-only project to the new project.

*Note: you can safely delete 'head' projects for platforms that you're sure you don't want to target.*

## Adding dependencies

If your old WinUI/UWP app project had dependencies, you will have to add those dependencies to each new target platform project you wish to support. These include:

- .NET Standard projects within the solution: you should be able to add these as dependencies to any target platform project without difficulties.

- WinUI/UWP projects within the solution: these will have to be modified to be cross-platform class libraries - follow the instructions in [Migrating Libraries](migrating-libraries.md).

- external dependencies: [see information here](migrating-before-you-start.md) on finding equivalent dependencies for NuGet packages and framework libraries.

## Migrating app configuration

For WinUI/UWP, the `Package.appxmanifest` file can be copied directly from your old WinUI/UWP project to the new one in the `Platforms` folder. For other platforms, you will need to manually set the app name, capabilities, and other packaging information.

## Adjusting code for Uno compatibility

[See the next section](migrating-guidance.md) for adjustments to make to get your code compiling on Uno.

## What if my existing app targets WinUI 3?

The basic idea is the same, however, you'll use the [`dotnet new` templates](xref:Uno.GetStarted.dotnet-new) to create a WinUI 3-compatible Uno Platform app, into which you can transfer files and dependencies in a similar manner.
