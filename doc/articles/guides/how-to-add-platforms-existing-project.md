---
uid: Uno.Guides.AddAdditionalPlatforms
---

# Adding Platforms to an Existing Project

If you have an existing Uno Platform project, and you have not selected all the platforms you need when creating the project, this guide will show you how to add new ones.

Considering that your project is called `MyProject`, and you want to add the `Gtk` project head:

1. In a separate temporary folder, create a new project using the **Visual Studio 2022** or `dotnet new` templates, using `MyProject` for its name.
1. Unselect all platforms except `Gtk` in the platforms selection dialog
1. Once the project has been created, navigate to the new folder `MyProject.Skia.Gtk`

1. Copy this folder to the existing project structure, at the same level as the other platform folders

1. In Visual Studio, right-click on the **Platforms** solution folder, then select **Add**, **Existing project**
1. Save your solution

Your new platform project is now ready to be compiled.
