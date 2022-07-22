#if __ANDROID__ || __IOS__
using Microsoft.UI.Private.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal class NativeIRefreshInfoProviderAdapter : IRefreshInfoProviderAdapter
{
	public IRefreshInfoProvider AdaptFromTree(UIElement root, Size visualizerSize)
	{
		return new NativeIRefreshInfoProvider
	}

	public void SetAnimations(UIElement refreshVisualizerAnimatableContainer)
	{
	}
}
#endif
