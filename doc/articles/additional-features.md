---
uid: Uno.Development.AdditionalFeatures
---

# Additional Uno Platform Features

Uno Platform ships a set of extension methods, attached properties and behaviors that enhance WinUI and activate device/OS specific features. They are available on every target, including WinAppSDK.

Those methods are built to have no effect on a platform that does not support the enhanced feature: no need to wrap them into conditional code.

## UICommand - Destructive style

iOS can display `UICommand` to warn user the action will be "destructive". In that case, the button text is displayed in red.

`IsDestructive` is declared on `UICommand` itself, and only on iOS and tvOS heads — so set it
directly, guarded by platform-conditional code if the project also builds other heads:

```csharp
var uic = new UICommand("Destructive action");
#if __IOS__ || __TVOS__
uic.IsDestructive = true;
#endif
```

## FromJson markup extension

`FromJson` lets you keep structured sample data in XAML without creating temporary view models. The extension takes a JSON payload, converts it to a dynamic object graph (`ExpandoObject` and nested dictionaries/lists), and assigns it to the consuming property.

The conversion follows simple rules so you always know what type a binding receives:

- JSON objects → `ExpandoObject` (access through `IDictionary<string, object?>`)
- JSON arrays → `List<object?>`
- JSON numbers → `int` (`Int32`) when the value fits, otherwise `double`
- JSON booleans → `bool`
- JSON strings → `string`
- JSON null → `null`

1. Store the JSON in an `x:String` resource so you can keep it formatted with `xml:space="preserve"`.
2. Use the markup extension to deserialize the resource and set the `DataContext`, `ItemsSource`, or any other property that accepts an arbitrary object.

```xml
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:uum="using:Uno.UI.Markup"
      xmlns:xml="http://www.w3.org/XML/1998/namespace">
    <Grid.Resources>
        <x:String x:Key="SampleData" xml:space="preserve">
{
    "Title": "Runtime Test",
    "Owner": { "Name": "Uno Platform" },
    "Numbers": [ 1, 2, 3 ]
}
        </x:String>
    </Grid.Resources>

    <TextBlock DataContext="{uum:FromJson Source={StaticResource SampleData}}"
               Text="{Binding Owner.Name}" />
</Grid>
```

You can also apply `FromJson` directly inside `Page.DataContext`, either by referencing an `x:String` resource or by using its content property to inline JSON:

```xml
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:uum="using:Uno.UI.Markup"
      xmlns:xml="http://www.w3.org/XML/1998/namespace">
    <Page.Resources>
        <x:String x:Key="SamplePerson" xml:space="preserve">
            {
                "Name": "Inline"
            }
        </x:String>
    </Page.Resources>

    <Page.DataContext>
        <uum:FromJson Source="{StaticResource SamplePerson}" />
    </Page.DataContext>

    <TextBlock Text="{Binding Name}" />
</Page>
```

```xml
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:uum="using:Uno.UI.Markup">
    <Page.DataContext>
        <uum:FromJson>
            <uum:FromJson.Source>
            {
                "Name": "Inline"
            }
            </uum:FromJson.Source>
        </uum:FromJson>
    </Page.DataContext>

    <TextBlock Text="{Binding Name}" />
</Page>
```

If the JSON string is empty or invalid, the extension throws a `XamlParseException`, allowing issues to surface early during page initialization.

## ScrollBar - dragging the thumb with a finger

WinUI has the `ScrollBar` parts ignore touch on purpose: touch scrolling goes through direct manipulation, and the bar is only an indicator, so a finger on the thumb does nothing. A pointer reveals the interactive bar on hover, and touch has no hover. That is the default in Uno Platform as well.

On a touch-only target - a browser on a tablet most notably - there is no pointer to reveal that bar with, so the user has nothing to drag. Opt into an interactive bar with `ScrollBarExtensions.IsTouchThumbDragEnabled`:

```xml
xmlns:toolkit="using:Uno.UI.Toolkit"
```

```xml
<ScrollBar Orientation="Vertical"
           toolkit:ScrollBarExtensions.IsTouchThumbDragEnabled="True" />
```

While enabled, the bar keeps its interactive (mouse) indicator instead of the thin touch indicator, which both keeps it visible and lets a finger drag the thumb. This also shows on a device with both a mouse and a touch screen: after a finger pan the bar stays expanded instead of switching to the thin indicator. Panning the content with a finger is unaffected.

The property is honoured wherever scroll bar input is handled by Uno Platform's own `ScrollBar` - that is, on every Skia target: Desktop (Windows, macOS, Linux), WebAssembly, Android, and iOS. It has no effect on Windows (WinAppSDK), where the WinUI `ScrollBar` handles the input. The native Android, iOS, and WebAssembly UI backends are not supported targets for this opt-in - touch scrolling there is performed by the OS - so leave the property unset on them.

The bar of a `ScrollViewer` is a template part, so it cannot be addressed directly. Reach it with an implicit `Style` in your application - or page - resources, which is also how the bars inside a third-party control such as the CommunityToolkit `DataGrid` are reached:

```xml
<Style TargetType="ScrollBar">
    <Setter Property="toolkit:ScrollBarExtensions.IsTouchThumbDragEnabled" Value="True" />
</Style>
```

A known limitation: a finger pan which *starts* on a visible bar does not scroll the content, because hit-testing resolves into the `ScrollBar` and the content presenter is a sibling of it. Start the pan over the content instead.
