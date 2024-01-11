---
uid: Uno.Controls.Flyout
---

# Flyout

If you want to show a dimmed overlay underneath the flyout, set the `Flyout.LightDismissOverlayMode` property to `On`.

If you wish to customize the overlay color, add the following to your top-level `App.Resources`:

```xml
<SolidColorBrush x:Key="FlyoutLightDismissOverlayBackground"
                 Color="Blue" />
```
