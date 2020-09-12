# Fluent-styled controls

Uno 3.0 and above supports control styles conforming to the [Fluent design system](https://www.microsoft.com/design/fluent). This article explains how to use them in your app.

## Upgrading existing Uno apps to use Fluent styles

To modify an existing Uno app to use the Fluent control styles, follow these steps:

1. Update `Uno.UI` NuGet packages to 3.0 or above.
1. In the `UWP` head project of your solution, install the [WinUI 2 NuGet package](https://www.nuget.org/packages/Microsoft.UI.Xaml).
1. Add the `XamlControlsResources` resource dictionary to your application resources in `App.xaml`:
    ```xaml
        <Application>
            <Application.Resources>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            </Application.Resources>
        </Application>
    ```
    or if you have other existing resources, then add `XamlControlsResources` at the top as a merged dictionary:
    ```xaml
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
1. The Fluent control styles require the Uno Fluent Assets icon font to display correctly. [Follow the instructions here](../uno-fluent-assets.md) to upgrade your app to use the font.