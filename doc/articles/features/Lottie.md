---
uid: Uno.Features.Lottie
---

# Lottie for Uno

> [!TIP]
> This article covers Uno-specific information for `Lottie`. For a full description of the feature and instructions on using it, see [Lottie](https://learn.microsoft.com/windows/communitytoolkit/animations/lottie).

* The `Microsoft.Toolkit.Uwp.UI.Lottie` namespace provides classes for rendering Lottie animations in a `Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer`.

## Using the `LottieVisualSource`

### [**WinUI 3**](#tab/winui)

Add the following namespaces:

```xml
<Page
    ...
    xmlns:winui="using:Microsoft.UI.Xaml.Controls"
    xmlns:lottie="using:Microsoft.Toolkit.Uwp.UI.Lottie"
    ...>
```

```xml
<winui:AnimatedVisualPlayer
    x:Name="player"
    AutoPlay="true">

    <lottie:LottieVisualSource
        UriSource="ms-appx:///Lottie/4930-checkbox-animation.json" />
</winui:AnimatedVisualPlayer>
```

On all Uno Platform targets, you'll need the following packages:

* `Uno.WinUI.Lottie` (for the `LottieVisualSource`)

Additionally, on Skia targets (Gtk, WPF, FrameBuffer), you'll need the following packages:

* `SkiaSharp.Views.Uno.WinUI` version 2.88.3 or later
* `SkiaSharp.Skottie` version 2.88.3 or later

On Windows/WinAppSDK, [the support for Lottie is still pending](https://github.com/CommunityToolkit/Lottie-Windows/issues/478).

### [**UWP**](#tab/uwp)

Add the following namespaces:

```xml
<Page
    ...
    xmlns:winui="using:Microsoft.UI.Xaml.Controls"
    xmlns:lottie="using:Microsoft.Toolkit.Uwp.UI.Lottie"
    ...>
```

```xml
<winui:AnimatedVisualPlayer
    x:Name="player"
    AutoPlay="true">

    <lottie:LottieVisualSource
        UriSource="ms-appx:///Lottie/4930-checkbox-animation.json" />
</winui:AnimatedVisualPlayer>
```

On all Uno Platform targets, you'll need the following packages:

* `Uno.UI.Lottie` (for the `LottieVisualSource`)

Additionally, on Skia targets (Gtk, WPF, FrameBuffer), you'll need the following packages:

* `SkiaSharp.Views.Uno` version 2.88.3 or later
* `SkiaSharp.Skottie` version 2.88.3 or later

On UWP, you'll need to reference the following packages in your head project:

* `Microsoft.Toolkit.Uwp.UI.Lottie` (for the `LottieVisualSource`)
* `Microsoft.UI.Xaml` (for the `AnimatedVisualPlayer`)

***

For more information, see [AnimatedVisualPlayer Class](https://learn.microsoft.com/uwp/api/microsoft.ui.xaml.controls.animatedvisualplayer).

## Lottie JSON file location

On WASM, iOS, and macOS, you can put the Lottie .json files directly in a folder of the Class Library project or your app's head project (for example "Lottie/myanimation.json") and set their Build action as Content.

On Android, Lottie .json files need to be added into the Assets folder. To match the same path as for the other platforms, the file could be stored at `Assets/Lottie/myanimation.json`. Set its Build action to AndroidAsset.

To reference the animations in XAML, use the `ms-appx:` URI, in this case `ms-appx:///Lottie/myanimation.json`.

## Using `embedded://` scheme

> [!WARNING]
> This feature is only available on Uno Platform targets. WinUI and UWP on Windows is not supported.

You can put the file as `<EmbeddedResource>` in your assembly and retrieve it using the following url format as `UriSource`:

```uri
embedded://<assemblyname>/<resource name>
```

* You can specify `.` in assembly name to use the Application's assembly.
* You can specify `(assembly)` in path: will be replaced by assembly name.
* AssemblyName is case insensitive, **but the resource name is**.

## Theme Properties

As Microsoft documented in the [release notes for Lottie-Windows v6.1.0](https://github.com/windows-toolkit/Lottie-Windows/releases/tag/v6.1.0), there is a new feature called _Theme property binding_. This allows to dynamically change a value (usually a color) at runtime.

To use this feature on Windows, you need to generate code with the [`LottieGen` tool](https://learn.microsoft.com/windows/communitytoolkit/animations/lottie-scenarios/getting_started_codegen). On Uno supported platforms, it's available at runtime using the `ThemableLottieVisualSource` instead of the original `LottieAnimatedVisualSource`.

Here's how to use this feature:

1. Add a _css-like_ declaration to your Lottie shape like this then put this in the name of the shape. That means the `nm` property in the json-generated file.

   ``` css
   { Color: var(MyColor); }
   ```

   In that case, it will create a property (variable) named `MyColor`. It is possible to reuse the same property name on many layers in the same lottie file.

2. Use it that way:

   ``` xml
   <winui:AnimatedVisualPlayer
       x:Name="player"
       AutoPlay="true">

       <lottie:ThemableLottieVisualSource
           x:Name="animation"
           UriSource="ms-appx:///Assets/Lottie/CheckBoxAnimation.json" />
   </winui:AnimatedVisualPlayer>
   ```

3. Change the property in your code:

   ```csharp
   animation.SetColorThemeProperty("MyColor", Color.FromArgb(1, 255, 0, 0));
   ```

**Notes**:

* Changing a property will force the player to reload its animation. Not only the animation will be restarted, but it can also be heavy on the CPU. Animating these properties should be avoided.

* To reach compatibility with code generated by _LottieGen_ on Windows, it is possible to derive from `ThemableLottieVisualSource` and create required properties like this:

  ```csharp
  [Bindable]
  public sealed class CheckBoxAnimation : ThemableLottieVisualSource
  {
    public Color MyColor
    {
      get => GetColorThemeProperty(nameof(MyColor));
      set => SetColorThemeProperty(nameof(MyColor), value);
    }
  }
  ```

## Limitations

On Android, the `Stretch` mode of `Fill` is not currently supported.
