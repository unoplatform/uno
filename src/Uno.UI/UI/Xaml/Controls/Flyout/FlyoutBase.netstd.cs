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

		partial void SetPopupPositionPartial(UIElement placementTarget, Point? positionInTarget)
		{
			_popup.Anchor = placementTarget;

			if (positionInTarget is Point position)
			{
				_popup.HorizontalOffset = position.X;
				_popup.VerticalOffset = position.Y;
			}
		}
	}
}
