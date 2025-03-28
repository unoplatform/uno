#if __IOS__ || __ANDROID__
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class RefreshContainer : ContentControl
{
	private void OnNativeRefreshingChanged()
	{
		if (Visualizer is { } visualizer && visualizer.State != RefreshVisualizerState.Refreshing)
		{
			visualizer.RequestRefresh();
		}
	}
}
#endif
