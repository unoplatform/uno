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

		protected override Point? PositionInAnchorControl => null;
	}
}
