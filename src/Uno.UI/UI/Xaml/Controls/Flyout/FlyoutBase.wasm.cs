namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class FlyoutBase
	{
		partial void InitializePartial()
		{
			_popup.PopupPanel = new FlyoutBasePopupPanel(this)
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
