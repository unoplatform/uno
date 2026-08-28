#nullable enable

#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using System;
using Uno.UI.Extensions;

namespace Microsoft.UI.Xaml.Controls;

partial class ScrollViewer
{
	// Sub-pixel leftovers are not worth chaining to the next ScrollViewer.
	private const double ScrollChainingResidualEpsilon = 0.01;

	/// <summary>
	/// Scrolls the <see cref="ScrollViewer"/> ancestry of <paramref name="origin"/> by the given delta,
	/// chaining outwards: each ScrollViewer consumes what it can and passes the rest to the next one.
	/// </summary>
	/// <remarks>
	/// This is the entry point for scroll deltas that originate outside of Uno's own pointer pipeline -
	/// currently a native HTML element hosted on Skia WebAssembly which has exhausted its own scrolling.
	/// The delta is treated as user touch input, not as a programmatic scroll.
	/// </remarks>
	/// <param name="isIntermediate">
	/// False only for the last delta of a gesture. Callers that end a gesture without a final delta
	/// should use <see cref="CompleteChainedScrollFromDescendant"/> instead.
	/// </param>
	/// <param name="isInertial">
	/// True when the delta comes from the inertia (fling) phase of a gesture rather than from the user's
	/// finger. A ScrollViewer with <see cref="IsScrollInertiaEnabled"/> false then refuses the delta.
	/// </param>
	/// <returns>
	/// Whether any ScrollViewer moved, along with the delta that no ScrollViewer in the ancestry could consume.
	/// </returns>
	internal static (bool DidScroll, double RemainingHorizontalDelta, double RemainingVerticalDelta) ChainScrollFromDescendant(
		UIElement origin,
		double horizontalDelta,
		double verticalDelta,
		bool isIntermediate,
		bool isInertial = false)
	{
		// These deltas cross the JS boundary, where nothing guarantees they are finite. A NaN reaching
		// ScrollContentPresenter.ValidateInputOffset throws, so reject it before it gets there.
		if (!double.IsFinite(horizontalDelta) || !double.IsFinite(verticalDelta))
		{
			return (false, 0, 0);
		}

		var remainingHorizontalDelta = horizontalDelta;
		var remainingVerticalDelta = verticalDelta;
		var didScroll = false;

		foreach (var ancestor in origin.GetVisualAncestry())
		{
			if (ancestor is not ScrollViewer { Presenter: { } presenter } scrollViewer)
			{
				continue;
			}

			var scrollsHorizontally = presenter.CanHorizontallyScroll && Math.Abs(remainingHorizontalDelta) >= ScrollChainingResidualEpsilon;
			var scrollsVertically = presenter.CanVerticallyScroll && Math.Abs(remainingVerticalDelta) >= ScrollChainingResidualEpsilon;

			if (isInertial && !scrollViewer.IsScrollInertiaEnabled)
			{
				// Mirrors IDirectManipulationHandler.OnInertiaStarting: with inertia opted out, this ScrollViewer
				// neither moves during the inertial phase nor lets the fling continue past it on the axes it takes
				// part in - an outer ScrollViewer flinging while this one stands still would look broken.
				if (scrollsHorizontally)
				{
					remainingHorizontalDelta = 0;
				}

				if (scrollsVertically)
				{
					remainingVerticalDelta = 0;
				}
			}
			else if (scrollsHorizontally || scrollsVertically)
			{
				var initialHorizontalOffset = presenter.HorizontalOffset;
				var initialVerticalOffset = presenter.VerticalOffset;

				// This is user input, not a programmatic ChangeView. ChangeView arms the ScrollViewer's offset
				// intent, which the post-layout recompute then keeps re-applying and would fight the drag - so
				// clear it and go through the presenter exactly like PointerWheelScroll and
				// TryEnableDirectManipulation do.
				scrollViewer.ClearOffsetIntents();
				presenter.Set(
					horizontalOffset: scrollsHorizontally ? initialHorizontalOffset + remainingHorizontalDelta : null,
					verticalOffset: scrollsVertically ? initialVerticalOffset + remainingVerticalDelta : null,
					disableAnimation: true,
					isIntermediate: isIntermediate,
					isTouch: true);

				// The presenter clamps and commits its offsets synchronously, unlike the ScrollViewer's own
				// properties which are refreshed through a notification, so the residual is read back from it.
				var consumedHorizontalDelta = presenter.HorizontalOffset - initialHorizontalOffset;
				var consumedVerticalDelta = presenter.VerticalOffset - initialVerticalOffset;

				remainingHorizontalDelta -= consumedHorizontalDelta;
				remainingVerticalDelta -= consumedVerticalDelta;
				didScroll |= Math.Abs(consumedHorizontalDelta) >= ScrollChainingResidualEpsilon
					|| Math.Abs(consumedVerticalDelta) >= ScrollChainingResidualEpsilon;
			}

			// A ScrollViewer that opts out of chaining absorbs the rest of the delta on that axis rather than
			// letting it reach its own ancestors, mirroring IDirectManipulationHandler.OnUpdated. Only applied
			// to an axis this ScrollViewer actually takes part in, so a horizontal-only scroller does not
			// swallow the vertical delta of the scroller above it.
			if (scrollsHorizontally && !scrollViewer.IsHorizontalScrollChainingEnabled)
			{
				remainingHorizontalDelta = 0;
			}

			if (scrollsVertically && !scrollViewer.IsVerticalScrollChainingEnabled)
			{
				remainingVerticalDelta = 0;
			}

			if (Math.Abs(remainingHorizontalDelta) < ScrollChainingResidualEpsilon
				&& Math.Abs(remainingVerticalDelta) < ScrollChainingResidualEpsilon)
			{
				break;
			}
		}

		return (didScroll, remainingHorizontalDelta, remainingVerticalDelta);
	}

	/// <summary>
	/// Reports the end of a gesture previously driven through <see cref="ChainScrollFromDescendant"/>, so
	/// consumers of <see cref="ViewChanged"/> observe a final non-intermediate view change.
	/// </summary>
	internal static void CompleteChainedScrollFromDescendant(UIElement origin)
	{
		foreach (var ancestor in origin.GetVisualAncestry())
		{
			if (ancestor is ScrollViewer { Presenter: { } presenter })
			{
				// No offset is passed: this only re-reports the offsets already in place, with
				// IsIntermediate false to close the sequence of intermediate deltas.
				presenter.Set(disableAnimation: true, isIntermediate: false, isTouch: true);
			}
		}
	}
}
#endif
