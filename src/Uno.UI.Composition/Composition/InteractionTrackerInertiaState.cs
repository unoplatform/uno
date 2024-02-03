#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

internal sealed class InteractionTrackerInertiaState : InteractionTrackerState
{
	public InteractionTrackerInertiaState(InteractionTracker interactionTracker) : base(interactionTracker)
	{
	}

	internal override int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, bool isInertiaFromImpulse)
	{
		// Inertia is restarted (state re-enters inertia) and inertia modifiers are evaluated with requested velocity added to current velocity
		// TODO: Add velocities.
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker));

		// TODO: Return RequestId
		return 0;
	}
}
