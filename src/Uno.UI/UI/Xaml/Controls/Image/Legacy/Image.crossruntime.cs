using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class Image : FrameworkElement
{
	private readonly SerialDisposable _sourceDisposable = new();

	partial void OnSourceChanged(ImageSource newValue, bool forceReload = false);

	private void OnStretchChanged(Stretch newValue, Stretch oldValue) => InvalidateArrange();

	internal override bool IsViewHit() => Source != null || base.IsViewHit();

#if !__NETSTD_REFERENCE__
	private protected override Rect? GetClipRect(bool needsClipToSlot, Rect finalRect, Size maxSize, Thickness margin)
		=> base.GetClipRect(needsClipToSlot, finalRect, maxSize, margin) ?? new Rect(default, RenderSize);
#endif
}
