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
	/// The delta is treated as user input, not as a programmatic scroll.
	/// </remarks>
	/// <returns>
	/// Whether any ScrollViewer moved, along with the delta that no ScrollViewer in the ancestry could consume.
	/// </returns>
	internal static (bool DidScroll, double RemainingHorizontalDelta, double RemainingVerticalDelta) ChainScrollFromDescendant(
		UIElement origin,
		double horizontalDelta,
		double verticalDelta)
	{
		var remainingHorizontalDelta = horizontalDelta;
		var remainingVerticalDelta = verticalDelta;
		var didScroll = false;

		foreach (var ancestor in origin.GetVisualAncestry())
		{
			if (ancestor is not ScrollViewer { Presenter: { } presenter } scrollViewer)
			{
				continue;
			}

			var horizontalOffset = presenter.CanHorizontallyScroll && remainingHorizontalDelta is not 0
				? presenter.HorizontalOffset + remainingHorizontalDelta
				: (double?)null;
			var verticalOffset = presenter.CanVerticallyScroll && remainingVerticalDelta is not 0
				? presenter.VerticalOffset + remainingVerticalDelta
				: (double?)null;

			if (horizontalOffset is null && verticalOffset is null)
			{
				continue;
			}

			var initialHorizontalOffset = presenter.HorizontalOffset;
			var initialVerticalOffset = presenter.VerticalOffset;

			// This is user input, not a programmatic ChangeView. ChangeView arms the ScrollViewer's offset
			// intent, which the post-layout recompute then keeps re-applying and would fight the drag - so
			// clear it and go through the presenter exactly like PointerWheelScroll and
			// TryEnableDirectManipulation do.
			scrollViewer.ClearOffsetIntents();
			presenter.Set(
				horizontalOffset: horizontalOffset,
				verticalOffset: verticalOffset,
				disableAnimation: true,
				isIntermediate: false);

			// The presenter clamps and commits its offsets synchronously, unlike the ScrollViewer's own
			// properties which are refreshed through a notification, so the residual is read back from it.
			var consumedHorizontalDelta = presenter.HorizontalOffset - initialHorizontalOffset;
			var consumedVerticalDelta = presenter.VerticalOffset - initialVerticalOffset;

			remainingHorizontalDelta -= consumedHorizontalDelta;
			remainingVerticalDelta -= consumedVerticalDelta;
			didScroll |= consumedHorizontalDelta is not 0 || consumedVerticalDelta is not 0;

			if (Math.Abs(remainingHorizontalDelta) < ScrollChainingResidualEpsilon
				&& Math.Abs(remainingVerticalDelta) < ScrollChainingResidualEpsilon)
			{
				break;
			}
		}

		return (didScroll, remainingHorizontalDelta, remainingVerticalDelta);
	}
}
#endif
