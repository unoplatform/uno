#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

internal abstract class InteractionTrackerState
{
	private protected InteractionTracker _interactionTracker;

	public InteractionTrackerState(InteractionTracker interactionTracker)
	{
		_interactionTracker = interactionTracker;
	}

	internal abstract int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, bool isInertiaFromImpulse);
}
