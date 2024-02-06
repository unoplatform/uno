#nullable enable

using System.Numerics;
using Windows.Foundation;

namespace Microsoft.UI.Composition.Interactions;

internal sealed class InteractionTrackerInteractingState : InteractionTrackerState
{
	public InteractionTrackerInteractingState(InteractionTracker interactionTracker) : base(interactionTracker)
	{
	}

	protected override void EnterState(IInteractionTrackerOwner owner)
	{
		owner.InteractingStateEntered(_interactionTracker, new InteractionTrackerInteractingStateEnteredArgs(requestId: 0, isFromBinding: false));
	}

	internal override void StartUserManipulation()
	{
		// This probably shouldn't happen.
		// We ignore.
	}

	internal override void CompleteUserManipulation(Vector3 linearVelocity)
	{
		// Can this happen?
		// Should we always get ReceiveInertiaStarting first?
		// Anyway, it's likely still safe to go to inertia state here as interacting state can only transition to inertia.
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, linearVelocity));
	}

	internal override void ReceiveManipulationDelta(Point translationDelta)
	{
		_interactionTracker.SetPosition(_interactionTracker.Position + new Vector3((float)translationDelta.X, (float)translationDelta.Y, 0), isFromUserManipulation: true);
		// Set interaction tracker position.
	}

	internal override void ReceiveInertiaStarting(Point linearVelocity)
	{
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, new Vector3((float)linearVelocity.X, (float)linearVelocity.Y, 0)));
	}

	internal override int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond)
	{
		// Request ignored

		// TODO: Return RequestId
		return 0;
	}
}
