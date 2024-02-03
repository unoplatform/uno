#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

internal sealed class InteractionTrackerCustomAnimationState : InteractionTrackerState
{
	public InteractionTrackerCustomAnimationState(InteractionTracker interactionTracker) : base(interactionTracker)
	{
	}

	protected override void EnterState(IInteractionTrackerOwner owner)
	{
		// TODO: Args.
		owner.CustomAnimationStateEntered(_interactionTracker, null);
	}

	internal override int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, bool isInertiaFromImpulse)
	{
		// TODO: Stop current animation

		// State changes to inertia with inertia modifiers evaluated using requested velocity as initial velocity.
		// TODO: Understand more the "inertia modifiers" part.
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker));

		// TODO: Return RequestId
		return 0;
	}
}
