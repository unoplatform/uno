using Windows.Foundation;
using Windows.UI.Xaml;

namespace Microsoft.UI.Private.Controls
{
	internal interface IRefreshInfoProviderAdapter
    {
		IRefreshInfoProvider AdaptFromTree(UIElement root, Size visualizerSize);

		void SetAnimations(UIElement refreshVisualizerAnimatableContainer);
	}
}
