# Adding WinUI 2 to an Uno Platform application

The [WinUI 2 library](https://docs.microsoft.com/en-us/windows/apps/winui/winui2/) provides additional controls above those that are available in the UWP framework. This article explains how to use WinUI 2 controls in an Uno Platform application.

> [!TIP]
> If you're looking for information on [WinUI 3](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/), which is a total replacement for UWP XAML, read our [WinUI 3 explainer](../uwp-vs-winui3.md) or our [WinUI 3 upgrade guide](../updating-to-winui3.md).

## Enabling WinUI 2 controls

1. In the `UWP` head project of your solution, install the [WinUI 2 NuGet package](https://www.nuget.org/packages/Microsoft.UI.Xaml).
1. There's no extra package to install for non-Windows head projects - the WinUI 2 controls are already included in the `Uno.UI` NuGet package which is installed with the default Uno Platform template.
1. Open the `App.xaml` file inside the shared project used by all platform heads. Add the `XamlControlsResources` resource dictionary to your application resources inside `App.xaml`.
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
        mc:Ignorable="d">

        <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
            <winui:NumberBox />
        </Grid>
    </Page>
    ```