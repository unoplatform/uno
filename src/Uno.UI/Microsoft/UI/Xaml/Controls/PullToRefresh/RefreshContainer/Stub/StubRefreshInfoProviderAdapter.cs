using Microsoft.UI.Private.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal class StubRefreshInfoProviderAdapter : IRefreshInfoProviderAdapter
{
	private readonly StubRefreshInfoProvider _stubRefreshInfoProvider = new StubRefreshInfoProvider();

	public IRefreshInfoProvider AdaptFromTree(UIElement root, Size visualizerSize) => _stubRefreshInfoProvider;

	public void SetAnimations(UIElement refreshVisualizerAnimatableContainer)
	{
		refreshVisualizerAnimatableContainer.Visibility = Visibility.Collapsed;
	}
}
