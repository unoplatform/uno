---
uid: Uno.Controls.MenuFlyout
---

# MenuFlyout

MenuFlyout is implemented to provide support for `ContextMenu` and `MenuBar` features.

## Platform specific behavior

On iOS and Android, the flyout can either be displayed using native popup, or Uno managed popups.

The default behavior of the MenuFlyout is to follow the value `FeatureConfiguration.Style.UseUWPDefaultStyles`, but this can be changed per MenuFlyout using the `FlyoutBase.UseNativePopup` property.

### iOS (native)

#### Destructive action

> If an alert button results in a destructive action, such as deleting content, set the button’s style to Destructive so that it gets appropriate formatting by the system.

```xml
xmlns:toolkit="using:Uno.UI.Toolkit"

<MenuFlyoutItem Text="Dangerous Menu Item 1"
                toolkit:MenuFlyoutItemExtensions.IsDestructive="True" />
```

#### Cancel button text

```xml
xmlns:toolkit="using:Uno.UI.Toolkit"

<MenuFlyout UseNativePopup="True"
            toolkit:MenuFlyoutExtensions.CancelTextIosOverride="Custom cancel text">
```
