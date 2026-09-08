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

		if (Flyout.OverlayInputPassThroughElement is not UIElement passThroughElement)
		{
			return;
		}

		var point = args.GetCurrentPoint(null);
		var hitTestIgnoringThis = VisualTreeHelper.DefaultGetTestability.Except(XamlRoot?.VisualTree.PopupRoot as UIElement ?? this);
		var (elementHitUnderOverlay, _) = VisualTreeHelper.HitTest(point.Position, XamlRoot?.VisualTree.RootElement, hitTestIgnoringThis);

		if (elementHitUnderOverlay is null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
				this.Log().Debug($"{this.GetDebugName()} PassThroughElement ({passThroughElement.GetDebugName()}) ignored as hit-tested element is null.");

			return;
		}

		// MUX Reference PointerInputProcessor.cpp:1100-1120, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75.
		// If the app specified that the overlay input pass-through element is the root visual,
		// then we want to always allow the event to pass through the light-dismiss layer.
		// This is needed because setting the root visual as the pass-through element implies that
		// you want all input to go through the light-dismiss layer, but since popups don't have
		// the root visual as their ancestor in the visual tree, the test for ancestry will fail.
		if (!ReferenceEquals(passThroughElement, XamlRoot?.Content)
			&& !ReferenceEquals(passThroughElement, elementHitUnderOverlay)
			&& !VisualTreeHelper.EnumerateAncestors(elementHitUnderOverlay).Contains(passThroughElement))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
				this.Log().Debug(
					$"{this.GetDebugName()} PassThroughElement ({passThroughElement.GetDebugName()}) ignored as hit-tested element ({elementHitUnderOverlay.GetDebugIdentifier()})"
					+ $" is not a child of the PassThroughElement ({VisualTreeHelper.EnumerateAncestors(elementHitUnderOverlay).Reverse().Select(elt => elt.GetDebugIdentifier()).JoinBy(">") ?? "--null--"}).");

			// The element found by the HitTest is not a child of the pass-through element.
			return;
		}

		XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.ReRoute(args, from: this, to: elementHitUnderOverlay);
	}
}
