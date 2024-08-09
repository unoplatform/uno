using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

partial class Image : FrameworkElement
{
	partial void OnSourceChanged(ImageSource newValue, bool forceReload = false);

	private void OnStretchChanged(Stretch newValue, Stretch oldValue) => InvalidateArrange();

	internal override bool IsViewHit() => Source != null || base.IsViewHit();

#if !__NETSTD_REFERENCE__
	private protected override Rect? GetClipRect(bool needsClipToSlot, Point visualOffset, Rect finalRect, Size maxSize, Thickness margin)
		=> base.GetClipRect(needsClipToSlot, visualOffset, finalRect, maxSize, margin) ?? new Rect(default, RenderSize);
#endif
}
