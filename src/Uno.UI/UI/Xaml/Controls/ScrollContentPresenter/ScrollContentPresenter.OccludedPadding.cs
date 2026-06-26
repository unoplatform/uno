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

	internal IDisposable Pad(Rect occludedRect, Rect focusedElementRect)
	{
		var viewPortPoint = this.TransformToVisual(null).TransformPoint(new Point());
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
			_oldPadding = Scroller.Padding;
			var bottom = focusedElementRect.IsEmpty ?
				intersection.Height :
				Math.Max(intersection.Height - (viewPortRect.Height - focusedElementRect.Bottom), 0);

			SetOccludedRectPadding(new Thickness(_oldPadding.Left, _oldPadding.Top, _oldPadding.Right, bottom));
		}

		return Disposable.Create(() => SetOccludedRectPadding(new Thickness()));
	}

	private void SetOccludedRectPadding(Thickness occludedRectPadding)
	{
		_occludedRectPadding = occludedRectPadding;
		Scroller.Padding = occludedRectPadding;
	}
}
