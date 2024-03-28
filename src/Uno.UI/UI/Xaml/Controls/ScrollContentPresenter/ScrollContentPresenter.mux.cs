using System;
using Windows.Foundation;
using Uno.UI.Helpers.WinUI;
using Uno.Extensions;
using Windows.UI.Xaml.Media;

using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls;

public partial class ScrollContentPresenter
{
#pragma warning disable IDE0051 // Remove unused private members
	// Default physical amount to scroll with Up/Down/Left/Right key
	//const double ScrollViewerLineDelta = 16.0;

	// This value comes from WHEEL_DELTA defined in WinUser.h. It represents the universal default mouse wheel delta.
	internal const int ScrollViewerDefaultMouseWheelDelta = 120;

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
	//const double ScrollViewerMinimumZoomFactor = 0.1f;

	// Tolerated rounding delta in pixels between requested scroll offset and
	// effective value. Used to handle non-DM-driven scrolls.
	//const double ScrollViewerScrollRoundingTolerance = 0.05f;

	// Tolerated rounding delta in pixels between requested scroll offset and
	// effective value for cases where IScrollInfo is implemented by a
	// IManipulationDataProvider provider. Used to handle non-DM-driven scrolls.
	//const double ScrollViewerScrollRoundingToleranceForProvider = 1.0f;

	// Delta required between the current scroll offsets and target scroll offsets
	// in order to warrant a call to BringIntoViewport instead of
	// SetOffsetsWithExtents, SetHorizontalOffset, SetVerticalOffset.
	//const double ScrollViewerScrollRoundingToleranceForBringIntoViewport = 0.001f;

	// Tolerated rounding delta in between requested zoom factor and
	// effective value. Used to handle non-DM-driven zooms.
	//const double ScrollViewerZoomExtentRoundingTolerance = 0.001f;

	// Tolerated rounding delta in between old and new zoom factor
	// in DM delta handling.
	//const double ScrollViewerZoomRoundingTolerance = 0.000001f;

	// Delta required between the current zoom factor and target zoom factor
	// in order to warrant a call to BringIntoViewport instead of ZoomToFactor.
	//const double ScrollViewerZoomRoundingToleranceForBringIntoViewport = 0.00001f;

	// When a snap point is within this tolerance of the scrollviewer's extent
	// minus its viewport we nudge the snap point back into place.
	//const double ScrollViewerSnapPointLocationTolerance = 0.0001f;

	// If a ScrollViewer is going to reflow around docked CoreInputView occlussions
	// by shrinking its viewport, we want to at least guarantee that it will keep
	// an appropriate size.
	//const double ScrollViewerMinHeightToReflowAroundOcclusions = 32.0f;
#pragma warning restore IDE0051 // Remove unused private members

	// BringIntoView functionality is ported from WinUI ScrollPresenter
	// https://github.com/microsoft/microsoft-ui-xaml/blob/main/dev/ScrollPresenter/ScrollPresenter.cpp
	// with partial modifications to match the ScrollViewer control behavior.

