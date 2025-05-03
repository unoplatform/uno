using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		var viewPortSize = new Size(ActualWidth, ActualHeight);
		var viewPortRect = new Rect(viewPortPoint, viewPortSize);
		var intersection = viewPortRect;
		intersection.Intersect(occludedRect);

		if (intersection.IsEmpty)
		{
			SetOccludedRectPadding(new Thickness());
		}
		else
		{
#if __ANDROID__
			_oldPadding = Native.Padding;
#else
			_oldPadding = Scroller.Padding;
#endif
			var bottom = Math.Max(intersection.Height - viewPortPoint.Y, 0);
			SetOccludedRectPadding(new Thickness(_oldPadding.Left, _oldPadding.Top, _oldPadding.Right, bottom));
		}

		return Disposable.Create(() => SetOccludedRectPadding(new Thickness()));
	}

	private void SetOccludedRectPadding(Thickness occludedRectPadding)
	{
		_occludedRectPadding = occludedRectPadding;
#if __ANDROID__
		Native.Padding = occludedRectPadding;
#else
		Scroller.Padding = occludedRectPadding;
#endif
	}
}
