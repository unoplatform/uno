#if !__UWP__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Controls.Primitives;
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
	}
}
#endif
