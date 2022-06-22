#nullable enable
#pragma warning disable CS0414
#pragma warning disable CS0169

using Microsoft.UI.Private.Controls;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class RefreshContainer
{
	private Panel? m_root;
	private Panel? m_refreshVisualizerPresenter;
	private RefreshVisualizer? m_refreshVisualizer;
	private Deferral? m_visualizerRefreshCompletedDeferral;
	private RefreshPullDirection m_refreshPullDirection = RefreshPullDirection.TopToBottom;
	private IRefreshInfoProviderAdapter? m_refreshInfoProviderAdapter;
	private readonly SerialDisposable m_refreshVisualizerSizeChangedToken = new SerialDisposable();

	private bool m_hasDefaultRefreshVisualizer = false;
	private bool m_hasDefaultRefreshInfoProviderAdapter = false;
}
