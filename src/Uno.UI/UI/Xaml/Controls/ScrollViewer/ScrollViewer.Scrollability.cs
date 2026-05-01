#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		private void UpdateComputedVerticalScrollability(bool invalidate)
		{
			var scrollable = ScrollableHeight;
			var visibility = VerticalScrollBarVisibility;
			var mode = VerticalScrollMode;

			var allowed = ComputeIsScrollAllowed(visibility, mode);
			var computedVisibility = ComputeScrollBarVisibility(scrollable, visibility);
			var computedEnabled = ComputeIsScrollEnabled(scrollable, visibility, mode);

			if (_presenter is null)
			{
				ComputedVerticalScrollBarVisibility = computedVisibility; // Retro-compatibility, probably useless
				ComputedIsVerticalScrollEnabled = computedEnabled; // Retro-compatibility, probably useless
				return; // Control not ready yet
			}
			_presenter.CanVerticallyScroll = allowed;

			MaterializeVerticalScrollBarIfNeeded(computedVisibility);

			ComputedVerticalScrollBarVisibility = computedVisibility;
			ComputedIsVerticalScrollEnabled = computedEnabled;

#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
			// Support for the native scroll bars (delegated to the native _presenter).
			_presenter.NativeVerticalScrollBarVisibility = ComputeNativeScrollBarVisibility(scrollable, visibility, mode, _verticalScrollbar);
			if (invalidate && _verticalScrollbar is null)
			{
				InvalidateMeasure(); // Useless for managed ScrollBar, it will invalidate itself if needed.
			}
#endif
		}

		private void UpdateComputedHorizontalScrollability(bool invalidate)
		{
			var scrollable = ScrollableWidth;
			var visibility = HorizontalScrollBarVisibility;
			var mode = HorizontalScrollMode;

			var allowed = ComputeIsScrollAllowed(visibility, mode);
			var computedVisibility = ComputeScrollBarVisibility(scrollable, visibility);
			var computedEnabled = ComputeIsScrollEnabled(scrollable, visibility, mode);

			if (_presenter is null)
			{
				ComputedHorizontalScrollBarVisibility = computedVisibility; // Retro-compatibility, probably useless
				ComputedIsHorizontalScrollEnabled = computedEnabled; // Retro-compatibility, probably useless
				return; // Control not ready yet
			}
			_presenter.CanHorizontallyScroll = allowed;

			MaterializeHorizontalScrollBarIfNeeded(computedVisibility);

			ComputedHorizontalScrollBarVisibility = computedVisibility;
			ComputedIsHorizontalScrollEnabled = computedEnabled;

#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
			// Support for the native scroll bars (delegated to the native _presenter).
			_presenter.NativeHorizontalScrollBarVisibility = ComputeNativeScrollBarVisibility(scrollable, visibility, mode, _horizontalScrollbar);
			if (invalidate && _horizontalScrollbar is null)
			{
				InvalidateMeasure(); // Useless for managed ScrollBar, it will invalidate itself if needed.
			}
#endif
		}

		/// <summary>
		/// Determines if the scroll has been allowed on that scroll viewer, not matter if scroll is possible or not due to the size of the content.
		/// </summary>
		private static bool ComputeIsScrollAllowed(ScrollBarVisibility visibility, ScrollMode mode)
			=> visibility != ScrollBarVisibility.Disabled
				&& mode != ScrollMode.Disabled;

		private static Visibility ComputeScrollBarVisibility(double scrollable, ScrollBarVisibility visibility)
		{
			// Note: The ScrollMode DOES NOT impact the visibility of the ScrollBar, but just it's hit testability!

			switch (visibility)
			{
				case ScrollBarVisibility.Auto when scrollable > 0:
				case ScrollBarVisibility.Visible:
					return Visibility.Visible;

				default: // i.e.: Auto when scrollable <= 0; Hidden; Disabled;
					return Visibility.Collapsed;
			}
		}

		// Determines if the scrolling is enabled or not.
		// Unlike the Visibility of the scroll bar, this will also applies to the mousewheel!
		private static bool ComputeIsScrollEnabled(double scrollable, ScrollBarVisibility visibility, ScrollMode mode)
			=> scrollable > 0
				&& visibility != ScrollBarVisibility.Disabled
				&& mode != ScrollMode.Disabled;

#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
		private ScrollBarVisibility ComputeNativeScrollBarVisibility(double scrollable, ScrollBarVisibility visibility, ScrollMode mode, ScrollBar? managedScrollbar)
			=> (scrollable, visibility, mode, managedScrollbar) switch
			{
				(_, _, ScrollMode.Disabled, _) => ScrollBarVisibility.Disabled,
				(0, ScrollBarVisibility.Auto, _, null) => ScrollBarVisibility.Hidden, // If scrollable is 0, the managed scrollbar won't be realized, we prefer to hide the native one until we are sure!
				(_, _, _, null) when Uno.UI.Xaml.Controls.ScrollViewer.GetShouldFallBackToNativeScrollBars(this) => visibility,
				(_, ScrollBarVisibility.Disabled, _, _) => ScrollBarVisibility.Disabled,
				_ => ScrollBarVisibility.Hidden // If a managed scroll bar was set in the template, native scroll bar has to stay Hidden
			};
#endif
	}
}
