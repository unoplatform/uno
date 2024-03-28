#nullable enable

using System;
using System.Numerics;
using Uno.UI.Dispatching;
using Windows.Foundation;

namespace Windows.UI.Composition.Interactions;

internal abstract class InteractionTrackerState : IDisposable
{
	private protected InteractionTracker _interactionTracker;
	private protected bool _disposed;

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
	internal abstract void ReceivePointerWheel(int delta, bool isHorizontal);
	internal abstract void TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, int requestId);
	internal abstract void TryUpdatePosition(Vector3 value, InteractionTrackerClampingOption option, int requestId);
	public virtual void Dispose() => _disposed = true;
}
