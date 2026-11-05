using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.DataBinding;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Input;
using Uno.Helpers;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal partial class PopupPanel : Panel
{
	public Popup Popup { get; }

	public PopupPanel(Popup popup)
	{
		Popup = popup ?? throw new ArgumentNullException(nameof(popup));
		Visibility = Visibility.Collapsed;
		PointerPressed += OnPointerPressed;
	}

	protected override AutomationPeer OnCreateAutomationPeer()
		=> Popup?.GetAutomationPeer() ?? base.OnCreateAutomationPeer();

	protected Size _lastMeasuredSize;

	// MUX Reference CPopupRoot::MeasureOverride, core\core\elements\Popup.cpp, tag winui3/release/2.5.1, commit ba3a8d59e
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

		_lastMeasuredSize = MeasureElement(child, isOpen ? new Size(double.PositiveInfinity, double.PositiveInfinity) : availableSize);

		// Note that we return the availableSize and not the _lastMeasuredSize. This is because this
		// Panel always take the whole screen for the dismiss layer, but it's content will not.
		return availableSize;
	}

	// MUX Reference CPopupRoot::ArrangeOverride, core\core\elements\Popup.cpp, tag winui3/release/2.5.1, commit ba3a8d59e
	protected override Size ArrangeOverride(Size finalSize)
	{
		var isOpen = Visibility != Visibility.Collapsed;
		if (!isOpen)
		{
			finalSize = default;
		}

		var child = this.GetChildren().FirstOrDefault();
		if (child == null)
		{
			return finalSize;
		}

		if (!isOpen)
		{
			ArrangeElement(child, default);
			return finalSize;
		}

		var popup = Popup;
		var x = popup.HorizontalOffset + popup.m_hAdjustment + popup.m_calculatedHOffset;
		var y = popup.VerticalOffset + popup.m_vAdjustment + popup.m_calculatedVOffset;

		// TODO Uno: The child is not a visual child of the popup, so the offsets are applied to the popup's location
		// here. In WinUI the composition tree does it, and it mirrors them under a right-to-left ancestor as well.
		var isParented = popup.GetParent() is not null;
		var popupLocation = isParented ? popup.TransformToVisual(null).TransformPoint(default) : default;
		var isParentedRtl = isParented && popup.FlowDirection == FlowDirection.RightToLeft;

		var left = popupLocation.X + (isParentedRtl ? -x : x);
		var top = popupLocation.Y + y;

		// CUIElement::GetShouldFlipRTL: the right-to-left child of a parentless popup is not flipped in place,
		// and the same goes for the content of a mirrored parent. The offset is the child's right edge.
		var childSize = child.DesiredSize;
		if (isParentedRtl || (!isParented && child is FrameworkElement { FlowDirection: FlowDirection.RightToLeft }))
		{
			left -= childSize.Width;
		}

		ArrangeElement(child, new Rect(left, top, childSize.Width, childSize.Height));

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug($"Arranged PopupPanel #={GetHashCode()} DC={Popup.DataContext} child={child} popupLocation={popupLocation} offset={x},{y} finalSize={finalSize}");
		}

		return finalSize;
	}

	private void XamlRootChanged(object sender, XamlRootChangedEventArgs e)
		=> InvalidateMeasure();

	private protected override void OnLoaded()
	{
		base.OnLoaded();

		this.XamlRoot.Changed += XamlRootChanged;
	}

	private protected override void OnUnloaded()
	{
		base.OnUnloaded();

		if (XamlRoot is { } xamlRoot)
		{
			xamlRoot.Changed -= XamlRootChanged;
		}
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
