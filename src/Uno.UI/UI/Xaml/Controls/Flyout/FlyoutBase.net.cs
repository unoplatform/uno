using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Extensions;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class FlyoutBase
	{
		partial void InitializePopupPanelPartial()
		{
			_popup.PopupPanel = new FlyoutBasePopupPanel(this)
			{
				Visibility = Visibility.Collapsed,
				Background = SolidColorBrushHelper.Transparent,
			};
		}

		partial void SetPopupPositionPartial(UIElement placementTarget, Point? position)
		{
			_popup.Anchor = placementTarget;
		}

		internal PopupPanel GetPopupPanel() => _popup.PopupPanel;
	}
}
