using Microsoft.UI.Private.Controls;
using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a container control that provides a RefreshVisualizer and pull-to-refresh functionality for scrollable content.
/// </summary>
public partial class RefreshContainer : ContentControl
{
	private void SetDefaultRefreshInfoProviderAdapter()
	{
		if (m_refreshInfoProviderAdapter is not null)
		{
			m_refreshInfoProviderAdapter.Dispose();
		}
		
#if !__ANDROID__ && !__IOS__
		m_refreshInfoProviderAdapter =  new StubIRefreshInfoProviderAdapter();
#else
		m_refreshInfoProviderAdapter = new NativeRefreshInfoProviderAdapter(this);
#endif
	}
}
