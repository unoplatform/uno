#nullable enable

using System.Numerics;
using Uno.Foundation.Logging;
using Windows.Foundation;

namespace Windows.UI.Composition.Interactions;

internal sealed class InteractionTrackerInteractingState : InteractionTrackerState
{
	public InteractionTrackerInteractingState(InteractionTracker interactionTracker) : base(interactionTracker)
	{
	}

	protected override void EnterState(IInteractionTrackerOwner? owner)
	{
		owner?.InteractingStateEntered(_interactionTracker, new InteractionTrackerInteractingStateEnteredArgs(requestId: 0, isFromBinding: false));
	}

	internal override void StartUserManipulation()
	{
		// This probably shouldn't happen.
		// We ignore.
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error("Unexpected StartUserManipulation while in interacting state");
		}
	}

	internal override void CompleteUserManipulation(Vector3 linearVelocity)
	{
		// Can this happen?
		// Should we always get ReceiveInertiaStarting first?
		// Anyway, it's likely still safe to go to inertia state here as interacting state can only transition to inertia.
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error("Unexpected CompleteUserManipulation while in interacting state");
		}

		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, linearVelocity, requestId: 0, isFromPointerWheel: false));
	}

	internal override void ReceiveManipulationDelta(Point translationDelta)
	{
		_interactionTracker.SetPosition(_interactionTracker.Position + new Vector3((float)translationDelta.X, (float)translationDelta.Y, 0), requestId: 0);
	}

	internal override void ReceiveInertiaStarting(Point linearVelocity)
	{
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, new Vector3((float)linearVelocity.X, (float)linearVelocity.Y, 0), requestId: 0, isFromPointerWheel: false));
	}

	internal override void ReceivePointerWheel(int delta, bool isHorizontal)
	{
	}

	internal override void TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, int requestId)
	{
		_interactionTracker.Owner?.RequestIgnored(_interactionTracker, new InteractionTrackerRequestIgnoredArgs(requestId));
	}

	internal override void TryUpdatePosition(Vector3 value, InteractionTrackerClampingOption option, int requestId)
	{
		_interactionTracker.Owner?.RequestIgnored(_interactionTracker, new InteractionTrackerRequestIgnoredArgs(requestId));
	}
}
