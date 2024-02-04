#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

internal sealed class InteractionTrackerInteractingState : InteractionTrackerState
{
	public InteractionTrackerInteractingState(InteractionTracker interactionTracker) : base(interactionTracker)
	{
	}

	protected override void EnterState(IInteractionTrackerOwner owner)
	{
		owner.InteractingStateEntered(_interactionTracker, null);
	}

	internal override void ReceiveUserManipulation()
	{
		// This probably shouldn't happen.
		// We ignore.
	}

	internal override void CompleteUserManipulation()
	{
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker));
	}

	internal override int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, bool isInertiaFromImpulse)
	{
		// Request ignored

		// TODO: Return RequestId
		return 0;
	}
}
