#nullable enable

namespace Windows.UI.Composition.Interactions;

public partial interface IInteractionTrackerOwner
{
	void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args);

	void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args);

	void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args);

	void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args);

	void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args);

	void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args);
}
