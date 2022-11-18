#if !__UWP__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
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
			Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
		}

		protected override PopupPlacementMode PopupPlacement => _flyout.EffectivePlacement switch
		{
			FlyoutPlacementMode.Top => PopupPlacementMode.Top,
			FlyoutPlacementMode.Bottom => PopupPlacementMode.Bottom,
			FlyoutPlacementMode.Left => PopupPlacementMode.Bottom,
			FlyoutPlacementMode.Right => PopupPlacementMode.Bottom,
			FlyoutPlacementMode.TopEdgeAlignedLeft => PopupPlacementMode.TopEdgeAlignedLeft,
			FlyoutPlacementMode.TopEdgeAlignedRight => PopupPlacementMode.TopEdgeAlignedRight,
			FlyoutPlacementMode.BottomEdgeAlignedLeft => PopupPlacementMode.BottomEdgeAlignedLeft,
			FlyoutPlacementMode.BottomEdgeAlignedRight => PopupPlacementMode.BottomEdgeAlignedRight,
			FlyoutPlacementMode.LeftEdgeAlignedTop => PopupPlacementMode.LeftEdgeAlignedTop,
			FlyoutPlacementMode.LeftEdgeAlignedBottom => PopupPlacementMode.LeftEdgeAlignedBottom,
			FlyoutPlacementMode.RightEdgeAlignedTop => PopupPlacementMode.RightEdgeAlignedTop,
			FlyoutPlacementMode.RightEdgeAlignedBottom => PopupPlacementMode.RightEdgeAlignedBottom,
			FlyoutPlacementMode.Full => PopupPlacementMode.Full,
			_ => PopupPlacementMode.Auto,
		};

		protected override FrameworkElement AnchorControl => _flyout.Target as FrameworkElement;

		protected override Point? PositionInAnchorControl => _flyout.PopupPositionInTarget;

		internal override FlyoutBase Flyout => _flyout;
	}
}
#endif
