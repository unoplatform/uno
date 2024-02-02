---
uid: Uno.Skia.Embedding.Native
---

# Embedding Native Controls in Skia Apps

In a Gtk or Wpf-based app, you can embed native controls in your Skia app. This is useful if you want to use a native control for a specific task, for instance, to integrate an existing GTK or WPF control.

In principle, the native control hosting places the native control in an overlay on top of the Skia control which renders the whole app's UI. The native control is then rendered by the native windowing system and is not part of the Uno Platform visual tree. That control cannot be styled by Uno Platform styles, and Uno Platform controls cannot be placed on top of native controls.

## Using embedded native controls

To embedded native controls in your Skia heads, you will need to set the native control as `Content` of a `ContentControl`, either via code or XAML.

For instance, in a Skia.GTK app, you can add a control as follows:

```xml
<ContentControl x:Name="MyControl">
    <ContentControl.Content>
        <Button xmlns="using:Gtk" />
    </ContentControl.Content>
</ContentControl>
```

## Cross-platform considerations

It is important to take into account platform specific limitations to the inclusion of such controls, particularly in libraries.

Libraries in Uno Platform are compiled for all platforms, and therefore cannot include native controls that are not available on all platforms. For instance, a library cannot include a native GTK control built for `net7.0` only and you may need to use head-specific XAML to include such controls.

## Limitations

The current implementation does not support the following features:

- Opacity changes to the `ContentControl` are not reflected to the native control
- Visibility changes are reflected to the native control. For the native control to disappear, you will need to set the `Content` to `null`, or remove any of the parents from the visual tree. `x:Load` can also be used to achieve this behavior.
