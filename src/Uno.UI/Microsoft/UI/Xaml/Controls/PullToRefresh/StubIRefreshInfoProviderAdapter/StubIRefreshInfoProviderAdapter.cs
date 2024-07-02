using Microsoft.UI.Private.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal class StubIRefreshInfoProviderAdapter : IRefreshInfoProviderAdapter
{
	private readonly StubIRefreshInfoProvider _stubRefreshInfoProvider = new StubIRefreshInfoProvider();

	public IRefreshInfoProvider AdaptFromTree(UIElement root, Size visualizerSize) => _stubRefreshInfoProvider;

	public void SetAnimations(UIElement refreshVisualizerAnimatableContainer)
	{
		refreshVisualizerAnimatableContainer.Visibility = Visibility.Collapsed;
	}

	public void Dispose() { }
}
