#nullable enable

using System.Numerics;
using Windows.Foundation;

namespace Microsoft.UI.Composition.Interactions;

internal sealed class InteractionTrackerCustomAnimationState : InteractionTrackerState
{
	public InteractionTrackerCustomAnimationState(InteractionTracker interactionTracker) : base(interactionTracker)
	{
	}

	protected override void EnterState(IInteractionTrackerOwner? owner)
	{
		// TODO: Args.
		owner?.CustomAnimationStateEntered(_interactionTracker, new());
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
		// TODO: Stop current animation

		// State changes to inertia with inertia modifiers evaluated using requested velocity as initial velocity.
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
		_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker));
	}
}
