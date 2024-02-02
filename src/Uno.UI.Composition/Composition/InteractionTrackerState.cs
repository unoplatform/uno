#nullable enable

namespace Microsoft.UI.Composition.Interactions;

internal abstract class InteractionTrackerState
{
	private InteractionTracker _interactionTracker;

	public InteractionTrackerState(InteractionTracker interactionTracker)
	{
		_interactionTracker = interactionTracker;
	}
}
