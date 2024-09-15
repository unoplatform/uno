#if !__CROSSRUNTIME__
namespace Microsoft.UI.Xaml;

public sealed partial class XamlRoot
{
	internal void InvalidateMeasure()
	{
		global::Uno.UI.Xaml.Core.CoreServices.RequestAdditionalFrame();
	}

	internal void InvalidateArrange()
	{
		global::Uno.UI.Xaml.Core.CoreServices.RequestAdditionalFrame();
	}
}
#endif
