using Microsoft.UI.Private.Controls;
using Uno.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a container control that provides a RefreshVisualizer and pull-to-refresh functionality for scrollable content.
/// </summary>
public partial class RefreshContainer : ContentControl
{
	private void SetDefaultRefreshInfoProviderAdapter()
	{
		//if (m_refreshInfoProviderAdapter is not null)
		//{
		//	// TODO Uno specific: We currently don't need to switch refresh info provider adapter.
		//	return;
		//}

		m_refreshInfoProviderAdapter = new ScrollViewerIRefreshInfoProviderAdapter(PullDirection, null);
	}

	private void SetDefaultRefreshVisualizer()
	{
		Visualizer = new RefreshVisualizer();
	}
}
