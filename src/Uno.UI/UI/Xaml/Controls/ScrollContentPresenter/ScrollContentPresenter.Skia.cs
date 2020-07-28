using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter
	{
		// Default physical amount to scroll with Up/Down/Left/Right key
		const double ScrollViewerLineDelta = 16.0;

		// This value comes from WHEEL_DELTA defined in WinUser.h. It represents the universal default mouse wheel delta.
		const int ScrollViewerDefaultMouseWheelDelta = 120;

		// These macros compute how many integral pixels need to be scrolled based on the viewport size and mouse wheel delta.
		// - First the maximum between 48 and 15% of the viewport size is picked.
		// - Then that number is multiplied by (mouse wheel delta/120), 120 being the universal default value.
		// - Finally if the resulting number is larger than the viewport size, then that viewport size is picked instead.
		private static double GetVerticalScrollWheelDelta(Size size, double delta)
			=> Math.Min(Math.Floor(size.Height), Math.Round(delta * Math.Max(48.0, Math.Round(size.Height * 0.15, 0)) / ScrollViewerDefaultMouseWheelDelta, 0));
		private static double GetHorizontalScrollWheelDelta(Size size, double delta)
			=> Math.Min(Math.Floor(size.Width), Math.Round(delta * Math.Max(48.0, Math.Round(size.Width * 0.15, 0)) / ScrollViewerDefaultMouseWheelDelta, 0));

		// Minimum value of MinZoomFactor, ZoomFactor and MaxZoomFactor
		// ZoomFactor can be manipulated to a slightly smaller value, but
		// will jump back to 0.1 when the manipulation completes.
		const double ScrollViewerMinimumZoomFactor = 0.1f;

		// Tolerated rounding delta in pixels between requested scroll offset and
		// effective value. Used to handle non-DM-driven scrolls.
		const double ScrollViewerScrollRoundingTolerance = 0.05f;

		// Tolerated rounding delta in pixels between requested scroll offset and
		// effective value for cases where IScrollInfo is implemented by a
		// IManipulationDataProvider provider. Used to handle non-DM-driven scrolls.
		const double ScrollViewerScrollRoundingToleranceForProvider = 1.0f;

		// Delta required between the current scroll offsets and target scroll offsets
		// in order to warrant a call to BringIntoViewport instead of
		// SetOffsetsWithExtents, SetHorizontalOffset, SetVerticalOffset.
		const double ScrollViewerScrollRoundingToleranceForBringIntoViewport = 0.001f;

		// Tolerated rounding delta in between requested zoom factor and
		// effective value. Used to handle non-DM-driven zooms.
		const double ScrollViewerZoomExtentRoundingTolerance = 0.001f;

		// Tolerated rounding delta in between old and new zoom factor
		// in DM delta handling.
		const double ScrollViewerZoomRoundingTolerance = 0.000001f;

		// Delta required between the current zoom factor and target zoom factor
		// in order to warrant a call to BringIntoViewport instead of ZoomToFactor.
		const double ScrollViewerZoomRoundingToleranceForBringIntoViewport = 0.00001f;

		// When a snap point is within this tolerance of the scrollviewer's extent
		// minus its viewport we nudge the snap point back into place.
		const double ScrollViewerSnapPointLocationTolerance = 0.0001f;

		// If a ScrollViewer is going to reflow around docked CoreInputView occlussions
		// by shrinking its viewport, we want to at least guarantee that it will keep
		// an appropriate size.
		const double ScrollViewerMinHeightToReflowAroundOcclusions = 32.0f;



		public ScrollMode HorizontalScrollMode { get; set; }

		public ScrollMode VerticalScrollMode { get; set; }

		public float MinimumZoomScale { get; set; }

		public float MaximumZoomScale { get; set; }

		public ScrollBarVisibility VerticalScrollBarVisibility { get; set; }

		public ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }

		public double HorizontalOffset { get; internal set; }

		public double VerticalOffset { get; internal set; }

		internal Size ScrollBarSize
		{
			get
			{
				return new Size(0, 0);
			}
		}

		public ScrollContentPresenter()
		{
			PointerWheelChanged += ScrollContentPresenter_PointerWheelChanged;
		}

		private void ScrollContentPresenter_PointerWheelChanged(object sender, Input.PointerRoutedEventArgs e)
		{
			var properties = e.GetCurrentPoint(null).Properties;

			if (Content is UIElement c)
			{
				if (properties.IsHorizontalMouseWheel)
				{
					HorizontalOffset += GetHorizontalScrollWheelDelta(c.RenderSize, properties.MouseWheelDelta);
				}
				else
				{
					VerticalOffset += GetVerticalScrollWheelDelta(c.RenderSize, properties.MouseWheelDelta);
				}

				c.RenderTransform = new TranslateTransform() { X = -HorizontalOffset, Y = -VerticalOffset };

				Window.Current.QueueInvalidateRender();
			}


			(TemplatedParent as ScrollViewer)?.OnScrollInternal(HorizontalOffset, VerticalOffset, false);
		}
	}
}
