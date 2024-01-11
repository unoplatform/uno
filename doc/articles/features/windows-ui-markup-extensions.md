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

> [!NOTE]
> While this feature is available on all Uno Platform targets (UWP and WinUI), the UWP head on Windows does not support this feature, only the WinAppSDK head supports it.

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

Not supported as of Uno 4.3

### IUriContext

Not supported as of Uno 4.3

### IXamlTypeResolver

Not supported as of Uno 4.3
