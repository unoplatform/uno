---
uid: Uno.Features.WinUI2
---

# Using Fluent styles in legacy apps

> This article is only relevant for migration of **legacy UWP-based Uno Platform applications**. Modern template has Fluent styles included by default.

## Enabling Fluent styles

1. In the `UWP` head project of your solution, install the [WinUI 2 NuGet package](https://www.nuget.org/packages/Microsoft.UI.Xaml).
1. There's no extra package to install for non-Windows head projects - the WinUI 2 controls are already included in the `Uno.UI` NuGet package which is installed with the default Uno Platform template.
1. Open the `App.xaml` file inside one of the Head project used by all platform heads. Add the `XamlControlsResources` resource dictionary to your application resources inside `App.xaml`.

    ```xml
        <Application>
            <Application.Resources>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            </Application.Resources>
        </Application>
    ```

    Or, if you have other existing application-scope resources, add `XamlControlsResources` at the top (before other resources) as a merged dictionary:

    ```xml
        <Application.Resources>
            <ResourceDictionary>
                <ResourceDictionary.MergedDictionaries>
                    <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
                    <!-- Other merged dictionaries here -->
                </ResourceDictionary.MergedDictionaries>
                <!-- Other app resources here -->
            </ResourceDictionary>
        </Application.Resources>
    ```

1. Now you're ready to use WinUI 2 controls in your application. Sample usage for the `NumberBox` control:

    ```xml
    <Page x:Class="UnoLovesWinUI.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:UnoLovesWinUI"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:winui="using:Microsoft.UI.Xaml.Controls"
        mc:Ignorable="d"
        Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

        <Grid>
            <winui:NumberBox />
        </Grid>
    </Page>
    ```
