#nullable enable

using System.Numerics;
using Windows.Foundation;

namespace Windows.UI.Composition.Interactions;

internal sealed class InteractionTrackerIdleState : InteractionTrackerState
{
	private readonly bool _isInitialIdleState;
	private readonly int _requestId;

	public InteractionTrackerIdleState(InteractionTracker interactionTracker, int requestId, bool isInitialIdleState = false) : base(interactionTracker)
	{
		_requestId = requestId;
		_isInitialIdleState = isInitialIdleState;
	}

	protected override void EnterState(IInteractionTrackerOwner? owner)
	{
		if (!_isInitialIdleState)
		{
			owner?.IdleStateEntered(_interactionTracker, new InteractionTrackerIdleStateEnteredArgs(requestId: _requestId, isFromBinding: false));
		}
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
		// Constant velocity for 250ms
		var velocityValue = delta / 0.25f;
		Vector3 velocity = isHorizontal ? new Vector3(velocityValue, 0, 0) : new Vector3(0, velocityValue, 0);
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, velocity, requestId: 0, isFromPointerWheel: true));
	}

	internal override void TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, int requestId)
	{
		// State changes to inertia and inertia modifiers are evaluated with requested velocity as initial velocity
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
	}
}
