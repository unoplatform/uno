#nullable enable

using System;
using System.Numerics;
using Uno.Foundation.Logging;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition.Interactions;

/// <summary>Drives an inertia motion once per frame, on the UI thread, from the frame's timestamp.</summary>
internal abstract class InteractionTrackerFrameInertiaHandler : IInteractionTrackerInertiaHandler
{
	private ICompositionTarget? _target;
	private EventHandler<long>? _handler;
	private long? _startTimestamp;

	protected InteractionTrackerFrameInertiaHandler(InteractionTracker interactionTracker, int requestId)
	{
		InteractionTracker = interactionTracker;
		RequestId = requestId;
	}

	protected InteractionTracker InteractionTracker { get; }

	protected int RequestId { get; }

	public abstract Vector3 InitialVelocity { get; }
	public abstract Vector3 FinalPosition { get; }
	public abstract Vector3 FinalModifiedPosition { get; }
	public float FinalScale => InteractionTracker.Scale; // TODO: Scale not yet implemented

	/// <summary>Called once per frame with the ticks elapsed since the motion started.</summary>
	protected abstract void Advance(long elapsedTicks);

	public void Start()
	{
		if (_handler is not null)
		{
			throw new InvalidOperationException("Cannot start inertia twice.");
		}

		if (InteractionTracker.FrameTarget is not { } target)
		{
			// Nothing would ever advance the motion, and advancing is the only way out of the inertia state.
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("No composition target to advance the inertia; completing it immediately.");
			}

			Complete();
			return;
		}

		_startTimestamp = null;
		_target = target;
		_handler = OnFrameStarting;
		target.FrameStarting += _handler;
	}

	public void Stop()
	{
		if (_handler is not null)
		{
			_target!.FrameStarting -= _handler;
			_handler = null;
			_target = null;
		}
	}

	protected void Complete()
	{
		Stop();
		InteractionTracker.SetPosition(FinalModifiedPosition, RequestId);
		InteractionTracker.ChangeState(new InteractionTrackerIdleState(InteractionTracker, RequestId));
	}

	private void OnFrameStarting(object? sender, long timestamp)
	{
		if (_startTimestamp is not { } start)
		{
			// A tick already queued can run before the owner hears about the inertia, which must come first.
			if (!InteractionTracker.State.HasEntered)
			{
				return;
			}

			// Back-dated by a frame, so the first frame moves by a whole frame's worth.
			start = timestamp - _target!.FrameIntervalInTicks;
			_startTimestamp = start;
		}

		Advance(timestamp - start);
	}
}