	protected override void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs args)
	{
		base.OnBringIntoViewRequested(args);

		UIElement content = RealContent as UIElement;

		if (args.Handled ||
			args.TargetElement == this ||
			(args.TargetElement == content && content?.Visibility == Visibility.Collapsed) ||
			!SharedHelpers.IsAncestor(args.TargetElement, this, true /*checkVisibility*/))
		{
			// Ignore the request when:
			// - It was handled already.
			// - The target element is this ScrollPresenter itself. A parent scrollPresenter may fulfill the request instead then.
			// - The target element is effectively collapsed within the ScrollPresenter.
			return;
		}

		Rect targetRect = new Rect();
		double targetZoomedHorizontalOffset = 0.0;
		double targetZoomedVerticalOffset = 0.0;
		double appliedOffsetX = 0.0;
		double appliedOffsetY = 0.0;
		var viewportWidth = ViewportWidth;
		var viewportHeight = ViewportHeight;
		var zoomFactor = Scroller.ZoomFactor;

#if __ANDROID__ // Adjust for region blocked by keyboard.
		viewportHeight -= _occludedRectPadding.Bottom;
#endif

		// Compute the target offsets based on the provided BringIntoViewRequestedEventArgs.
		ComputeBringIntoViewTargetOffsets(
			content,
			args,
			out targetZoomedHorizontalOffset,
			out targetZoomedVerticalOffset,
			out appliedOffsetX,
			out appliedOffsetY,
			out targetRect);

		// Do not include the applied offsets so that potential parent bring-into-view contributors ignore that shift.
		Rect nextTargetRect = new Rect(
			targetRect.X * zoomFactor - targetZoomedHorizontalOffset - appliedOffsetX,
			targetRect.Y * zoomFactor - targetZoomedVerticalOffset - appliedOffsetY,
			Math.Min(targetRect.Width * zoomFactor, viewportWidth),
			Math.Min(targetRect.Height * zoomFactor, viewportHeight));

		Rect viewportRect = new Rect(
			0.0f,
			0.0f,
			(float)viewportWidth,
			(float)viewportHeight);

		var verticalOffset = Scroller.VerticalOffset;
		var horizontalOffset = Scroller.HorizontalOffset;
		var zoomedVerticalOffset = verticalOffset;
		var zoomedHorizontalOffset = horizontalOffset;

		if (targetZoomedHorizontalOffset != zoomedHorizontalOffset ||
			targetZoomedVerticalOffset != zoomedVerticalOffset)
		{
			Scroller.ChangeView(targetZoomedHorizontalOffset, targetZoomedVerticalOffset, zoomFactor, !args.AnimationDesired);
		}
		else
		{
			// No offset change was triggered because the target offsets are the same as the current ones. Mark the operation as completed immediately.				
			//RaiseViewChangeCompleted(true /*isForScroll*/, ScrollPresenterViewChangeResult::Completed, offsetsChangeCorrelationId);
		}

		if (SharedHelpers.DoRectsIntersect(nextTargetRect, viewportRect))
		{
			// Next bring a portion of this ScrollPresenter into view.
			args.TargetRect = nextTargetRect;
			args.TargetElement = Scroller;
			args.HorizontalOffset = args.HorizontalOffset - appliedOffsetX;
			args.VerticalOffset = args.VerticalOffset - appliedOffsetY;
		}
		else
		{
			// This ScrollPresenter did not even partially bring the TargetRect into its viewport.
			// Mark the operation as handled since no portion of this ScrollPresenter needs to be brought into view.
			args.Handled = true;
		}
	}

	private void ComputeBringIntoViewTargetOffsets(
		UIElement content,
		BringIntoViewRequestedEventArgs requestEventArgs,
		out double targetZoomedHorizontalOffset,
		out double targetZoomedVerticalOffset,
		out double appliedOffsetX,
		out double appliedOffsetY,
		out Rect targetRect)
	{

		targetZoomedHorizontalOffset = 0.0;
		targetZoomedVerticalOffset = 0.0;

		appliedOffsetX = 0.0;
		appliedOffsetY = 0.0;

		targetRect = new Rect();

		var target = requestEventArgs.TargetElement;

		MUX_ASSERT(content != null);
		MUX_ASSERT(target != null);

		Rect transformedRect = GetDescendantBounds(content, target, requestEventArgs.TargetRect);

		double targetX = transformedRect.X;
		double targetWidth = transformedRect.Width;
		double targetY = transformedRect.Y;
		double targetHeight = transformedRect.Height;

		var viewportWidth = ViewportWidth;
		var viewportHeight = ViewportHeight;
		var zoomFactor = Scroller.ZoomFactor;

#if __ANDROID__ // Adjust for region blocked by keyboard.
		viewportHeight -= _occludedRectPadding.Bottom;
#endif

		if (!double.IsNaN(requestEventArgs.HorizontalAlignmentRatio))
		{
			// Account for the horizontal alignment ratio
			MUX_ASSERT(requestEventArgs.HorizontalAlignmentRatio >= 0.0 && requestEventArgs.HorizontalAlignmentRatio <= 1.0);


			targetX += (targetWidth - viewportWidth / zoomFactor) * requestEventArgs.HorizontalAlignmentRatio;
			targetWidth = viewportWidth / zoomFactor;
		}

		if (!double.IsNaN(requestEventArgs.VerticalAlignmentRatio))
		{
			// Account for the vertical alignment ratio
			MUX_ASSERT(requestEventArgs.VerticalAlignmentRatio >= 0.0 && requestEventArgs.VerticalAlignmentRatio <= 1.0);

			targetY += (targetHeight - viewportHeight / zoomFactor) * requestEventArgs.VerticalAlignmentRatio;
			targetHeight = viewportHeight / zoomFactor;
		}

		var verticalOffset = Scroller.VerticalOffset;
		var horizontalOffset = Scroller.HorizontalOffset;
		var zoomedVerticalOffset = verticalOffset;
		var zoomedHorizontalOffset = horizontalOffset;

		double targetZoomedHorizontalOffsetTmp = ComputeZoomedOffsetWithMinimalChange(
			zoomedHorizontalOffset,
			zoomedHorizontalOffset + viewportWidth,
			targetX * zoomFactor,
			(targetX + targetWidth) * zoomFactor);
		double targetZoomedVerticalOffsetTmp = ComputeZoomedOffsetWithMinimalChange(
			zoomedVerticalOffset,
			zoomedVerticalOffset + viewportHeight,
			targetY * zoomFactor,
			(targetY + targetHeight) * zoomFactor);

		double scrollableWidth = Scroller.ScrollableWidth;
		double scrollableHeight = Scroller.ScrollableHeight;

#if __ANDROID__ // Adjust for region blocked by keyboard.
		scrollableHeight += _occludedRectPadding.Bottom;
#endif

		targetZoomedHorizontalOffsetTmp = Math.Clamp(targetZoomedHorizontalOffsetTmp, 0.0, scrollableWidth);
		targetZoomedVerticalOffsetTmp = Math.Clamp(targetZoomedVerticalOffsetTmp, 0.0, scrollableHeight);

		double offsetX = requestEventArgs.HorizontalOffset;
		double offsetY = requestEventArgs.VerticalOffset;
		double appliedOffsetXTmp = 0.0;
		double appliedOffsetYTmp = 0.0;

		// If the target offset is within bounds and an offset was provided, apply as much of it as possible while remaining within bounds.
		if (offsetX != 0.0 && targetZoomedHorizontalOffsetTmp >= 0.0)
		{
			if (targetZoomedHorizontalOffsetTmp <= scrollableWidth)
			{
				if (offsetX > 0.0)
				{
					appliedOffsetXTmp = Math.Min(targetZoomedHorizontalOffsetTmp, offsetX);
				}
				else
				{
					appliedOffsetXTmp = -Math.Min(scrollableWidth - targetZoomedHorizontalOffsetTmp, -offsetX);
				}
				targetZoomedHorizontalOffsetTmp -= appliedOffsetXTmp;
			}
		}

		if (offsetY != 0.0 && targetZoomedVerticalOffsetTmp >= 0.0)
		{
			if (targetZoomedVerticalOffsetTmp <= scrollableHeight)
			{
				if (offsetY > 0.0)
				{
					appliedOffsetYTmp = Math.Min(targetZoomedVerticalOffsetTmp, offsetY);
				}
				else
				{
					appliedOffsetYTmp = -Math.Min(scrollableHeight - targetZoomedVerticalOffsetTmp, -offsetY);
				}
				targetZoomedVerticalOffsetTmp -= appliedOffsetYTmp;
			}
		}

		MUX_ASSERT(targetZoomedHorizontalOffsetTmp >= 0.0);
		MUX_ASSERT(targetZoomedVerticalOffsetTmp >= 0.0);
		MUX_ASSERT(targetZoomedHorizontalOffsetTmp <= scrollableWidth);
		MUX_ASSERT(targetZoomedVerticalOffsetTmp <= scrollableHeight);

		//if (snapPointsMode == ScrollingSnapPointsMode::Default)
		//{
		//	// Finally adjust the target offsets based on snap points
		//	targetZoomedHorizontalOffsetTmp = ComputeValueAfterSnapPoints<ScrollSnapPointBase>(
		//		targetZoomedHorizontalOffsetTmp, m_sortedConsolidatedHorizontalSnapPoints);
		//	targetZoomedVerticalOffsetTmp = ComputeValueAfterSnapPoints<ScrollSnapPointBase>(
		//		targetZoomedVerticalOffsetTmp, m_sortedConsolidatedVerticalSnapPoints);

		//	// Make sure the target offsets are within the scrollable boundaries
		//	targetZoomedHorizontalOffsetTmp = targetZoomedHorizontalOffsetTmp.Clamp(0.0, scrollableWidth);
		//	targetZoomedVerticalOffsetTmp = targetZoomedVerticalOffsetTmp.Clamp(0.0, scrollableHeight);

		//	MUX_ASSERT(targetZoomedHorizontalOffsetTmp >= 0.0);
		//	MUX_ASSERT(targetZoomedVerticalOffsetTmp >= 0.0);
		//	MUX_ASSERT(targetZoomedHorizontalOffsetTmp <= scrollableWidth);
		//	MUX_ASSERT(targetZoomedVerticalOffsetTmp <= scrollableHeight);
		//}

		targetZoomedHorizontalOffset = targetZoomedHorizontalOffsetTmp;
		targetZoomedVerticalOffset = targetZoomedVerticalOffsetTmp;

		appliedOffsetX = appliedOffsetXTmp;
		appliedOffsetY = appliedOffsetYTmp;

		targetRect = new Rect(
			targetX,
			targetY,
			targetWidth,
			targetHeight);
	}

	private double ComputeZoomedOffsetWithMinimalChange(
		double viewportStart,
		double viewportEnd,
		double childStart,
		double childEnd)
	{
		bool above = childStart < viewportStart && childEnd < viewportEnd;
		bool below = childEnd > viewportEnd && childStart > viewportStart;
		bool larger = (childEnd - childStart) > (viewportEnd - viewportStart);

		// # CHILD POSITION   CHILD SIZE   SCROLL   REMEDY
		// 1 Above viewport   <= viewport  Down     Align top edge of content & viewport
		// 2 Above viewport   >  viewport  Down     Align bottom edge of content & viewport
		// 3 Below viewport   <= viewport  Up       Align bottom edge of content & viewport
		// 4 Below viewport   >  viewport  Up       Align top edge of content & viewport
		// 5 Entirely within viewport      NA       No change
		// 6 Spanning viewport             NA       No change
		if ((above && !larger) || (below && larger))
		{
			// Cases 1 & 4
			return childStart;
		}
		else if (above || below)
		{
			// Cases 2 & 3
			return childEnd - viewportEnd + viewportStart;
		}

		// cases 5 & 6
		return viewportStart;
	}

	private Rect GetDescendantBounds(
		UIElement content,
		UIElement descendant,
		Rect descendantRect)
	{
		MUX_ASSERT(content != null);

		FrameworkElement contentAsFE = content as FrameworkElement;

		GeneralTransform transform = descendant.TransformToVisual(content);
		Thickness contentMargin = new Thickness();

		// TODO Uno specific: We need to add presenter padding, as it is not accounted
		// for when bringing into view nested ScrollViewer.
		// This is not happening in the WinUI ScrollView control,
		// but matches our ScrollViewer requirements.
		if (descendant is ScrollViewer sv)
		{
			contentMargin = sv.Presenter.Margin;
		}

		if (contentAsFE != null)
		{
			contentMargin = new Thickness(
				contentMargin.Left + contentAsFE.Margin.Left,
				contentMargin.Top + contentAsFE.Margin.Top,
				contentMargin.Right + contentAsFE.Margin.Right,
				contentMargin.Bottom + contentAsFE.Margin.Bottom);
		}

		return transform.TransformBounds(
			new Rect(
				contentMargin.Left + descendantRect.X,
				contentMargin.Top + descendantRect.Y,
				descendantRect.Width,
				descendantRect.Height));
	}
}
