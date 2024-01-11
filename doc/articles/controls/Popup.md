---
uid: Uno.Controls.Popup
---

# Popup

## Namespace discrepancy

`Popup` is implemented on non-Windows platforms, but it's located in a different namespace than on Windows. This discrepancy will be fixed in an upcoming Uno version, but for now you can use [conditional `using` definitions](../platform-specific-csharp.md) to refer to `Popup` in C# code:

```csharp
#if NETFX_CORE
using _Popup = Windows.UI.Xaml.Controls.Primitives.Popup;
#else
using _Popup = Windows.UI.Xaml.Controls.Popup;
#endif
...

protected override void OnApplyTemplate()
{
    var popup = GetTemplateChild("PART_Popup") as _Popup;
}
```

In XAML markup, you can use `Popup` in the same way across all platforms without any workaround.

## Customizing light-dismiss overlay

If you want to show a dimmed overlay underneath the popup, set the `Popup.LightDismissOverlayMode` property to `On`.

If you wish to customize the overlay color, add the following to your top-level `App.Resources`:

```xml
<SolidColorBrush x:Key="PopupLightDismissOverlayBackground"
                 Color="Red" />
```
