
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Uno.UI;
using Uno.Disposables;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class ListViewBaseScrollContentPresenter : IScrollContentPresenter, INativeScrollContentPresenter
	{
		// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
		//
		// This class is about to be replaced by an implementation of INativeScrollContentPresenter which only adapts the 'NativePanel'.
		// For now it inherits from the ScrollContentPresenter as it's used directly in teh SV template,
		// and also sets itself as the Native implementation (acts as the adapter to the NativePanel for now)
		// 
		// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE 

		public bool IsZoomEnabled { get; set; }
		public float MinimumZoomScale { get; set; }
		public float MaximumZoomScale { get; set; }
		public float ZoomScale { get; set; }

		[global::Uno.NotImplemented]
		public bool BringIntoViewOnFocusChange { get; set; }

		Size? IScrollContentPresenter.CustomContentExtent
		{
			get
			{
				if (NativePanel?.NativeLayout is { } layouter)
				{
					var physicalExtent = new Size(layouter.HorizontalScrollRange, layouter.VerticalScrollRange);
					return physicalExtent.PhysicalToLogicalPixels();
				}

				return null;
			}
		}

		void IScrollContentPresenter.SmoothScrollTo(int physicalDeltaX, int physicalDeltaY)
			=> NativePanel?.SmoothScrollTo(physicalDeltaX, physicalDeltaY);

		public override void ScrollTo(int x, int y)
		{
			NativePanel?.ScrollTo(x, y);
		}

		Thickness INativeScrollContentPresenter.Padding
		{
			get => NativePanel?.Padding ?? default;
			set
			{
				if (NativePanel is { } native)
				{
					native.Padding = value;
				}
			}
		}

		void INativeScrollContentPresenter.SmoothScrollBy(int physicalDeltaX, int physicalDeltaY)
			=> NativePanel?.SmoothScrollBy(physicalDeltaX, physicalDeltaY);

		bool INativeScrollContentPresenter.Set(
			double? horizontalOffset,
			double? verticalOffset,
			float? zoomFactor,
			bool disableAnimation,
			bool isIntermediate)
			=> throw new NotImplementedException();
	}
}
