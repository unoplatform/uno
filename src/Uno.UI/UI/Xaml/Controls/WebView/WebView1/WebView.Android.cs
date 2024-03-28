namespace Windows.UI.Xaml.Controls;

public partial class WebView : ICustomClippingElement
{
	bool ICustomClippingElement.AllowClippingToLayoutSlot => true;

	// Force clipping, otherwise native WebView may exceed its bounds in some circumstances (eg when Xaml WebView is animated)
	bool ICustomClippingElement.ForceClippingToLayoutSlot => true;
}
