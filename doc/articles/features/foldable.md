---
uid: Uno.Features.Foldable
---

# Foldable devices and TwoPaneView

`Uno.WinUI.Foldable` adds Android foldable-device support for the [TwoPaneView](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.twopaneview) control. The package supplies the display-region information that TwoPaneView uses when an app is spanned across a separating hinge or fold.

TwoPaneView can also create a responsive two-pane layout based on the available size. The Foldable package is only needed for the Android foldable-device integration described in this article.

## Supported features

- `TwoPaneView` is implemented on WebAssembly, Skia, and Mobile targets.
- `Uno.WinUI.Foldable` provides foldable-device integration on Android.
- When the Android app is spanned across a separating hinge or fold, TwoPaneView uses the two display regions to select wide or tall mode and leaves the hinge or fold between the panes.
- When the app is not spanned, TwoPaneView uses its available width and height to select a mode. Its default minimum width and height for split modes are 641 pixels.

## Using Foldable with Uno

For projects using the `Uno.Sdk`, add the `Foldable` feature to the project's `<UnoFeatures>` property:

```xml
<UnoFeatures>$(UnoFeatures);Foldable</UnoFeatures>
```

This adds a reference to the [Uno.WinUI.Foldable NuGet package](https://www.nuget.org/packages/Uno.WinUI.Foldable). Restore the project after changing `UnoFeatures`.

The package is intended for Android targets. No application code is required to pass fold or hinge coordinates to TwoPaneView. The package provides that information to the control through Uno's platform integration.

## Configure TwoPaneView

Place the content for each side in `Pane1` and `Pane2`. Configure the order for wide and tall layouts according to the way the content should appear on the device:

```xml
<muxc:TwoPaneView
    WideModeConfiguration="LeftRight"
    TallModeConfiguration="TopBottom">
    <muxc:TwoPaneView.Pane1>
        <Grid>
            <TextBlock Text="Pane 1" />
        </Grid>
    </muxc:TwoPaneView.Pane1>
    <muxc:TwoPaneView.Pane2>
        <Grid>
            <TextBlock Text="Pane 2" />
        </Grid>
    </muxc:TwoPaneView.Pane2>
</muxc:TwoPaneView>
```

In the page that contains this markup, map the `muxc` prefix to the WinUI controls namespace:

```xml
xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
```

Use `PanePriority` to choose which pane remains visible when TwoPaneView is in `SinglePane` mode. `Mode` reports the current layout (`SinglePane`, `Wide`, or `Tall`), and `ModeChanged` can be used when application logic needs to respond to a layout change.

## See TwoPaneView in action

The [TwoPaneView sample](https://github.com/unoplatform/uno/tree/master/src/SamplesApp/SamplesApp.Samples/Microsoft_UI_Xaml_Controls/TwoPaneViewTests) demonstrates pane ordering, single-pane priority, size thresholds, and simulated wide and tall display regions.

For the complete control API and general usage guidance, see the [TwoPaneView documentation](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.twopaneview).
