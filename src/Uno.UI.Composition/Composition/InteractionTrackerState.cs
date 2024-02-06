#nullable enable

using System;
using System.Numerics;
using Uno.UI.Dispatching;
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
			NativeDispatcher.Main.Enqueue(() => EnterState(owner));
		}
	}

	protected abstract void EnterState(IInteractionTrackerOwner owner);
	internal abstract void StartUserManipulation();
	internal abstract void CompleteUserManipulation(Vector3 linearVelocity);
	internal abstract void ReceiveManipulationDelta(Point translationDelta);
	internal abstract void ReceiveInertiaStarting(Point linearVelocity);
	internal abstract int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond);
}
