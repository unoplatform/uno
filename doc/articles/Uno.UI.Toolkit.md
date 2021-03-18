# Uno.UI.Toolkit

Uno.UI.Toolkit is a set of extension methods or behaviors used to enhance UWP and activate device/OS specific features.

Those methods are built to have no effect on platform that does not support the enhanced feature: no need to wrap them into conditional code.

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
