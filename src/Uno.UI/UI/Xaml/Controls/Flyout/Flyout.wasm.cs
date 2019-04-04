namespace Windows.UI.Xaml.Controls
{
	public partial class Flyout
	{
		partial void SetPopupPositionPartial(UIElement placementTarget)
		{
			_popup.Anchor = placementTarget;
		}
	}
}
