using System;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

partial class Image : FrameworkElement
{
	partial void OnSourceChanged(ImageSource newValue, bool forceReload = false);

	private void OnStretchChanged(Stretch newValue, Stretch oldValue) => InvalidateArrange();

	internal override bool IsViewHit() => Source != null || base.IsViewHit();

	private protected override Rect? GetClipRect(bool needsClipToSlot, Rect finalRect, Size maxSize, Thickness margin)
	{
		Console.WriteLine($"Clipping to {RenderSize}");
		return new Rect(default, RenderSize);
	}
}
