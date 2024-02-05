#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

internal sealed class InteractionTrackerInertiaState : InteractionTrackerState
{
	public InteractionTrackerInertiaState(InteractionTracker interactionTracker) : base(interactionTracker)
	{
	}

	protected override void EnterState(IInteractionTrackerOwner owner)
	{
		owner.InertiaStateEntered(_interactionTracker, null);
	}

	internal override void StartUserManipulation()
	{
		_interactionTracker.ChangeState(new InteractionTrackerInteractingState(_interactionTracker));
	}

	internal override void CompleteUserManipulation()
	{
	}

	internal override void ReceiveManipulationDelta()
	{
	}

	internal override int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond)
	{
		// Inertia is restarted (state re-enters inertia) and inertia modifiers are evaluated with requested velocity added to current velocity
		// TODO: Add velocities.
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker));

		// TODO: Return RequestId
		return 0;
	}
}
