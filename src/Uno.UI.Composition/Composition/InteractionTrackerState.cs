#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;

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
	internal abstract void StartUserManipulation();
	internal abstract void CompleteUserManipulation();
	internal abstract void ReceiveManipulationDelta(Point translationDelta);
	internal abstract int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond);
}
