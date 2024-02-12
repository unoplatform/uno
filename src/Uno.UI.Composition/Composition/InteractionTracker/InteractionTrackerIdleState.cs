#nullable enable

using System.Numerics;
using Windows.Foundation;

namespace Microsoft.UI.Composition.Interactions;

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

	internal override void TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond)
	{
		// State changes to inertia and inertia modifiers are evaluated with requested velocity as initial velocity
		// TODO: inertia modifiers not yet implemented.
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, velocityInPixelsPerSecond, _interactionTracker.CurrentRequestId));
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
