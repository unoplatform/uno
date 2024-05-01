using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

partial class Image : FrameworkElement
{
	partial void OnSourceChanged(ImageSource newValue, bool forceReload = false);

	private void OnStretchChanged(Stretch newValue, Stretch oldValue) => InvalidateArrange();

	internal override bool IsViewHit() => Source != null || base.IsViewHit();

#if !__NETSTD_REFERENCE__
	private protected override Rect? GetClipRect(bool needsClipToSlot, double visualOffsetX, double visualOffsetY, Rect finalRect, Size maxSize, Thickness margin)
		=> base.GetClipRect(needsClipToSlot, visualOffsetX, visualOffsetY, finalRect, maxSize, margin) ?? new Rect(default, RenderSize);
#endif
}
