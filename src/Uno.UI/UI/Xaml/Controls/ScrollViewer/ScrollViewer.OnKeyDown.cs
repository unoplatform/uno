#nullable enable

using System;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
#if !__ANDROID__ && !__APPLE_UIKIT__ // ScrollContentPresenter.[Horizontal|Vertical]Offset not implemented on Android and iOS
		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);

			// On WASM, we could choose to scroll in the managed layer and suppress the native scrolling
			// but it can lead to some chaotic scenarios where it's really difficult to reconcile the
			// numbers between ScrollViewer and ScrollContentPresenter, so we choose to keep the scrolling native
#if !__WASM__
			var key = args.Key;

			// WinUI stops keyboard scrolling if TemplatedParentHandlesScrolling
			// but interestingly that doesn't seem to affect pointer wheel scrolling
			// despite the generic name implying that it would stop all scrolling
			if (Presenter is null || TemplatedParentHandlesScrolling)
			{
				return;
			}

			var oldHorizontalOffset = Presenter.TargetHorizontalOffset;
			var oldVerticalOffset = Presenter.TargetVerticalOffset;

			// Check whether scrolling is allowed and focus can be moved.
			var (shouldScroll, shouldMoveFocus) = HandleKeyDownForXYNavigation(args);

			if (!shouldScroll)
			{
				return;
			}

			var newOffset = key switch
			{
				VirtualKey.Up => Math.Max(0, oldVerticalOffset - GetDelta(ActualHeight)),
				VirtualKey.Down => Math.Min(oldVerticalOffset + GetDelta(ActualHeight), ScrollableHeight),
				VirtualKey.Left => Math.Max(0, oldHorizontalOffset - GetDelta(ActualWidth)),
				VirtualKey.Right => Math.Min(oldHorizontalOffset + GetDelta(ActualWidth), ScrollableWidth),
				VirtualKey.PageUp => Math.Max(0, oldVerticalOffset - ActualHeight),
				VirtualKey.PageDown => Math.Min(oldVerticalOffset + ActualHeight, ScrollableHeight),
				VirtualKey.Home => 0,
				VirtualKey.End => ScrollableHeight,
				_ => double.E
			};

			if (newOffset == double.E)
			{
				return;
			}

			if (Content is UIElement)
			{
				var canScrollHorizontally = Presenter.CanHorizontallyScroll;
				var canScrollVertically = Presenter.CanVerticallyScroll;

				if (canScrollHorizontally && key is VirtualKey.Left or VirtualKey.Right)
				{
					ScrollToHorizontalOffset(newOffset);
					args.Handled = !NumericExtensions.AreClose(oldHorizontalOffset, Presenter.TargetHorizontalOffset);
				}
				else if (canScrollVertically && key is not (VirtualKey.Left or VirtualKey.Right))
				{
					ScrollToVerticalOffset(newOffset);
					args.Handled = !NumericExtensions.AreClose(oldVerticalOffset, Presenter.TargetVerticalOffset);
				}

				args.Handled |= key is VirtualKey.PageUp or VirtualKey.PageDown;
			}

			if (args.Handled && shouldMoveFocus)
			{
				// Continue bubbling the event so that the focus can be moved.
				args.Handled = false;
			}

			// This gets the delta that should be applied when arrow keys are pressed as a function of the
			// ScrollViewer length in the scrolling direction. WinUI's logic is not quite clear, I just
			// reverse-engineered the numbers until they matched precisely. I think the original code just
			// has some weird rounding somewhere that makes the numbers weird to calculate.
			static int GetDelta(double l)
			{
				var length = (int)Math.Max(0, Math.Round(l) - 16);
				var result = 2 + length / 20 * 3;

				switch (length % 20)
				{
					case 0:
						break;
					case <= 7:
						result += 1;
						break;
					case <= 14:
						result += 2;
						break;
					default:
						result += 3;
						break;
				}

				return result;
			}
#endif
		}
#endif
	}
}
