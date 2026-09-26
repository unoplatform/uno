#nullable enable

using System;
using System.Numerics;
using Uno.UI.Dispatching;
using Windows.Foundation;

namespace Microsoft.UI.Composition.Interactions;

internal abstract class InteractionTrackerState : IDisposable
{
	private protected InteractionTracker _interactionTracker;
	private protected bool _disposed;

	public InteractionTrackerState(InteractionTracker interactionTracker)
	{
		_interactionTracker = interactionTracker;
		NativeDispatcher.Main.Enqueue(() =>
		{
			HasEntered = true;
			EnterState(interactionTracker.Owner);
		});
	}

	/// <summary>Whether the owner has been told about this state, which it must be before any motion it drives.</summary>
	internal bool HasEntered { get; private set; }

	protected abstract void EnterState(IInteractionTrackerOwner? owner);

	/// <summary>
	/// Called synchronously once this became the tracker's state, unlike <see cref="EnterState"/> which only
	/// raises the owner's notification and is enqueued.
	/// </summary>
	internal virtual void OnActivated() { }

	internal virtual void InterruptInertia() { }
	internal abstract void StartUserManipulation();
	internal abstract void CompleteUserManipulation(Vector3 linearVelocity);
	internal abstract void ReceiveManipulationDelta(Point translationDelta);
	internal abstract void ReceiveInertiaStarting(Point linearVelocity);
	internal abstract void ReceivePointerWheel(double delta, bool isHorizontal);
	internal abstract void TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, int requestId);
	internal abstract void TryUpdatePosition(Vector3 value, InteractionTrackerClampingOption option, int requestId);
	internal abstract void TryUpdateScale(float value, Vector3 centerPoint, int requestId);

	// Idle, Inertia and CustomAnimation all (re-)enter CustomAnimation.
	internal virtual void TryUpdatePositionWithAnimation(CompositionAnimation animation, int requestId)
		=> _interactionTracker.ChangeState(InteractionTrackerCustomAnimationState.ForPosition(_interactionTracker, animation, requestId));

	internal virtual void TryUpdateScaleWithAnimation(CompositionAnimation animation, Vector3 centerPoint, int requestId)
		=> _interactionTracker.ChangeState(InteractionTrackerCustomAnimationState.ForScale(_interactionTracker, animation, centerPoint, requestId));
	public virtual void Dispose() => _disposed = true;
}
