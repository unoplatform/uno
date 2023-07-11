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

		private protected override void OnPointerPressed(object sender, PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(sender, args);

			// Make sure we are the original source.  We do not want to handle PointerPressed on the Popup itself.
			if (args.OriginalSource == this)
			{
				if (Flyout.OverlayInputPassThroughElement is UIElement passThroughElement)
				{
					var (elementToBeHit, _) = VisualTreeHelper.SearchDownForTopMostElementAt(
						args.GetCurrentPoint(null).Position,
						passThroughElement.XamlRoot.VisualTree.RootElement,
						VisualTreeHelper.DefaultGetTestability,
						childrenFilter: elements => elements.Where(e => e != this));

					var eventArgs = new PointerRoutedEventArgs(
						new PointerEventArgs(args.GetCurrentPoint(null), args.KeyModifiers),
						args.OriginalSource as UIElement);

					elementToBeHit.OnPointerDown(eventArgs);
				}
			}
		}
	}
}
#endif
