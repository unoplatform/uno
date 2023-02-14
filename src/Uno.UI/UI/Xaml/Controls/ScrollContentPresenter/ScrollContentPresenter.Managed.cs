#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using Uno.Extensions;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
using System.IO;
using Windows.Devices.Input;
using Windows.System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter, ICustomClippingElement
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

		private /*readonly - partial*/ IScrollStrategy _strategy;

		private bool _canHorizontallyScroll;
		public bool CanHorizontallyScroll
		{
			get => _canHorizontallyScroll
#if __SKIA__
			|| _forceChangeToCurrentView
#endif
			;
			set => _canHorizontallyScroll = value;
		}

		private bool _canVerticallyScroll;
		public bool CanVerticallyScroll
		{
			get => _canVerticallyScroll
#if __SKIA__
			|| _forceChangeToCurrentView
#endif
			;
			set => _canVerticallyScroll = value;
		}

		public double HorizontalOffset { get; private set; }

		public double VerticalOffset { get; private set; }

		public double ExtentHeight { get; internal set; }

		public double ExtentWidth { get; internal set; }

		internal Size ScrollBarSize => new Size(0, 0);

		internal Size? CustomContentExtent => null;

		private object RealContent => Content;

		partial void InitializePartial()
		{
#if __SKIA__
			_strategy = CompositorScrollStrategy.Instance;
#elif __MACOS__
			_strategy = TransformScrollStrategy.Instance;
#endif

			_strategy.Initialize(this);

			// Mouse wheel support
			PointerWheelChanged += ScrollContentPresenter_PointerWheelChanged;

			// Touch scroll support
			ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY; // Updated in PrepareTouchScroll!
			ManipulationStarting += PrepareTouchScroll;
			ManipulationDelta += UpdateTouchScroll;
			ManipulationCompleted += CompleteTouchScroll;
		}

		public void SetVerticalOffset(double offset)
			=> Set(verticalOffset: offset);

		public void SetHorizontalOffset(double offset)
			=> Set(horizontalOffset: offset);

		/// <inheritdoc />
		protected override void OnContentChanged(object oldValue, object newValue)
		{
			if (oldValue is UIElement oldElt)
			{
				_strategy.Update(oldElt, 0, 0, 1, disableAnimation: true);
			}

			base.OnContentChanged(oldValue, newValue);

			if (newValue is UIElement newElt)
			{
				_strategy.Update(newElt, HorizontalOffset, VerticalOffset, 1, disableAnimation: true);
			}
		}

		internal void OnMinZoomFactorChanged(float newValue) { }

		internal void OnMaxZoomFactorChanged(float newValue) { }

		internal bool Set(
			double? horizontalOffset = null,
			double? verticalOffset = null,
			float? zoomFactor = null,
			bool disableAnimation = true,
			bool isIntermediate = false)
		{
			var success = true;

			if (horizontalOffset is double hOffset)
			{
				var extentWidth = ExtentWidth;
				var viewportWidth = ViewportWidth;
				var scrollX = ValidateInputOffset(hOffset, 0, extentWidth - viewportWidth);

				success &= scrollX == hOffset;

				if (!NumericExtensions.AreClose(HorizontalOffset, scrollX))
				{
					HorizontalOffset = scrollX;
				}
			}

			if (verticalOffset is double vOffset)
			{
				var extentHeight = ExtentHeight;
				var viewportHeight = ViewportHeight;
				var scrollY = ValidateInputOffset(vOffset, 0, extentHeight - viewportHeight);

				success &= scrollY == vOffset;

				if (!NumericExtensions.AreClose(VerticalOffset, scrollY))
				{
					VerticalOffset = scrollY;
				}
			}

			Apply(disableAnimation, isIntermediate);

			return success;
		}

		private void Apply(bool disableAnimation, bool isIntermediate)
		{
			if (Content is UIElement contentElt)
			{
				_strategy.Update(contentElt, HorizontalOffset, VerticalOffset, 1, disableAnimation);
			}

			Scroller?.OnPresenterScrolled(HorizontalOffset, VerticalOffset, isIntermediate);

			// Note: We do not capture the offset so if they are altered in the OnPresenterScrolled,
			//		 we will apply only the final ScrollOffsets and only once.
			ScrollOffsets = new Point(HorizontalOffset, VerticalOffset);
			InvalidateViewport();
		}

		// Ensure the offset we're scrolling to is valid.
		private double ValidateInputOffset(double offset, int minOffset, double maxOffset)
		{
			if (offset.IsNaN())
			{
				throw new InvalidOperationException($"Invalid scroll offset value");
			}

			return Math.Max(minOffset, Math.Min(offset, maxOffset));
		}

		private void ScrollContentPresenter_PointerWheelChanged(object sender, Input.PointerRoutedEventArgs e)
		{
			var properties = e.GetCurrentPoint(null).Properties;

			if (Content is UIElement)
			{
				var canScrollHorizontally = CanHorizontallyScroll;
				var canScrollVertically = CanVerticallyScroll;

				if (e.KeyModifiers == VirtualKeyModifiers.Control)
				{
					// TODO: Handle zoom https://github.com/unoplatform/uno/issues/4309
				}
				else if (!canScrollVertically || properties.IsHorizontalMouseWheel || e.KeyModifiers == VirtualKeyModifiers.Shift)
				{
					if (canScrollHorizontally)
					{
						SetHorizontalOffset(HorizontalOffset + GetHorizontalScrollWheelDelta(DesiredSize, -properties.MouseWheelDelta * ScrollViewerDefaultMouseWheelDelta));
					}
				}
				else
				{
					SetVerticalOffset(VerticalOffset + GetVerticalScrollWheelDelta(DesiredSize, properties.MouseWheelDelta * ScrollViewerDefaultMouseWheelDelta));
				}
			}
		}

		private void PrepareTouchScroll(object sender, ManipulationStartingRoutedEventArgs e)
		{
			if (e.Container != this)
			{
				// This gesture is coming from a nested element, we just ignore it!
				return;
			}

			if (e.Pointer.Type != PointerDeviceType.Touch)
			{
				e.Mode = ManipulationModes.None;
				return;
			}

			if (!CanVerticallyScroll || ExtentHeight <= 0)
			{
				e.Mode &= ~ManipulationModes.TranslateY;
			}

			if (!CanHorizontallyScroll || ExtentWidth <= 0)
			{
				e.Mode &= ~ManipulationModes.TranslateX;
			}
		}

		private void UpdateTouchScroll(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (e.Container != this) // No needs to check the pointer type, if the manip is local it's touch, otherwise it was cancelled in starting.
			{
				// This gesture is coming from a nested element, we just ignore it!
				return;
			}

			Set(
				horizontalOffset: HorizontalOffset - e.Delta.Translation.X,
				verticalOffset: VerticalOffset - e.Delta.Translation.Y,
				isIntermediate: true);
		}

		private void CompleteTouchScroll(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			if (e.Container != this || (PointerDeviceType)e.PointerDeviceType != PointerDeviceType.Touch)
			{
				return;
			}

			Set(isIntermediate: false);
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => true;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => true; // force scrollviewer to always clip
	}
}
#endif
