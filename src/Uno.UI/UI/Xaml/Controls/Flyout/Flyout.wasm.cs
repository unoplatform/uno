namespace Windows.UI.Xaml.Controls
{
	public partial class Flyout
	{
		partial void InitializePartial()
		{
			_popup.PopupPanel = new FlyoutPopupPanel(this)
			{
				Visibility = Visibility.Collapsed,
				Background = SolidColorBrushHelper.Transparent,
			};
		}

		partial void SetPopupPositionPartial(UIElement placementTarget)
		{
			_popup.Anchor = placementTarget;
		}
	}
}
