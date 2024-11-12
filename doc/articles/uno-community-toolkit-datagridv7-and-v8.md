---
uid: Uno.Development.CommunityToolkit.DataGridv7Andv8
---

# How to use Windows Community Toolkit - DataGrid Version 7.x Alongside Version 8.x for Other Components

This tutorial will walk you through on how to set up the `DataGrid` control with WCT version 7.x alongside using WCT version 8.x for other components.
As mentioned by the WCT Team in the **migration guide\***: "You can safely use the 7.x `DataGrid` alongside the newer 8.x packages, as it has no dependencies on the toolkit itself".

**\* See the [migration guide notes](xref:Uno.Development.CommunityToolkit) section for more details.**

> [!NOTE]
> The complete source code that goes along with this guide is available in the [unoplatform/Uno.Samples](https://github.com/unoplatform/Uno.Samples) GitHub repository - [`DataGridv7Andv8` Sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/WindowsCommunityToolkit/Version-7.x-and-8.x/UnoWCTDataGridv7Andv8Sample).

## Prerequisites

For a step-by-step guide to installing the prerequisites for your preferred IDE and environment, consult the [Get Started guide](xref:Uno.GetStarted).

## NuGet Packages

Uno Platform has ported the Windows Community Toolkit 7.x for use in Uno Platform applications to allow for use on Windows,
Android, iOS, mac Catalyst, Linux, and WebAssembly.

But starting with version 8.x, Uno Platform is now supported out of the box by the Windows Community Toolkit and Windows Community Toolkit Labs.

## Referencing the Windows Community Toolkit

When using the Uno Platform solution templates, add the following to your application:

1. Install the NuGet package(s) reference(s) that you need

    ### [Single Project Template [WinUI / WinAppSDK]]

    1. Edit your project file `PROJECT_NAME.csproj` and add the following conditional references:

        ```xml
        <!-- For DataGrid (with WCT v7) -->
        <ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">
            <PackageReference Include="CommunityToolkit.WinUI.UI.Controls.DataGrid" />
        </ItemGroup>
        <ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) != 'windows'">
            <PackageReference Include="Uno.CommunityToolkit.WinUI.UI.Controls.DataGrid" />
        </ItemGroup>

        <ItemGroup>
            <!-- For SettingsCard (with WCT v8)-->
            <PackageReference Include="CommunityToolkit.WinUI.Controls.SettingsControls" />
            <!-- Add more v8 Windows Community Toolkit references here -->
        </ItemGroup>
        ```

        If you already had a reference to the Community Toolkit, you should remove this line:

        ```xml
        <ItemGroup>
          <PackageReference Include="Uno.CommunityToolkit.WinUI.UI.Controls" />
        </ItemGroup>
        ```

    1. Edit `Directory.Packages.props` and add the following conditional references:

        ```xml
        <!-- For DataGrid (with WCT v7) -->
        <ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">
            <PackageReference Include="CommunityToolkit.WinUI.UI.Controls.DataGrid" />
        </ItemGroup>
        <ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) != 'windows'">
            <PackageReference Include="Uno.CommunityToolkit.WinUI.UI.Controls.DataGrid" />
        </ItemGroup>

        <ItemGroup>
            <!-- For SettingsCard (with WCT v8)-->
            <PackageReference Include="CommunityToolkit.WinUI.Controls.SettingsControls" />
            <!-- For GridSplitter (with WCT v8)-->
            <PackageReference Include="CommunityToolkit.WinUI.Controls.Sizers" />
            <!-- Add more v8 Windows Community Toolkit references here -->
        </ItemGroup>
        ```

    If you're getting an error like this one :

    ```console
    Controls\TextBox\Themes\Generic.xaml : Xaml Internal Error error WMC9999: 
    Type universe cannot resolve assembly: Uno.UI, Version=255.255.255.255, 
    Culture=neutral, PublicKeyToken=null.
    ```

    This means that there's an unconditional reference to Uno Platform's packages, and you'll need to make sure to add the conditional references as suggested above for WCT v7.x.

    > [!NOTE]
    > Windows Community Toolkit version 8.x requires an update to Windows SDK **10.0.22621** and above, along with [Microsoft.WindowsAppSDK](https://www.nuget.org/packages/Microsoft.WindowsAppSDK) updated to the latest matching version.
    >
    > To override these versions within a single project structure, you can set the properties in the `Directory.Build.props` file or directly in your project's `csproj` file. For more detailed information, please see the [implicit packages details](xref:Uno.Features.Uno.Sdk#implicit-packages).
    >
    > For example, in `PROJECT_NAME.csproj`:
    >
    > ```xml
    > <TargetFrameworks>
    >   <!-- Code for other TargetFrameworks omitted for brevity -->
    >   net8.0-windows10.0.22621;
    > </TargetFrameworks>
    > ```
    >
    > ```xml
    > <PropertyGroup>
    >   <WindowsSdkPackageVersion>10.0.22621.33</WindowsSdkPackageVersion>
    >   <WinAppSdkVersion>1.5.240607001</WinAppSdkVersion>
    > </PropertyGroup>
    > ```

1. Add the related needed namespaces

    ### [WinUI / WinAppSDK]

      In XAML:
        ```
        xmlns:v7controls="using:CommunityToolkit.WinUI.UI.Controls"
        xmlns:v8controls="using:CommunityToolkit.WinUI.Controls"
        ```

      In C#:  
        ```
        // For WCT v7.x:
        using CommunityToolkit.WinUI.UI.Controls;
        // For WCT v8.x:
        using CommunityToolkit.WinUI.Controls;
        ```

## Example with WCT v7.x DataGrid Control and WCT v8.x SettingsCard and GridSplitter Controls

_TO_DO_

### See the working sample with examples

![to-do](Assets/to-do.gif)

A complete working sample with data is available on GitHub: [Uno WCT v7.x DataGrid Control and WCT v8.x SettingsCard and GridSplitter Controls Sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/WindowsCommunityToolkit/Version-7.x-and-8.x/UnoWCTDataGridv7Andv8Sample)

---

[!include[getting-help](includes/getting-help.md)]
