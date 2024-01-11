---
uid: Uno.Controls.ContentDialog
---

# ContentDialog

Represents a dialog box that can be customized to contain checkboxes, hyperlinks, buttons and any other XAML content.

## Overlay Background (ios/android)

You can override the overlay background by adding the following resources to the application resources:

```xml
<SolidColorBrush x:Key="ContentDialogLightDismissOverlayBackground" Color="#99000000" />
```

> note: There is no specific key to override for this other than `SystemControlPageBackgroundMediumAltMediumBrush` on window, see: https://stackoverflow.com/a/40397576.
