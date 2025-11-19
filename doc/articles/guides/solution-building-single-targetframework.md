---
uid: Build.Solution.TargetFramework-override
---
# Improve Build Times with Visual Studio 2022/2026

The Uno Platform template **prior to Uno Platform 5.2** provides a cross-targeted Class library that includes multiple target frameworks, and your application may contain your own cross-targeted projects as well. This document explains how to make your builds faster.

> [!NOTE]
> For projects created with Uno Platform 5.2 or later, the Uno.Sdk takes care of building appropriate target frameworks for the selected debugger target when starting a debugging session. Optionally, you can follow [our migration guide](xref:Uno.Development.MigratingToSingleProject) to use the new project structure. If you want to always build one target framework you can follow the guide below.

## Reduce the number of built TargetFrameworks (5.2 templates and later)

When using an Uno Platform 5.2 template or later, in the **Folder Explorer**, you can apply the following steps to build only a subset of target frameworks using a configuration file.

To do so:

- Create a file named `solution-config.props.sample` next to your `.sln` file, with this content:

    ```xml
    <Project>
        <!--
            This file is used to control the platforms compiled by Visual Studio, and allow for a faster
            build when testing for a single platform. This will also result in better intellisense, as
            the compiler will only load the assemblies for the platform that is being built. You do not
            need to use this when compiling from Visual Studio Code, the command line or other IDEs.

            Instructions:
            1) Copy this file and remove the ".sample" name
            2) Uncomment the single property below for the target you want to build
            3) Make sure to do a Rebuild, so that nuget restores the proper packages for the new target

            Notes:
            - You may optionally close the solution before making changes and reload the solution afterwards. This will avoid Visual Studio
            asking you to reload any projects, it will also ensure that the changes are picked up by Visual Studio, and trigger a restore of
            the packages.
            - You may want to unload the platform heads that you are not going to build for. This will ensure that Visual Studio does not
            try to build them, and will speed up the build process.
        -->

        <PropertyGroup>
            <!-- Uncomment each line for each platform that you want to build: -->

            <!-- <OverrideTargetFramework Condition="''!='hint: Windows App Sdk (WinUI)'">net9.0-windows10.0.19041.0</OverrideTargetFramework> -->
            <!-- <OverrideTargetFramework Condition="''!='hint: Webassembly'">net9.0-browserwasm</OverrideTargetFramework> -->
            <!-- <OverrideTargetFramework Condition="''!='hint: Desktop'">net9.0-desktop</OverrideTargetFramework> -->
            <!-- <OverrideTargetFramework Condition="''!='hint: iOS'">net9.0-ios</OverrideTargetFramework> -->
            <!-- <OverrideTargetFramework Condition="''!='hint: Android'">net9.0-android</OverrideTargetFramework> -->
            <!-- <OverrideTargetFramework Condition="''!='hint: TvOS'">net9.0-tvos</OverrideTargetFramework> -->
        </PropertyGroup>
    </Project>
    ```

    Make sure to replace `net9.0` and `-windows10.0.19041.0` with the appropriate version from your `.csproj` project.

- You can commit `solution-config.props.sample` to your source control.
- Next, make a copy of `solution-config.props.sample` to `solution-config.props`. This file is [automatically loaded](https://github.com/unoplatform/uno/blob/71f1d5ab067c0dcfad2f4cccd310e506cdeaf6bf/src/Uno.Sdk/targets/Uno.Import.SolutionConfig.props#L9) by the `Uno.Sdk`.
- If you're using git, add this `solution-config.props` to your `.gitignore`.

  > [!NOTE]
  > This avoids altering the target frameworks or your own CI, as well as other clones made by other developers.

- Follow the directives from the file, uncommenting one of the `OverrideTargetFramework` to your choosing.
- In the project head, as well as any other libraries using the `Sdk="Uno.Sdk"`, add the following block immediately below the last `<TargetFrameworks>` line:

    ```xml
    <TargetFrameworks Condition=" '$(OverrideTargetFramework)' != '' ">$(OverrideTargetFramework)</TargetFrameworks>
    ```

- Once done, if you're in Visual Studio 2022/2026, you may need to close and re-open your solution or otherwise click the reload button. For other IDEs, the projects will reload automatically.

At this point, you'll notice that the list of target frameworks available in the debugger will have been reduced to the list you added in `OverrideTargetFramework`.

## Improve performance using the Uno Platform templates (5.1 and earlier)

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

## Cross-targeted library builds in Visual Studio

While building with the command line `dotnet build -f net7.0-ios` only builds the application's head and the class library for `net7.0-ios`, as of Visual Studio 17.8, class library builds are considering all target frameworks, [regardless of the project head's target framework](https://developercommunity.visualstudio.com/t/Building-a-cross-targeted-project-with-m/651372) being built. _(Please help the Uno Platform community by upvoting the issue!)_

Considering that during development, it is common to work on a single platform at a given time, the two sections below contain a suggested set of changes to the Uno Platform solution that can be performed on the solution to restrict the active build platform, and therefore significantly speed up build times and make intellisense respond faster.

Choose the section covering your cases, whether you're using a solution built using Uno Platform 5.0 templates or created with an earlier version.

## Improve your own solution (4.9 and earlier)

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
1. Open the `MyApp-Mobile-Only.slnf` solution filter in Visual Studio 2022/2026
1. You should only see the **.Mobile** and **Class Library** projects in your solution
1. When building and debugging the app, you'll only now build for the target specified in `solution-config.props`.

> [!IMPORTANT]
> When changing the `MyAppTargetFrameworkOverride` value, make sure to close the solution and reload it so the build system recognizes properly the change.
