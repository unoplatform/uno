using Microsoft.UI.Private.Controls;
using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a container control that provides a RefreshVisualizer and pull-to-refresh functionality for scrollable content.
/// </summary>
public partial class RefreshContainer : ContentControl
{
	private IRefreshInfoProviderAdapter GetDefaultRefreshInfoProvider()
	{
#if !__ANDROID__ && !__IOS__
		return new StubIRefreshInfoProviderAdapter();
#else
		return new NativeIRefreshInfoProviderAdapter(this);
#endif
	}
}
