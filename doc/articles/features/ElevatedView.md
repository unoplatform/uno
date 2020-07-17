# ElevatedView
On many design language like _Material Design_, there's a notion of elevation where a portion of the UI should be presented as been _elevated_ over the rest of the content.

In this case, the UWP proposed solution can't work on all platforms because of technical limitations. To address this problem, Uno provides a control called `ElevatedView`, able to produce a similar result on all platforms, providing an elevated result.

This control is very useful to create cards with both rounded corners and elevated effect - which could be challenging to produce on some platforms.

## How to use the `ElevatedView`

Fist you need to add the `toolkit` namespace in your XAML file:

```
xmlns:toolkit="using:Uno.UI.Toolkit"
```

After that, use the `ElevatedView` to host the content you need to be elevated:
``` xml
<StackPanel Orientation="Horizontal" Spacing="20">

	<Button>Non-Elevated Button</Button>

	<toolkit:ElevatedView Elevation="10">
		<Button>Elevated Button</Button>
	</toolkit:ElevatedView>

</StackPanel>
```

Will produce the following result:

![ElevatedView sample](../Assets/features/elevatedview/elevatedview-sample.png)

## Settings

You can set the following properties:

* `Elevation`: numeric number representing the level of the elevation effect. Typical values are between 5 and 30. Default to `0` - no elevation.
* `ShadowColor`: By default the casted shadow will be `Black`, but you can set any other value. You can reduce the shadow effect by using the alpha channel [except Android]. On Android, the shadow color can only be changed since Android Pie (API 28+). Default to `Black` with alpha channel at 25%.
* `Background`: Default to `Transparent`. Setting `null` will remove the shadow effect.
* `CornerRadius`: Use it to create rounded corner effects. The shadow should follow them.

## Particularities

* Make sure to _give room_ for the shadow in the layout.  Some platforms like macOS could easily clip the shadow. For the same reason, avoid wrapping the `<toolkit:ElevatedView>` directly in a `<ScrollViewer> ` because it's designed to clip its content.



