---
uid: Uno.Features.Lottie
---

# Lottie for Uno Platform

> [!TIP]
> This article covers Uno-specific information for `Lottie`. For a full description of the feature and instructions on using it, see [Lottie](https://learn.microsoft.com/windows/communitytoolkit/animations/lottie).

* The `CommunityToolkit.WinUI.Lottie` (for WinUI) namespace provides classes for rendering Lottie animations in a `Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer`.

## Using the `LottieVisualSource`

Add the following namespaces:

```xml
<Page
    ...
    xmlns:winui="using:Microsoft.UI.Xaml.Controls"
    xmlns:lottie="using:CommunityToolkit.WinUI.Lottie"
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

### References

`AnimatedVisualPlayer`, `LottieVisualSource` and `ThemableLottieVisualSource` are part of Uno Platform, so no package is needed on Uno Platform targets. The Skottie renderer comes with the Skia renderer, which is present by default. See [Lottie renderers](#lottie-renderers) below.

On Windows (WinAppSDK), reference the [`CommunityToolkit.WinUI.Lottie` NuGet package](https://www.nuget.org/packages/CommunityToolkit.WinUI.Lottie).

### Lottie renderers

On Uno Platform targets, Lottie animations are rendered by one of two renderers:

| Renderer | Used when | Coverage |
|----------|-----------|----------|
| Skottie (part of the Skia renderer) | The Skia renderer is present (the `Skia` feature, which is on by default), even when WebGPU draws the UI | Most Lottie features, as supported by [Skottie](https://skia.org/docs/user/modules/skottie/) |
| Managed engine (built into `Uno.WinUI`) | The Skia renderer is not present, for example in a WebGPU-only app | A subset of Lottie: unsupported features are skipped. |

The host builder picks the renderer automatically. An app can also register one explicitly, which takes precedence over both. Each renderer is created by its assembly's factory:

```csharp
var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    // Skottie (available whenever the Skia renderer is):
    .LottieRenderer(Uno.UI.Composition.Skia.SkiaBackend.CreateLottieRenderer())
    // or the managed engine:
    // .LottieRenderer(Uno.UI.Composition.Managed.ManagedBackend.CreateLottieRenderer())
    .Build();
```

Explicit registration is required on a trimmed or AOT-compiled head (iOS, tvOS), where the host builder can't find the Skottie renderer automatically.

For more information, see [AnimatedVisualPlayer Class](https://learn.microsoft.com/uwp/api/microsoft.ui.xaml.controls.animatedvisualplayer).

## Lottie JSON file location

On WASM, iOS, and macOS, you can put the Lottie .json files directly in a folder of your app's head project (for example "Lottie/myanimation.json") and set their Build action as Content.

On Android, Lottie .json files need to be added to the Assets folder. To match the same path as for the other platforms, the file could be stored at `Assets/Lottie/myanimation.json`. Set its Build action to AndroidAsset.

To reference the animations in XAML, use the `ms-appx:` URI, in this case `ms-appx:///Lottie/myanimation.json`.

## Using `embedded://` scheme

> [!WARNING]
> This feature is only available on Uno Platform targets. WinUI on Windows is not supported.

You can put the file as `<EmbeddedResource>` in your assembly and retrieve it using the following URL format as `UriSource`:

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

   ```css
   { Color: var(MyColor); }
   ```

   In that case, it will create a property (variable) named `MyColor`. It is possible to reuse the same property name on many layers in the same lottie file.

2. Use it that way:

   ```xml
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

## Writing a custom animated visual source

`LottieVisualSource` is one implementation of `IAnimatedVisualSource`; you can write your own to
feed `AnimatedVisualPlayer` from another format or from generated code.

Since Uno Platform 7.0 that interface matches WinUI exactly — a single method:

```csharp
IAnimatedVisual TryCreateAnimatedVisual(Compositor compositor, out object diagnostics);
```

Return an `IAnimatedVisual` carrying the composition `RootVisual`, its natural `Size` and its
`Duration`; `AnimatedVisualPlayer` drives playback, progress and measurement from there. Because
this is the WinUI contract, C# emitted by [LottieGen](https://aka.ms/lottiegen) with
`-Language CSharp -Public -WinUIVersion 3.0` compiles and runs unchanged.

Before 7.0, Uno's `IAnimatedVisualSource` was a nine-method Uno-only contract in which the source
drove playback itself. See the
[Uno Platform 7.0 migration guide](xref:Uno.Development.MigratingToUno7) for the old-to-new
mapping.

## Limitations

On Android, the `Stretch` mode of `Fill` is not currently supported.
