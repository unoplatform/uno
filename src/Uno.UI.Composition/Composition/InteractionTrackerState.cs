#nullable enable

using System;
using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

internal abstract class InteractionTrackerState
{
	private protected InteractionTracker _interactionTracker;

	public InteractionTrackerState(InteractionTracker interactionTracker)
	{
		_interactionTracker = interactionTracker;
		if (interactionTracker.Owner is { } owner)
		{
			// TODO: Should be enqueued to dispatcher or called directly?
			EnterState(owner);
		}
	}

	protected abstract void EnterState(IInteractionTrackerOwner owner);
	internal abstract void ReceiveUserManipulation();
	internal abstract void CompleteUserManipulation();
	internal abstract int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, bool isInertiaFromImpulse);
}
