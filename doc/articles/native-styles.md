# Using platform-native control styles

For many controls in Uno, two prepackaged styles are provided:
* NativeDefault[Control] which is customized to match the UI guidelines of the target platform.
* XamlDefault[Control] which is the default style of controls on Windows.

An application can set native styles as the default for supported controls by setting the static flag `Uno.UI.FeatureConfiguration.Style.UseUWPDefaultStyles = false;` somewhere in app code (eg, the `App.xaml.cs` constructor). It's also possible to configure only certain controls to default to the native style, in the following manner: `Uno.UI.FeatureConfiguration.Style.UseUWPDefaultStylesOverride[typeof(Frame)] = false;`

Third-party libraries can define native variants of default styles for custom controls, using the `xamarin:IsNativeStyle="True"` tag in XAML. These will be used if the consuming application is configured to use native styles.

On WASM, the `NativeDefault[Control]` styles are currently only aliases to the `XamlDefault[Control]`, for code compatibility with other platforms. 

### Native navigation styles

Since there are several controls that participate in navigation (`Frame`, `CommandBar`, `AppBarButton`), which should all be configured in the same way if native Frame navigation is used, there's a convenience method for configuring them all to use native styles:

```csharp
#if !NETFX_CORE
    Uno.UI.FeatureConfiguration.Style.ConfigureNativeFrameNavigation();
#endif
```
