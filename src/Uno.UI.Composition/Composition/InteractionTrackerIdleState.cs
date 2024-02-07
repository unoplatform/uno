#nullable enable

using System.Numerics;
using Windows.Foundation;

namespace Microsoft.UI.Composition.Interactions;

internal sealed class InteractionTrackerIdleState : InteractionTrackerState
{
	public InteractionTrackerIdleState(InteractionTracker interactionTracker) : base(interactionTracker)
	{
	}

	protected override void EnterState(IInteractionTrackerOwner? owner)
	{
		owner?.IdleStateEntered(_interactionTracker, new InteractionTrackerIdleStateEnteredArgs(requestId: 0 /*TODO: Request id*/, isFromBinding: false));
	}

	internal override void StartUserManipulation()
	{
		_interactionTracker.ChangeState(new InteractionTrackerInteractingState(_interactionTracker));
	}

	internal override void CompleteUserManipulation(Vector3 linearVelocity)
	{
	}

	internal override void ReceiveManipulationDelta(Point translationDelta)
	{
	}

	internal override void ReceiveInertiaStarting(Point linearVelocity)
	{
	}

	internal override void TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond)
	{
		// State changes to inertia and inertia modifiers are evaluated with requested velocity as initial velocity
		// TODO: Understand more the "inertia modifiers" part.
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, velocityInPixelsPerSecond));
	}

	internal override void TryUpdatePosition(Vector3 value, InteractionTrackerClampingOption option)
	{
		if (option == InteractionTrackerClampingOption.Auto)
		{
			value = Vector3.Clamp(value, _interactionTracker.MinPosition, _interactionTracker.MaxPosition);
		}

		_interactionTracker.SetPosition(value, isFromUserManipulation: true);
	}
}
