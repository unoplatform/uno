---
uid: Uno.Development.NativeStyles
---

# Using platform-native control styles

By default, controls in Uno Platform applications are rendered exactly the same way on every target platform. But for Android and iOS, an additional set of 'native' [control styles](https://learn.microsoft.com/windows/uwp/design/controls-and-patterns/xaml-styles) are included, which customize the [templates of the controls](https://learn.microsoft.com/windows/uwp/design/controls-and-patterns/control-templates) to make them look and behave natively. In fact, the native high-level equivalents of each control are placed inside the native control template.

## About native control styles

For supported controls, two pre-defined styles are provided:

* NativeDefault[Control] which is customized to match the UI guidelines of the target platform.
* XamlDefault[Control] which are consistent in look and behavior across platforms.

If native control styles are desired, they can be enabled either globally for the whole application, or individually per control.

In general, control properties like `Button.Command` and `CheckBox.IsChecked` are supported when using native styles, but some properties that normally modify the visual appearance of the control may not be supported.

The full set of native control styles can be found in [Generic.Native.xaml](https://github.com/unoplatform/uno/blob/master/src/Uno.UI/UI/Xaml/Style/Generic/Generic.Native.xaml).

> [!NOTE]
> Third-party libraries can define native variants of default styles for custom controls, using the `not_win:IsNativeStyle="True"` tag in XAML. These will be used if the consuming application is configured to use native styles.

On WASM, the `NativeDefault[Control]` styles are currently only aliases to the `XamlDefault[Control]`, for code compatibility with other platforms.

## Enabling native control styles globally

An application can set native styles as the default for supported controls by setting the static flag `Uno.UI.FeatureConfiguration.Style.UseUWPDefaultStyles = false;` somewhere in app code (typically from the `App()` constructor in `App.cs` or `App.xaml.cs`). It's also possible to configure only certain control types to default to the native style, by adding an entry to `UseUWPDefaultStylesOverride` for that type. For example, to default to native styling for every `ToggleSwitch` in the app, you would add the following code: `Uno.UI.FeatureConfiguration.Style.UseUWPDefaultStylesOverride[typeof(ToggleSwitch)] = false;`

The default Uno app template includes Fluent control styles in the application. If enabling native control styles globally, these Fluent resources **should be removed from `App.xaml`**, by removing `<XamlControlsResources />` from the `Resources.MergedDictionaries` declaration.

In order for the native styles to be used globally by default, you cannot include resources in your `App.xaml` that also contain implicit styles for those same controls. For example, if you are using Uno.Material or Uno.Cupertino, those resources will override the native styles and, therefore, cannot be used if you are planning to enable native styles globally. You should remove the corresponding resource dictionaries from `App.xaml`.

### Native navigation styles

Since there are several controls that participate in navigation (`Frame`, `CommandBar`, `AppBarButton`), which should all be configured in the same way if native Frame navigation is used, there's a convenience method for configuring them all to use native styles:

```csharp
#if !NETFX_CORE
    Uno.UI.FeatureConfiguration.Style.ConfigureNativeFrameNavigation();
#endif
```

### Example

This sample shows how you'd enable native styles globally for your whole app. You need to modify both `App.cs` and `App.xaml`:

`App.xaml.cs`:

```diff
public class App : Application
{
    public static Window? _window;

+    public App()
+    {
+ #if HAS_UNO
+        FeatureConfiguration.Style.UseUWPDefaultStyles = false;
+ #endif
+    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // OnLaunched Logic...
    }
}
```

App.xaml:

```diff
 <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <ResourceDictionary.MergedDictionaries>
-        <!--  Load WinUI resources  -->
-        <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />

    </ResourceDictionary.MergedDictionaries>
    <!--  Add resources here  -->

 </ResourceDictionary>
```

## Enable native styling on a single control

To enable native styling on a per-control basis, you can set the native style explicitly on the control, referencing the `NativeDefault[Control]` style name.

For example, to use native styling on a single `CheckBox` in XAML:

```xml
<Page x:Class="MyApp.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:MyApp"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      xmlns:win="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:not_win="http://uno.ui/not_win"
      mc:Ignorable="d not_win"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid>
        <!--  This check box will use a platform-native style on Android and iOS  -->
        <CheckBox IsChecked="{Binding OptionSelected}"
                  not_win:Style="{StaticResource NativeDefaultCheckBox}" />
    </Grid>
</Page>
```

Note the use of the `not_win` prefix - this [conditional XAML prefix](platform-specific-xaml.md) ensures that the code compiles on Windows, where the `NativeDefaultCheckBox` resource doesn't exist.

You can use the native styles as you would any XAML style - you can extend them in your own `Style` definitions using `BasedOn={StaticResource NativeDefaultCheckBox}`, for instance.
