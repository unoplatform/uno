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
		owner.InteractingStateEntered(_interactionTracker, null);
	}

	internal override void StartUserManipulation()
	{
		// This probably shouldn't happen.
		// We ignore.
	}

	internal override void CompleteUserManipulation()
	{
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker));
	}

	internal override void ReceiveManipulationDelta(Point translationDelta)
	{
		_interactionTracker.SetPosition(_interactionTracker.Position + new Vector3((float)translationDelta.X, (float)translationDelta.Y, 0), isFromUserManipulation: true);
		// Set interaction tracker position.
	}

	internal override int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond)
	{
		// Request ignored

		// TODO: Return RequestId
		return 0;
	}
}
