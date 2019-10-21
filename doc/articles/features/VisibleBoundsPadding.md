# VisibleBoundsPadding behavior
The `Uno.UI.Toolkit.VisibleBoundsPadding` is a behavior that overrides the `Padding` property of a control to ensure that its inner content is always inside the `ApplicationView.VisibleBounds` rectangle.

The `ApplicationView.VisibleBounds` is the rectangular area of the screen which is completely unobscured by any window decoration, such as the status bar, rounded screen corners or some screen notch (e.g. the iPhone X or Essential Phone top sensors notch).

In some cases it's acceptable for visible content to be partially obscured (a page background for example) and it should extend to fill the entire window. Other types of content should be restricted to the visible bounds (for instance readable text, or interactive controls). VisibleBoundsPadding enables this kind of fine-grained control over responsiveness to the visible bounds.

## Using the behavior
The behavior can be placed on any control that provides a Padding property (e.g. Grid, StackPanel, ListView, ScrollViewer, Control, ContentPresenter or Border), and will be automatically adjusted based on the control's absolute position inside the `ApplicationView.VisibleBounds` rectangle.

```xml
<UserControl x:Class="Uno.UI.Samples.Controls.SampleChooserControl"
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:toolkit="using:Uno.UI.Toolkit">
    <Grid toolkit:VisibleBoundsPadding.PaddingMask="All">
    </Grid>
</UserControl>
```

This grid will automatically be assigned a padding, and will be refreshed every time its size or the size of the `ApplicationView.VisibleBounds` changes.

## Specifying a target

The `Uno.UI.Toolkit.VisibleBoundsPadding` behavior defines a `PaddingMask` property that specifies if you want to set the padding to a specific bound of the UI. The values are:

- `None` (*default value*)
- `All`
- `Top`
- `Bottom`
- `Left`
- `Right`

Usage is as follows:

```xml
<UserControl x:Class="Uno.UI.Samples.Controls.SampleChooserControl"
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:toolkit="using:Uno.UI.Toolkit">
    <Grid toolkit:VisibleBoundsPadding.PaddingMask="Bottom" />
</UserControl>
```

**Note: 
- This behavior applies the greater of the existing padding or the calculated padding set on the element it is attached to.
**
