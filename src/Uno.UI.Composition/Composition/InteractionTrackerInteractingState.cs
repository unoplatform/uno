#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

internal sealed class InteractionTrackerInteractingState : InteractionTrackerState
{
	public InteractionTrackerInteractingState(InteractionTracker interactionTracker) : base(interactionTracker)
	{
	}

	internal override int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, bool isInertiaFromImpulse)
	{
		// Request ignored

		// TODO: Return RequestId
		return 0;
	}
}
