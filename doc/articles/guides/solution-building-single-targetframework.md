---
uid: Build.Solution.TargetFramework-override
---
# Improve Build Times with Visual Studio 2022

The Uno Platform template **prior to Uno Platform 5.2** provides a cross-targeted Class library that includes multiple target frameworks, and your application may contain your own cross-targeted projects as well. This document explains how to make your builds faster.

> [!NOTE]
> For projects created with Uno Platform 5.2 or later, the Uno.Sdk takes care of building appropriate target frameworks for the selected debugger target. Optionally, you can follow [our migration guide](xref:Uno.Development.MigratingToSingleProject) to use the new project structure.

## Cross-targeted library builds in Visual Studio

While building with the command line `dotnet build -f net7.0-ios` only builds the application's head and the class library for `net7.0-ios`, as of Visual Studio 17.8, class library builds are considering all target frameworks, [regardless of the project head's target framework](https://developercommunity.visualstudio.com/t/Building-a-cross-targeted-project-with-m/651372) being built. _(Please help the Uno Platform community by upvoting the issue!)_

Considering that during development, it is common to work on a single platform at a given time, the two sections below contain a suggested set of changes to the Uno Platform solution that can be performed on the solution to restrict the active build platform, and therefore significantly speed up build times and make intellisense respond faster.

Choose the section that covers your cases, whether you're using a solution built using Uno Platform 5.0 templates, or if it was created with an earlier version.

## Improve performance using the Uno Platform templates

When using an Uno Platform 5.0 template, in the **Solution Explorer**, you'll find a folder named **Solution Items**, and a file named `solution-config.props.sample`.

You can follow the directions specified in this file, or follow them here:

- Right-click on the root item (your solution name) and **Open in file explorer**
- Locate the `solution-config.props.sample` and make a copy named `solution-config.props` in the same folder
- Back in Visual Studio, and for your convenience you can right-click on `Solution Items` and add `solution-config.props`

Now that we have configured this file, let's say that you want to debug for WebAssembly:

- In the `solution-config.props` file, uncomment the line that mentions `Wasm` or `WebAssembly` then save it.
- For the change to take effect, close and reopen the solution, or restart Visual Studio.

Repeat this process when changing your active development platform.

> [!NOTE]
> The `solution-config.props` is automatically included in a `.gitignore` file to avoid having your CI environment build only for one target.

## Improve your own solution

If you created your solution with an earlier version of Uno Platform, you can make some modifications to make your build faster:

1. Let's create a set of solution filters to ensure that individual project heads can be loaded:

    1. Create a new app template with **iOS**, **Android**, **WebAssembly** and **Windows** targets   selected.
    1. Right click on the **.Mobile** and **.Wasm** projects and select **Unload Project**
    1. On the top level Solution node, right-click to select **Save As Solution Filter**, name the    filter **MyApp-Windows-Only.slnf**
    1. Right-click on the **Mobile** project, select **Reload Project**
    1. Unload the **.Windows** project, then save a new solution filter called **MyApp-Mobile-Only.slnf**
    1. Repeat the operation with the **.Wasm** project, with a solution filter called **MyApp-Wasm-Only.slnf**

    These solution filters will prevent Visual Studio to restore NuGet packages for TargetFrameworks that will be ignored by the configuration done below.

1. Now, next to the solution file, create a file named `targetframework-override.props`:

    ```xml
    <Project>
        <Import Project="solution-config.props" Condition="exists('solution-config.props')" />

        <!-- Override the TargetFrameworks list with the one specified in MyAppTargetFrameworkOverride -->
        <PropertyGroup Condition="'$(MyAppTargetFrameworkOverride)'!=''">
            <TargetFrameworks>$(MyAppTargetFrameworkOverride)</TargetFrameworks>
        </PropertyGroup>
    </Project>
   ```

1. Also next to the solution file, create a file named `solution-config.props.sample`:

    ```xml
    <Project>
        <PropertyGroup>
            <!--
            Uncomment the following line to enable single-target framework builds
            in order to get faster performance when debugging for a single platform.

            Once this file is modified, use the appropriate solution filter to avoid
            NuGet restore issues.

            Available target frameworks can be found in the project heads of your solution.
            -->
            <!-- <MyAppTargetFrameworkOverride>net7.0-ios</MyAppTargetFrameworkOverride> -->
        </PropertyGroup>
    </Project>
    ```

1. Next, in all projects of the solution which are cross-targeted (with multiple TargetFrameworks values), add the following lines right after the `PropertyGroup` which defines `<TargetFrameworks>`:

    ```xml
    <!-- Import the TargetFramework override configuration -->
    <Import Project="../../targetframework-override.props" />
    ```

    The file should then look like this:

    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
            <TargetFrameworks>net7.0-windows10.0.19041;net7.0;net7.0-ios;net7.0-android</TargetFrameworks>
        </PropertyGroup>
    </Project>
    <!-- Import the TargetFramework override configuration -->
    <Import Project="../../targetframework-override.props" />
    ```

    > [!NOTE]
    > If the template is created with `dotnet new`, the path will instead be `../targetframework-override.props`

1. Create a copy of the file `solution-config.props.sample` next to itself, and name it `solution-config.props`
1. If using git, add this specific file to the `.gitignore` so it never gets committed. This way, each developer can keep their own version of the file without corrupting the repository.
1. Commit your changes to the repository.

At this point, your solution is ready for single-TargetFramework use.

For example, to work on `net7.0-ios`:

1. Before opening the solution, open the `solution-config.props` file and uncomment `MyAppTargetFrameworkOverride` to contain `net7.0-ios`
1. Open the `MyApp-Mobile-Only.slnf` solution filter in Visual Studio 2022
1. You should only see the **.Mobile** and **Class Library** projects in your solution
1. When building and debugging the app, you'll only now build for the target specified in `solution-config.props`.

> [!IMPORTANT]
> When changing the `MyAppTargetFrameworkOverride` value, make sure to close the solution and reload it so the build system recognizes properly the change.
