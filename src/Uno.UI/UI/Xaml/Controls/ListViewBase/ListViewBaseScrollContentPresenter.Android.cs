using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using Uno.UI;
using Uno.Disposables;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class ListViewBaseScrollContentPresenter : ContentPresenter, IScrollContentPresenter
	{

		private Thickness _oldPadding;

		public bool IsZoomEnabled { get; set; }
		public float MinimumZoomScale { get; set; }
		public float MaximumZoomScale { get; set; }
		public float ZoomScale { get; set; }

		[global::Uno.NotImplemented]
		public bool BringIntoViewOnFocusChange { get; set; }

		public override void ScrollTo(int x, int y)
		{
			NativePanel?.ScrollTo(x, y);
		}

		public void SmoothScrollTo(int x, int y)
		{
			NativePanel?.SmoothScrollTo(x, y);
		}

		IDisposable IScrollContentPresenter.Pad(Rect occludedRect)
		{
			var viewPortPoint = UIElement.TransformToVisual(this, null).TransformPoint(new Point());
			var viewPortSize = new Size(ActualWidth, ActualHeight);
			var viewPortRect = new Rect(viewPortPoint, viewPortSize);
			var intersection = viewPortRect;
			intersection.Intersect(occludedRect);

			if (!intersection.IsEmpty)
			{
				_oldPadding = NativePanel.Padding;
				SetOccludedRectPadding(new Thickness(_oldPadding.Left, _oldPadding.Top, _oldPadding.Right, intersection.Height));
			}

			return Disposable.Create(() => SetOccludedRectPadding(_oldPadding));
		}

		private Thickness _occludedRectPadding;
		private void SetOccludedRectPadding(Thickness occludedRectPadding)
		{	
			_occludedRectPadding = occludedRectPadding;
			NativePanel.Padding = occludedRectPadding;
		}

		public Rect MakeVisible(UIElement visual, Rect rectangle)
		{
			if (visual is FrameworkElement fe)
			{
				var scrollRect = new Rect(
					_occludedRectPadding.Left,
					_occludedRectPadding.Top,
					ActualWidth - _occludedRectPadding.Right,
					ActualHeight - _occludedRectPadding.Bottom
				);

				var visualPoint = UIElement.TransformToVisual(visual, null).TransformPoint(new Point());
				var visualRect = new Rect(visualPoint, new Size(fe.ActualWidth, fe.ActualHeight));

				var deltaX = Math.Min(visualRect.Left - scrollRect.Left, Math.Max(0, visualRect.Right - scrollRect.Right));
				var deltaY = Math.Min(visualRect.Top - scrollRect.Top, Math.Max(0, visualRect.Bottom - scrollRect.Bottom));

				NativePanel.SmoothScrollBy(ViewHelper.LogicalToPhysicalPixels(deltaX), ViewHelper.LogicalToPhysicalPixels(deltaY));
			}

			return rectangle;
		}
	}
}
