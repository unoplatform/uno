---
uid: Uno.Development.AdditionalFeatures
---

# Other Uno.UI Features

Uno.UI.Toolkit is a set of extension methods or behaviors used to enhance WinUI and activate device/OS specific features.

Those methods are built to have no effect on a platform that does not support the enhanced feature: no need to wrap them into conditional code.

## MenuFlyoutItem - Destructive style

iOS can display `MenuFlyoutItem` to warn user the action will be "destructive". In that case, the button text is displayed in red.

To set a `MenuFlyoutItem` as destructive, add the toolkit namespace to your XAML

```xml
xmlns:toolkit="using:Uno.UI.Toolkit"
```

And declare your `MenuFlyoutItem` as follow

```xml
<MenuFlyoutItem Text="Destructive action"
                toolkit:MenuFlyoutItemExtensions.IsDestructive="True" />
```

## UICommand - Destructive style

iOS can display `UICommand` to warn user the action will be "destructive". In that case, the button text is displayed in red.

To set a `UICommand` as destructive, add the toolkit namespace to your code

```csharp
using Uno.UI.Toolkit;
```

And declare your `UICommand` as follow

```csharp
var uic = new UICommand("Destructive action");
uic.SetDestructive(true);
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
