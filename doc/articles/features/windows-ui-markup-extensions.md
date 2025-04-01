---
uid: Uno.Features.WinUIMarkupExtension
---

# Markup Extensions

Uno Platform supports the `MarkupExtension` class, which gives the ability to enhance the XAML-first experience.

## Support for ProvideValue()

Given the following code:

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Markup;

namespace MyMarkupExtension;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public class Simple : Windows.UI.Xaml.Markup.MarkupExtension
{
    public string TextValue { get; set; }

    protected override object ProvideValue()
    {
        return TextValue + " markup extension";
    }
}
```

This class can be used as follows in the XAML:

```xml
<Grid xmlns:ex="using:MyMarkupExtension">
    <TextBlock Text="{ex:Simple TextValue='Just a simple '}"
                FontSize="16"
                Margin="0,0,0,40" />
</Grid>
```

## Support for ProvideValue(IXamlServiceProvider)

WinUI 3 provides enhanced support for MarkupExtension with the ability to get the markup context.

### IProvideValueTarget

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Markup;

namespace MyMarkupExtension;

public class SampleProvideValueTarget : MarkupExtension
{
    protected override object ProvideValue(IXamlServiceProvider serviceProvider)
    {
        var provideValueTarget = (IProvideValueTarget)context.GetService(typeof(IProvideValueTarget));

        return $"TargetProperty:{provideValueTarget.TargetProperty}, TargetObject:{provideValueTarget.TargetObject}";
    }
}
```

This class can be used as follows in the XAML:

```xml
<Grid xmlns:ex="using:MyMarkupExtension">
    <TextBlock Text="{ex:SampleProvideValueTarget}"
                FontSize="16"
                Margin="0,0,0,40" />
</Grid>
```

### IRootObjectProvider

With access to IRootObjectProvider becomes possible for a Markup extension to browse the visual tree, starting from the root of the XAML file.

This following example [from the WinUI specifications](https://github.com/microsoft/microsoft-ui-xaml-specs/blob/34b14114af141ceb843413bedb85705c9a2e9204/active/XamlServiceProvider/XamlServiceProviderApi.md#irootobjectprovider) give a glimpse of this feature.

Using the following XAML:

```csharp
public class DynamicBindExtension : MarkupExtension
{
    public DynamicBindExtension() { }

    public string Name { get; set; } = "";

    protected override object? ProvideValue(IXamlServiceProvider serviceProvider)
    {
        var root = ((IRootObjectProvider)serviceProvider.GetService(typeof(IRootObjectProvider))).RootObject;
        var info = root.GetType().GetProperty(Name);
        return info?.GetValue(root);
    }
}
```

The following XAML will display “Page Tag”:

```xml
<Page Tag='Page tag'
      x:Class="App1.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:App52"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid>
        <ContentControl>
            <ContentControl.ContentTemplate>
                <DataTemplate>
                    <TextBlock Text="{local:DynamicBind Name=Tag}" />
                </DataTemplate>
            </ContentControl.ContentTemplate>
        </ContentControl>
    </Grid>
</Page>
```

### IUriContext

Not supported as of Uno 4.3

### IXamlTypeResolver

Not supported as of Uno 4.3
