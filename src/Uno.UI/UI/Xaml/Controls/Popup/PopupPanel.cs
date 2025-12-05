using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.DataBinding;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Input;
using Uno.Helpers;
using Uno.UI.Xaml.Core;

#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal partial class PopupPanel : Panel
{
#if UNO_HAS_UIELEMENT_IMPLICIT_PINNING
	private ManagedWeakReference _popup;

	public Popup Popup
	{
		get => _popup?.Target as Popup;
		set
		{
			WeakReferencePool.ReturnWeakReference(this, _popup);
			_popup = WeakReferencePool.RentWeakReference(this, value);
		}
	}
#else
	public Popup Popup { get; }
#endif

	private FrameworkElement _trackedPlacementTarget;

	public PopupPanel(Popup popup)
	{
		Popup = popup ?? throw new ArgumentNullException(nameof(popup));
		Visibility = Visibility.Collapsed;
		PointerPressed += OnPointerPressed;
	}

	protected Size _lastMeasuredSize;

	protected override Size MeasureOverride(Size availableSize)
	{
		// Usually this check is achieved by the parent, but as this Panel
		// is injected at the root (it's a subView of the Window), we make sure
		// to enforce it here.
		var isOpen = Visibility != Visibility.Collapsed;
		if (!isOpen)
		{
			availableSize = default; // 0,0
		}

		var child = this.GetChildren().FirstOrDefault();
		if (child == null)
		{
			return availableSize;
		}

		if (!isOpen || Popup.CustomLayouter == null)
		{
			_lastMeasuredSize = MeasureElement(child, availableSize);
		}
		else
		{
			Rect visibleBounds = XamlRoot?.VisualTree.VisibleBounds ?? default;
			visibleBounds.Width = Math.Min(availableSize.Width, visibleBounds.Width);
			visibleBounds.Height = Math.Min(availableSize.Height, visibleBounds.Height);

			_lastMeasuredSize = Popup.CustomLayouter.Measure(availableSize, visibleBounds.Size);
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug($"Measured PopupPanel #={GetHashCode()} ({(Popup.CustomLayouter == null ? "" : "**using custom layouter**")}) DC={Popup.DataContext} child={child} offset={Popup.HorizontalOffset},{Popup.VerticalOffset} availableSize={availableSize} measured={_lastMeasuredSize}");
		}

		// Note that we return the availableSize and not the _lastMeasuredSize. This is because this
		// Panel always take the whole screen for the dismiss layer, but it's content will not.
		return availableSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		// Note: Here finalSize is expected to be the be the size of the window

		// Ensure we're tracking the current placement target for viewport changes
		UpdatePlacementTargetTracking();

		var size = _lastMeasuredSize;

		// Usually this check is achieved by the parent, but as this Panel
		// is injected at the root (it's a subView of the Window), we make sure
		// to enforce it here.
		var isOpen = Visibility != Visibility.Collapsed;
		if (!isOpen)
		{
			size = finalSize = default;
		}

		var child = this.GetChildren().FirstOrDefault();
		if (child == null)
		{
			return finalSize;
		}

		if (!isOpen)
		{
			ArrangeElement(child, default);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Arranged PopupPanel #={GetHashCode()} **closed** DC={Popup.DataContext} child={child} finalSize={finalSize}");
			}
		}
		else if (Popup.CustomLayouter == null)
		{
			// Gets the location of the popup (or its Anchor) in the VisualTree, so we will align Top/Left with it
			// Note: we do not prevent overflow of the popup on any side as UWP does not!
			//		 (And actually it also lets the view appear out of the window ...)
			Point anchorLocation = default;
			if (Popup.PlacementTarget is { } anchor)
			{
				anchorLocation = anchor.TransformToVisual(this).TransformPoint(default);
			}
			else
			{
				anchorLocation = Popup.TransformToVisual(null).TransformPoint(default);
			}

#if __ANDROID__
			// for android, the above line returns the absolute coordinates of anchor on the screen
			// because the parent view of this PopupPanel is a PopupWindow and GetLocationInWindow will be (0,0)
			// therefore, we need to make the relative adjustment
			if (this.NativeVisualParent is AView view)
			{
				var windowLocation = Point.From(view.GetLocationInWindow);
				var screenLocation = Point.From(view.GetLocationOnScreen);

				if (windowLocation == default)
				{
					anchorLocation -= ViewHelper.PhysicalToLogicalPixels(screenLocation);
				}
			}
#endif

			var finalFrame = new Rect(
				anchorLocation.X + (float)Popup.HorizontalOffset,
				anchorLocation.Y + (float)Popup.VerticalOffset,
				size.Width,
				size.Height);

			// TODO: For now, the layouting logic for managed DatePickerFlyout or TimePickerFlyout does not correctly work
			// against the placement target approach.
			var isFlyoutManagedDatePicker =
				(Popup.AssociatedFlyout is DatePickerFlyout || Popup.AssociatedFlyout is TimePickerFlyout)
#if __ANDROID__ || __IOS__
				&& (Popup.AssociatedFlyout is not NativeDatePickerFlyout && Popup.AssociatedFlyout is not NativeTimePickerFlyout)
#endif
				;

			if ((!isFlyoutManagedDatePicker
#if __ANDROID__ || __APPLE_UIKIT__
				 || NativeAnchor is not null
#endif
				 || !MathHelpers.DoesRectContainRect(GetVisibleBounds(), finalFrame) // if the finalFrame spills out of the window, always use PlacementArrangeOverride
				) && Popup.PlacementTarget is not null
			   )
			{
				return PlacementArrangeOverride(Popup, finalSize);
			}

			ArrangeElement(child, finalFrame);

			// Temporary workaround to avoid layout cycle on iOS. This block was added specifically for a bug on Android's
			// MenuFlyout so, for now, we restrict to to only Android
			// This can be re-evaluated and removed after https://github.com/unoplatform/uno/pull/18261 merges
#if !__APPLE_UIKIT__
			var updatedFinalFrame = new Rect(
				anchorLocation.X + (float)Popup.HorizontalOffset,
				anchorLocation.Y + (float)Popup.VerticalOffset,
				size.Width,
				size.Height);

			if (updatedFinalFrame != finalFrame)
			{
				// Workraround:
				// The HorizontalOffset is updated to the correct value in CascadingMenuHelper.OnPresenterSizeChanged
				// This update appears to be happening *during* ArrangeElement which was already passed wrong finalFrame.
				// We re-arrange with the new updated finalFrame.
				// Note: This might be a lifecycle issue, so this workaround needs to be revised in the future and deleted if possible.
				// See MenuFlyoutSubItem_Placement sample.
				ArrangeElement(child, updatedFinalFrame);
			}
#endif

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Arranged PopupPanel #={GetHashCode()} DC={Popup.DataContext} child={child} popupLocation={anchorLocation} offset={Popup.HorizontalOffset},{Popup.VerticalOffset} finalSize={finalSize} childFrame={finalFrame}");
			}
		}
		else
		{
			// Defer to the popup owner the responsibility to place the popup (e.g. ComboBox)

			Rect visibleBounds = XamlRoot?.VisualTree.VisibleBounds ?? default;

			visibleBounds.Width = Math.Min(finalSize.Width, visibleBounds.Width);
			visibleBounds.Height = Math.Min(finalSize.Height, visibleBounds.Height);

			Popup.CustomLayouter.Arrange(
				finalSize,
				visibleBounds,
				_lastMeasuredSize
			);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Arranged PopupPanel #={GetHashCode()} **using custom layouter** DC={Popup.DataContext} child={child} finalSize={finalSize}");
			}
		}

		return finalSize;
	}

	private protected override void OnLoaded()
	{
		base.OnLoaded();

		this.XamlRoot.Changed += XamlRootChanged;
		UpdatePlacementTargetTracking();
	}

	private protected override void OnUnloaded()
	{
		base.OnUnloaded();

		if (XamlRoot is { } xamlRoot)
		{
			xamlRoot.Changed -= XamlRootChanged;
		}

		UnsubscribeFromPlacementTargetViewportChanges();
	}

	private void UpdatePlacementTargetTracking()
	{
		var newTarget = Popup?.PlacementTarget ?? (Popup?.GetTemplatedParent() as FrameworkElement);

		if (_trackedPlacementTarget != newTarget && IsLoaded)
		{
			UnsubscribeFromPlacementTargetViewportChanges();

			_trackedPlacementTarget = newTarget;

			if (_trackedPlacementTarget != null)
			{
				_trackedPlacementTarget.EffectiveViewportChanged += OnPlacementTargetEffectiveViewportChanged;
			}
		}
	}

	private void UnsubscribeFromPlacementTargetViewportChanges()
	{
		if (_trackedPlacementTarget != null)
		{
			_trackedPlacementTarget.EffectiveViewportChanged -= OnPlacementTargetEffectiveViewportChanged;
			_trackedPlacementTarget = null;
		}
	}

	private void OnPlacementTargetEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
	{
		// When the placement target's viewport changes (e.g., due to scrolling),
		// we need to re-arrange the popup to follow its anchor
		InvalidateArrange();
	}

	// TODO: pointer handling should really go on PopupRoot. For now it's easier to put here because PopupRoot doesn't track open popups, and also we
	// need to support native popups on Android that don't use PopupRoot.
	private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
	{
		// Make sure we are the original source.  We do not want to handle PointerPressed on the Popup itself.
		if (args.OriginalSource == this && Popup is { } popup)
		{
			// CommandBars in WinUI don't rely on IsLightDismissEnabled, instead there's IsSticky.
			// Instead of handling it here, CommandBar should handle it using an LTE (look at the comment
			// in AppBar.SetupOverlayState) but we don't have the logic implemented in Uno yet, so we
			// rely on this workaround to close CommandBar's popup.
			if (popup.GetTemplatedParent() is CommandBar cb)
			{
				cb.TryDismissInlineAppBarInternal();
			}
			// The check is here because ContentDialogPopupPanel returns true for IsViewHit() even though light-dismiss is always
			// disabled for ContentDialogs.
			else if (popup.IsLightDismissEnabled)
			{
				OnPointerPressedDismissed(args);
				ClosePopup(popup);
			}
			args.Handled = true;
		}
	}

	private protected virtual void OnPointerPressedDismissed(PointerRoutedEventArgs args) { }

	private static void ClosePopup(Popup popup)
	{
		// Give the popup an opportunity to cancel closing.
		var cancel = false;
		popup.OnClosing(ref cancel);
		if (!cancel)
		{
			popup.IsOpen = false;
		}
	}

	internal override bool IsViewHit()
	{
		// CommandBars in WinUI don't rely on IsLightDismissEnabled, instead there's IsSticky.
		// Instead of handling it here, CommandBar should handle it using an LTE (look at the comment
		// in AppBar.SetupOverlayState) but we don't have the logic implemented in Uno yet, so we
		// rely on this workaround to close CommandBar's popup.
		if (Popup?.GetTemplatedParent() is CommandBar { IsSticky: false } and not CommandBarFlyoutCommandBar)
		{
			return true;
		}

		return Popup?.IsLightDismissEnabled ?? false;
	}
}
