#nullable enable

using System.Numerics;
using Windows.Foundation;

namespace Microsoft.UI.Composition.Interactions;

internal sealed class InteractionTrackerInertiaState : InteractionTrackerState
{
	private readonly InteractionTrackerInertiaHandler _handler;

	public InteractionTrackerInertiaState(InteractionTracker interactionTracker, Vector3 translationVelocities) : base(interactionTracker)
	{
		_handler = new InteractionTrackerInertiaHandler(interactionTracker, translationVelocities);
	}

	protected override void EnterState(IInteractionTrackerOwner owner)
	{
		owner.InertiaStateEntered(_interactionTracker, new InteractionTrackerInertiaStateEnteredArgs());
		_handler.Start();
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

	internal override int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond)
	{
		// Inertia is restarted (state re-enters inertia) and inertia modifiers are evaluated with requested velocity added to current velocity
		// TODO: Add velocities.
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, velocityInPixelsPerSecond));

		// TODO: Return RequestId
		return 0;
	}
}
