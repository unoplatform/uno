using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// PopupPanel implementation for <see cref="FlyoutBase"/>.
/// </summary>
/// <remarks>
/// This panel is *NOT* used by types derived from <see cref="PickerFlyoutBase"/>. Pickers use a plain
/// <see cref="PopupPanel"/> (see <see cref="PickerFlyoutBase.InitializePopupPanel()"/>).
/// </remarks>
internal partial class FlyoutBasePopupPanel : PopupPanel
{
	private readonly FlyoutBase _flyout;

	public FlyoutBasePopupPanel(FlyoutBase flyout) : base(flyout._popup)
	{
		_flyout = flyout;
		_flyout._popup.AssociatedFlyout = flyout;
		// Required for the dismiss handling
		// This should however be customized depending of the Popup.DismissMode
		Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
	}

	protected override bool FullPlacementRequested => _flyout.EffectivePlacement == FlyoutPlacementMode.Full;

	internal override FlyoutBase Flyout => _flyout;

	protected override int PopupPlacementTargetMargin => 5;

	private protected override void OnPointerPressedDismissed(PointerRoutedEventArgs args)
	{
		if (this.Log().IsEnabled(LogLevel.Debug)) this.Log().Debug($"{this.GetDebugName()} Dismissing flyout (OverlayInputPassThroughElement:{Flyout.OverlayInputPassThroughElement.GetDebugIdentifier()}).");

		if (Flyout.OverlayInputPassThroughElement is not UIElement passThroughElement
			|| XamlRoot?.VisualTree is not { } visualTree)
		{
			return;
		}

		// What is under this popup: the main tree and the popups below this one, but not this one nor those opened
		// above it. A flyout opened from a ContentDialog or another flyout then passes the press to that popup's
		// content, not to the window behind it.
		// Uno-specific: every light-dismiss popup's panel covers the window, so a press outside both this flyout and a
		// Standard flyout under it lands on that flyout's panel, which eats it as a Standard flyout eats its dismissing
		// press. In WinUI the popup root is the only overlay, and the press reaches the page.
		var hitTestUnderThis = VisualTreeHelper.DefaultGetTestability.Except(this);
		if (visualTree.PopupRoot is { } popupRoot)
		{
			var isAbove = false;
			foreach (var popupPanel in popupRoot.Children)
			{
				if (isAbove)
				{
					hitTestUnderThis = hitTestUnderThis.Except(popupPanel);
				}

				isAbove |= popupPanel == this;
			}
		}

		var point = args.GetCurrentPoint(null);
		var (elementHitUnderOverlay, _) = VisualTreeHelper.HitTest(point.Position, visualTree.RootElement, hitTestUnderThis);

		if (elementHitUnderOverlay is null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
				this.Log().Debug($"{this.GetDebugName()} PassThroughElement ({passThroughElement.GetDebugName()}) ignored as hit-tested element is null.");

			return;
		}

		// MUX Reference: PointerInputProcessor.cpp HitTestWithLightDismissAwareness. The root visual as pass-through element
		// lets everything through, popups included, as they aren't under it. Uno's XamlRoot.Content is the content inside
		// the window chrome, so it counts as the root too.
		var passesEverything = passThroughElement == visualTree.PublicRootVisual || passThroughElement == XamlRoot.Content;

		if (!passesEverything
			&& elementHitUnderOverlay != passThroughElement
			&& !VisualTreeHelper.EnumerateAncestors(elementHitUnderOverlay).Contains(passThroughElement))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
				this.Log().Debug(
					$"{this.GetDebugName()} PassThroughElement ({passThroughElement.GetDebugName()}) ignored as hit-tested element ({elementHitUnderOverlay.GetDebugIdentifier()})"
					+ $" is not in the PassThroughElement ({VisualTreeHelper.EnumerateAncestors(elementHitUnderOverlay).Reverse().Select(elt => elt.GetDebugIdentifier()).JoinBy(">") ?? "--null--"}).");

			return;
		}

		Flyout.OnDismissingPressPassedThrough(args);
		visualTree.ContentRoot.InputManager.Pointers.ReRoute(args, from: this, to: elementHitUnderOverlay);
	}
}
