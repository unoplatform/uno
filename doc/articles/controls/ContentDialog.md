---
uid: Uno.Controls.ContentDialog
---

# ContentDialog

Represents a dialog box that can be customized to contain checkboxes, hyperlinks, buttons, and any other XAML content.

## Using ContentDialog with Uno

If you're considering using a dialog in your app, check out our comprehensive video for a detailed guidance on the implementation:

<div style="position: relative; width: 100%; padding-bottom: 56.25%;">
    <iframe
        src="https://www.youtube-nocookie.com/embed/VAUYH01LMEE"
        title="YouTube video player"
        frameborder="0"
        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
        allowfullscreen
        style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;">
    </iframe>
</div>

## Overlay Background (iOS/Android)

You can override the overlay background by adding the following resources to the application resources:

```xml
<SolidColorBrush x:Key="ContentDialogLightDismissOverlayBackground" Color="#99000000" />
```

> [!NOTE]
> There is no specific key to override for this other than `SystemControlPageBackgroundMediumAltMediumBrush` on Windows, see [Changing the Overlay background color for ContentDialog question on StackOverflow](https://stackoverflow.com/a/40397576).
