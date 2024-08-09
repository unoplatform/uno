#if !__UWP__
using System;
using System.Linq;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml.Controls;

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
		Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
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

		if (!Flyout.GetAllParents(includeCurrent: false).Contains(passThroughElement))
		{
			// The element must be a parent of the Flyout (not 'this') to be able to receive the pointer events.

			if (this.Log().IsEnabled(LogLevel.Debug))
				this.Log().Debug(
					$"{this.GetDebugName()} PassThroughElement ignored as element ({Flyout.OverlayInputPassThroughElement?.GetAllParents().Reverse().Select(elt => elt.GetDebugName()).JoinBy(">") ?? "--null--"})"
					+ $" is not a parent of the Flyout ({Flyout.GetAllParents().Select(elt => elt.GetDebugName()).Reverse().JoinBy(">")}).");

			return;
		}

		var point = args.GetCurrentPoint(null);
		var hitTestIgnoringThis = VisualTreeHelper.DefaultGetTestability.Except(XamlRoot?.VisualTree.PopupRoot as UIElement ?? this);
		var (elementHitUnderOverlay, _) = VisualTreeHelper.HitTest(point.Position, passThroughElement.XamlRoot, hitTestIgnoringThis);

		if (elementHitUnderOverlay is null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
				this.Log().Debug($"{this.GetDebugName()} PassThroughElement ({passThroughElement.GetDebugName()}) ignored as hit-tested element is null.");

			return;
		}

		if (!VisualTreeHelper.EnumerateAncestors(elementHitUnderOverlay).Contains(passThroughElement))
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
#endif
