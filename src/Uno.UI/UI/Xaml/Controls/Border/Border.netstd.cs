namespace Microsoft.UI.Xaml.Controls;

partial class Border
{
	partial void OnChildChangedPartial(UIElement previousValue, UIElement newValue)
	{
		if (previousValue != null)
		{
			RemoveChild(previousValue);
		}

		AddChild(newValue);
	}

	bool ICustomClippingElement.AllowClippingToLayoutSlot => !(Child is UIElement ue) || ue.RenderTransform == null;

	bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadius != CornerRadius.None;
}
