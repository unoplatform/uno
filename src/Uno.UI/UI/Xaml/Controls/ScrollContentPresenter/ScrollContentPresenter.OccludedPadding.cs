using System;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ScrollContentPresenter
{
	private Thickness _oldPadding;
	private Thickness _occludedRectPadding;

	internal IDisposable Pad(Rect occludedRect)
	{
#if __ANDROID__
		var viewPortPoint = UIElement.TransformToVisual(this, null).TransformPoint(new Point());
#else
		var viewPortPoint = this.TransformToVisual(null).TransformPoint(new Point());
#endif
		// A previous Pad can still be applied (the keyboard occlusion changes while it animates);
		// it shrinks this presenter, so always reason on the unpadded viewport.
		var viewPortSize = new Size(ActualWidth, ActualHeight + _occludedRectPadding.Bottom);
		var viewPortRect = new Rect(viewPortPoint, viewPortSize);
		var intersection = viewPortRect;
		intersection.Intersect(occludedRect);

		if (intersection.IsEmpty)
		{
			RestoreOccludedRectPadding();
		}
		else
		{
			if (_occludedRectPadding == default)
			{
#if __ANDROID__
				_oldPadding = Native.Padding;
#else
				_oldPadding = Scroller.Padding;
#endif
			}

			// Shrink the viewport by the full occluded overlap: the minimal BringIntoView that
			// follows then scrolls the focused element flush with the new bottom edge, which sits
			// right above the keyboard.
			ApplyPadding(new Thickness(_oldPadding.Left, _oldPadding.Top, _oldPadding.Right, _oldPadding.Bottom + intersection.Height));
			_occludedRectPadding = new Thickness(0, 0, 0, intersection.Height);
		}

		return Disposable.Create(RestoreOccludedRectPadding);
	}

	private void RestoreOccludedRectPadding()
	{
		if (_occludedRectPadding != default)
		{
			_occludedRectPadding = default;
			ApplyPadding(_oldPadding);
		}
	}

	private void ApplyPadding(Thickness padding)
	{
#if __ANDROID__
		Native.Padding = padding;
#else
		Scroller.Padding = padding;
#endif
	}
}
