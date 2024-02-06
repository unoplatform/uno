namespace Microsoft.UI.Xaml.Controls;

public partial class ContentPresenter : FrameworkElement
{
	partial void RegisterContentTemplateRoot() => AddChild(ContentTemplateRoot);

	partial void UnregisterContentTemplateRoot() => RemoveChild(ContentTemplateRoot);

	bool ICustomClippingElement.AllowClippingToLayoutSlot => true;

	bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadius != CornerRadius.None;
}
