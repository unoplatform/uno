#if !__CROSSRUNTIME__
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml;

public sealed partial class XamlRoot
{
	internal void InvalidateMeasure()
	{
		CoreServices.RequestAdditionalFrame();
	}

	internal void InvalidateArrange()
	{
		CoreServices.RequestAdditionalFrame();
	}
}
#endif
