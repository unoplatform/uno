---
uid: Uno.Guides.AddAdditionalPlatforms
---

# Adding Platforms to an Existing Project

If you have an existing Uno Platform project, and you have not selected all the platforms you need when creating the project, this guide will show you how to add new ones.

Considering that your project is called `MyProject`, and you want to add the `desktop` target support:

1. In a separate temporary folder, create a new project using the **Visual Studio 2022** or `dotnet new` templates, using `MyProject` for its name.
1. Unselect all platforms except `Desktop` in the platforms selection dialog.
1. Make sure to select the template preset and features.
1. Once the project has been created, navigate to the new folder `MyProject`.
1. Copy `Platforms/Desktop` folder to the existing project structure, at the same level as the other platform folders.
1. In your `.csproj`, add the `net10.0-desktop` target framework to the `TargetFrameworks` property.
1. Save your solution.

Your new platform project is now ready to be compiled.

You can repeat a similar process for `net10.0-ios`, `net10.0-android`, `net10.0-browserwasm`, and `net10.0-windows10.0.xxxxx`.
