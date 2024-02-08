#nullable enable

using System;
using System.Numerics;
using Uno.UI.Dispatching;
using Windows.Foundation;

namespace Microsoft.UI.Composition.Interactions;

internal abstract class InteractionTrackerState : IDisposable
{
	private protected InteractionTracker _interactionTracker;

	public InteractionTrackerState(InteractionTracker interactionTracker)
	{
		_interactionTracker = interactionTracker;
		NativeDispatcher.Main.Enqueue(() => EnterState(interactionTracker.Owner));
	}

	protected abstract void EnterState(IInteractionTrackerOwner? owner);
	internal abstract void StartUserManipulation();
	internal abstract void CompleteUserManipulation(Vector3 linearVelocity);
	internal abstract void ReceiveManipulationDelta(Point translationDelta);
	internal abstract void ReceiveInertiaStarting(Point linearVelocity);
	internal abstract void TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond);
	internal abstract void TryUpdatePosition(Vector3 value, InteractionTrackerClampingOption option);
	public virtual void Dispose() { }
}
