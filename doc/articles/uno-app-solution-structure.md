# Uno app solution structure

This guide briefly explains the structure of an app created with the default [Uno app template](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin). It's particularly aimed at developers who have not worked with multi-platform codebases before. 

## The project files in an Uno app

Let's say we've created a new solution with the [Uno app template](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin), call it `HelloWorld`. It will already contain the following projects:

1. A `HelloWorld.[Platform].csproj` file for each platform that Uno supports: UWP (Windows), Android, iOS, and WebAssembly (Web). This project is known as the **head** for that platform. It contains typical information like settings, metadata, dependencies, and also a list of files included in the project. The platform *head* builds and packages the binary executable for that platform. 

> The Android head is named `Droid` to avoid namespace clashes with the original Android namespace.

2. A single `HelloWorld.Shared.shproj` file, plus an accompanying `HelloWorld.Shared.projitems` file. This *shared project* contains files that are shared between all of the heads.

> The main reason the solution contains a shared project and not a cross-targeted library is related to a missing Visual Studio feature. At present time (VS16.1.x), building a head that references such a cross-targeted library implies that all target frameworks are built, leading to a slow developer inner loop.

Normally, your UI and business logic will go in the shared project. Bootstrapping code, packaging settings, and platform-specific code goes in the corresponding platform head. [String resources](using-uno-ui.md#localization) normally go in the shared project. [Image assets](features/working-with-assets.md) may go either in the shared project or under each head. [Font assets](using-uno-ui.md#custom-fonts) must be placed under each head.

![Uno solution structure](Assets/solution-structure.png)

## Understanding shared projects

Clearly understanding how shared projects work is important to using Uno effectively. A shared project in Visual Studio is really nothing more than a list of files. Let's repeat that for emphasis: **a shared project is just a list of files**. Referencing a shared project in an ordinary `.csproj` project causes those files to be included in the project. They're treated in exactly the same way as the files inside the project. 

It's important to be aware that the code in a shared-project file is compiled separately for each platform head. This gives a great deal of flexibility, but it also means that shared code may work for one platform, but not another.

For example, we decide we need to use the `Json.NET` library in our app. We install the NuGet package in our `HelloWorld.Droid` head, and add a class to our `HelloWorld.Shared` project:

```csharp
using Newtonsoft.Json;

...
```

We run our app on Android and it works fine. But now the UWP head fails to compile because we forgot to install the NuGet package there. The solution in this case is to install the NuGet package in all the heads we're targeting.

## Further information

See additional guides on handling platform-specific [C# code](platform-specific-csharp.md) and [XAML markup](platform-specific-xaml.md) in an Uno project.
