---
uid: Uno.Development.MigratingToSingleProject
---
# Migrating Projects to Single Project

The Uno Platform 5.2 and later use the [Uno.Sdk](https://www.nuget.org/packages/uno.sdk) which provides a single Uno Project structure and a single location for updating the Uno Platform core packages version in `global.json`.

> [!IMPORTANT]
> Migrating to the Uno.Sdk and Single Project are not required. Existing projects, 5.1 and earlier, continue to be supported in Uno Platform 5.2 or later.

With Uno Platform Single Project, you can optimize your project structure and simplify maintenance while leveraging the power of Uno Platform for cross-platform app development. This guide provides step-by-step instructions and practical tips to ensure a smooth migration process. Let's get started!

## What is the Uno.Sdk

The Uno.Sdk is used to abstract the complexity needed to build cross-platform projects, while still ensuring that you have the ability to customize the configuration to your needs.

This means that values such as the `SupportedOSPlatformVersion`, which have been in the template's `Directory.Build.props` in previous Uno Platform templates, now have a default value set to the minimum supported by Uno Platform.

If you need to be more restrictive for your specific project you can provide your overrides for the platform(s) that you need.

## Project Structure

In the Uno Platform Single Project, all platform-specific code is put together in one project. Instead of having separate projects for each platform, everything is organized within a `Platforms` folder. This folder contains sub-folders for each platform your project targets, like Android, iOS, Skia, WebAssembly, and Windows. Each platform folder stores the code and resources needed for that platform, making development easier to manage for multi-platform projects.

## Upgrading an existing project to Single Project

The following sections detail how to upgrade an existing project to use a single project with the Uno.Sdk. Those modifications are made in place and **it is strongly recommended to work on a source-controlled environment**.

While following this guide, you can compare with a new empty project created using [our Wizard](xref:Uno.GetStarted.Wizard), or using [dotnet new templates](xref:Uno.GetStarted.dotnet-new).

### Migrating to a Single Project

Migrating from a 5.1 or earlier template is a matter of merging all the csproj files for all the platforms heads down to one csproj file.

We'll take the example of an Uno Platform 5.0 blank app, called `MyApp`, to migrate to Single Project :

1. In a separate folder from your existing app, create a new app using the same name as your app:

   ```bash
   dotnet new unoapp -o MyApp --preset=recommended
   ```

   This will allow for taking pieces of the structure with the proper namespaces. If your app was created using the blank template, use `--preset=blank` instead. You can also use the [Live Wizard](xref:Uno.GetStarted.dotnet-new) or the [Visual Studio Wizard](xref:Uno.GettingStarted.CreateAnApp.VS2022) and select the same optional features you used for your app.
1. Next to the solution, create a `global.json` file

    ```json
    {
        "msbuild-sdks": {
            "Uno.Sdk": "{Current Version of Uno.WinUI/Uno.Sdk}"
        }
    }
    ```

1. From the empty app created at step one:
    - Take the contents of the `MyApp.csproj` and copy it over your own `MyApp.csproj`
    - Take the folder named `Properties` and copy it over your own `MyApp`
1. If you have your own `PackageReference` and `ProjectReference`, make sure to preserve them.
1. From the empty app created at step one, take the `Platforms` folder and copy it over your own `MyApp` folder.
1. In you app's `MyApp.csproj`, create the following block:

   ```xml
   <Choose>
     <When Condition="'$(TargetFramework)'=='net8.0-ios'">
     </When>
     <When Condition="'$(TargetFramework)'=='net8.0-android'">
     </When>
     <When Condition="'$(TargetFramework)'=='net8.0-windows10.0.19041'">
     </When>
     <When Condition="'$(TargetFramework)'=='net8.0-desktop'">
     </When>
     <When Condition="'$(TargetFramework)'=='net8.0-browserwasm'">
     </When>
   </Choose>
   ```

   This will allow us to place custom project configurations in each of those sections. Use the ones that are relevant to your project targets.
1. From the empty app created at step one, take the `App.xaml` and `App.xaml.cs` and copy those over your own `MyApp` folder.
1. Copy the individual modifications you made to your `AppResources.xaml` and `App.cs` into `App.xaml` and `App.xaml.cs`, respectively. Those can, for instance, be changes made to `OnLaunched`, `InitializeLogging`, or `RegisterRoutes`.
1. For the WebAssembly project, most of the configuration is now included by the `Uno.Sdk`. You can:
    1. Move all the files from your `MyApp.Wasm`  folder to the `Platforms/WebAssembly` folder, except `MyApp.Wasm.csproj` and the `Properties` folder.
    1. Copy the individual configurations, `ProjectReference` and `PackageReference` entries from your `MyApp.Wasm.csproj` to your `MyApp.csproj`, into the appropriate `When` condition block we created above. This can include properties like `WasmShellGenerateAOTProfile` or `WasmShellMonoRuntimeExecutionMode`, depending on your choices.
    1. Delete the `MyApp.Wasm` folder
1. For the Mobile project, you can:
    1. In your app, move the folders `MyApp.Mobile/Android`, `MyApp.Mobile/iOS` to the `MyApp/Platforms`.
    1. Copy the individual configurations, `ProjectReference` and `PackageReference` entries from your `MyApp.Mobile.csproj` to your `MyApp.csproj`, into the appropriate `When` condition block we created above. This can include properties like `ApplicationTitle` or `ApplicationId`, depending on your choices.
    1. Delete the `MyApp.Mobile` folder
1. For the Skia.Gtk, Skia.Wpf and Skia.Framebuffer projects, you can:
    1. Copy individual modifications made to the `Program.cs` of each platform over to `Platforms/Desktop/Program.cs`
    1. Copy the individual configurations, `ProjectReference` and `PackageReference` entries from your `MyApp.Skia.[Gtk|Wpf|Framebuffer].csproj` to your `MyApp.csproj`, into the appropriate `When` condition block we created above.
    1. You can also copy the individual modifications made to `app.manifest` and `Package.manifest` to `MyApp`. Those two files are now shared across platforms.
    1. Delete the `MyApp.Skia.[Gtk|Wpf|Framebuffer]` folders
1. For the `MyApp.Windows` project, you can:
    1. Copy the individual configurations, `ProjectReference` and `PackageReference` entries from your `MyApp.Windows.csproj` to your `MyApp.csproj`, into the appropriate `When` condition block we created above.
    1. You can copy the individual modifications made to `app.manifest` and `Package.manifest` to `MyApp`. Those two files are now shared across platforms.
1. For the `MyApp.[Shared|Base]` project, you can:
    1. Move the `Icons` and `Splash` folders to the `MyApp/Assets`
    1. If you've made modifications to the `base.props` file, you can copy those over to the `MyApp.csproj`, outside the `Choose` section.
    1. If you've made individual changes to the `AppHead.xaml` file, copy those over to the `App.xaml` file
    1. If you've made individual changes to the `AppHead.xaml.cs` file, copy those over to the `App.xaml.cs` file
    1. Delete the `MyApp.[Shared|Base]` folder
1. In All the `.cs` files, replace `AppHead` with `App`
1. Once done, you can remove the `Platforms` folder from your solution explorer, as all other platform heads have been deleted.
1. If you have a `MyApp.Server` project, make sure to change from:

    ```xml
    <ProjectReference Include="..\MyApp.Wasm\MyApp.Wasm.csproj" />
    ```

    to

    ```xml
    <ProjectReference 
        Include="..\MyApp\MyApp.csproj"
        SetTargetFramework="TargetFramework=net8.0-browserwasm"
        ReferenceOutputAssembly="false" />
    ```

1. From the empty app created at step one, take the `Directory.Build.props` and `Directory.Build.targets` files and copy those over your own root folder. Make sure to copy individual changes made in your file, such as `CSharpVersion` or `Nullable`.
1. From the empty app created at step one, take the `Directory.Packages.props`file and copy it over to your own root folder. All of the NuGet packages can be removed from this file, as the Uno.SDK provides those automatically.
1. In all the projects files, replace `$(DotnetVersion)` with `net8.0`
1. Remove the `solution-config.props.sample` file. This part is now handled by Uno Platform automatically.

At this point, your solution can be compiled using a single project and Uno Platform 5.2. For a guide through using the new single project, head over to [our getting started](xref:Uno.GetStarted).

## Wrapping up

Now that your project has gotten simplified, you can head over to our [Uno.Sdk](xref:Uno.Features.Uno.Sdk) documentation to explore its features.
