#if __ANDROID__ || __IOS__ || __MACOS__

using Uno.UI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml;

partial class UIElement
{
	private protected void SetTotalClipRect()
		=> SetTotalClipRect(out _);

	private protected void SetTotalClipRect(out Rect? logicalLayoutClip)
	{
		// This method calculates and sets the intersection of the layout clip and the clip provided using UIElement.Clip dependency property.
		var clip = Clip;
		var clipRect = clip?.Rect;
		if (clipRect.HasValue && clip?.Transform is { } transform)
		{
			clipRect = transform.TransformBounds(clipRect.Value);
		}

		var layoutClip = m_pLayoutClipGeometry;
		// Ancestor clip is not yet supported properly.
		//if (layoutClip.HasValue &&
		//	ShouldApplyLayoutClipAsAncestorClip() &&
		//	VisualTreeHelper.GetParent(this) is UIElement parent)
		//{
		//	layoutClip = GetTransform(from: this, to: parent).Inverse().Transform(layoutClip.Value);
		//}

		logicalLayoutClip = layoutClip;

		if (clipRect.HasValue || layoutClip.HasValue)
		{
			var totalClip = (clipRect ?? Rect.Infinite).IntersectWith(layoutClip ?? Rect.Infinite) ?? default(Rect);
			SetClipPlatform(totalClip);
		}
		else
		{
			SetClipPlatform(null);
		}
	}
}
#endif
