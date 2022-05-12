using Windows.UI.Composition.Interactions;
using Windows.UI.Xaml;

namespace Microsoft.UI.Private.Controls;

internal interface IAdapterAnimationHandler
{
	void InteractionTrackerAnimation(UIElement refreshVisualizer, UIElement infoProvider, InteractionTracker interactionTracker);

	void RefreshRequestedAnimation(UIElement refreshVisualizer, UIElement infoProvider, double executionRatio);

	void RefreshCompletedAnimation(UIElement refreshVisualizer, UIElement infoProvider);
}
