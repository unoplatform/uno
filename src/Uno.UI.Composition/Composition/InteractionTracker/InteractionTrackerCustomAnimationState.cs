#nullable enable

using System.Numerics;
using Windows.Foundation;

namespace Windows.UI.Composition.Interactions;

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

	internal override void ReceivePointerWheel(int delta, bool isHorizontal)
	{
	}

	internal override void TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, int requestId)
	{
		// TODO: Stop current animation. Currently, the TryUpdate[Position|Scale]WithAnimation methods are not implemented.

		// State changes to inertia with inertia modifiers evaluated using requested velocity as initial velocity.
		// TODO: inertia modifiers not yet implemented.
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, velocityInPixelsPerSecond, requestId, isFromPointerWheel: false));
	}

	internal override void TryUpdatePosition(Vector3 value, InteractionTrackerClampingOption option, int requestId)
	{
		if (option == InteractionTrackerClampingOption.Auto)
		{
			value = Vector3.Clamp(value, _interactionTracker.MinPosition, _interactionTracker.MaxPosition);
		}

		_interactionTracker.SetPosition(value, requestId);
		_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId));
	}
}
