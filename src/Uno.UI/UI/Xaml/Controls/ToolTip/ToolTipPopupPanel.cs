using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	internal partial class ToolTipPopupPanel : PopupPanel
	{
		private readonly ToolTip _toolTip;

		internal ToolTipPopupPanel(ToolTip toolTip) : base(toolTip.Popup)
		{
			_toolTip = toolTip;

			Background = null; // No light dismiss for tooltip, dismiss is managed by the cursor location
		}

		protected override FlyoutPlacementMode PopupPlacement
		{
			get
			{
				switch (_toolTip.Placement)
				{
					case PlacementMode.Bottom: return FlyoutPlacementMode.Bottom;
					case PlacementMode.Top: return FlyoutPlacementMode.Top;
					case PlacementMode.Left: return FlyoutPlacementMode.Left;
					case PlacementMode.Right: return FlyoutPlacementMode.Right;
					default: return FlyoutPlacementMode.Top;
				}
			}
		}

		protected override Point? PositionInAnchorControl => null;

		protected override FrameworkElement AnchorControl => _toolTip.Popup.Anchor as FrameworkElement;
	}
}
