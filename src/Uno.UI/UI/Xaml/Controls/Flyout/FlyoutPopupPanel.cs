#if !__UWP__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Controls
{
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
			if (Flyout.OverlayInputPassThroughElement is not UIElement passThroughElement
				|| !VisualTreeHelper.EnumerateAncestors(Flyout).Contains(passThroughElement))
			{
				// The element must be a parent of the Flyout (not 'this') to be able to receive the pointer events.
				return;
			}

			var point = args.GetCurrentPoint(null);
			var hitTestIgnoringThis = VisualTreeHelper.DefaultGetTestability.Except(this);
			var (elementHitUnderOverlay, _) = VisualTreeHelper.HitTest(point.Position, passThroughElement.XamlRoot, hitTestIgnoringThis);

			if (elementHitUnderOverlay is null
				|| !VisualTreeHelper.EnumerateAncestors(elementHitUnderOverlay).Contains(passThroughElement))
			{
				// The element found by the HitTest is not a child of the pass-through element.
				return;
			}

#if UNO_HAS_MANAGED_POINTERS
			XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.ReRoute(args, from: this, to: elementHitUnderOverlay);
#else
			XamlRoot?.VisualTree.RootVisual.ReRoutePointerDownEvent(args, from: this, to: elementHitUnderOverlay);
#endif
		}
	}
}
#endif
