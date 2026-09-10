#nullable enable

using System;
using System.Numerics;
using Uno.Foundation.Logging;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition.Interactions;

internal class InteractionTrackerPointerWheelInertiaHandler : IInteractionTrackerInertiaHandler
{
	private ICompositionTarget? _target;
	private EventHandler<long>? _handler;
	private long _startTimestamp;

	private readonly InteractionTracker _interactionTracker;
	private readonly Vector3 _minPosition;
	private readonly Vector3 _maxPosition;
	private readonly Vector3 _initialPosition;
	private readonly Vector3 _calculatedFinalPosition;

	public InteractionTrackerPointerWheelInertiaHandler(InteractionTracker interactionTracker, Vector3 translationVelocities)
	{
		_interactionTracker = interactionTracker;
		_minPosition = interactionTracker.MinPosition;
		_maxPosition = interactionTracker.MaxPosition;
		_initialPosition = _interactionTracker.Position;

		InitialVelocity = translationVelocities;

		// This handler works with constant velocity for 0.25 second.
		_calculatedFinalPosition = interactionTracker.Position + InitialVelocity * 0.25f;
	}

	public Vector3 InitialVelocity { get; }

	public Vector3 FinalPosition => Vector3.Clamp(_calculatedFinalPosition, _minPosition, _maxPosition);

	public Vector3 FinalModifiedPosition => FinalPosition;

	public float FinalScale => _interactionTracker.Scale; // TODO: Scale not yet implemented

	/// <summary>
	/// Advanced once per presented frame rather than at a fixed interval of its own, so the motion is
	/// sampled on the cadence the frames are actually shown on.
	/// </summary>
	public void Start()
	{
		if (_handler is not null)
		{
			throw new InvalidOperationException("Cannot start inertia twice.");
		}

		if (Compositor.FrameDriverTargetResolver?.Invoke() is not { } target)
		{
			// Advance is the only path out of the inertia state, so settle here rather than leaving the
			// tracker mid-inertia with nothing left to move it.
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("No frame-driver target resolved; completing wheel inertia immediately.");
			}

			_interactionTracker.SetPosition(FinalModifiedPosition, requestId: 0);
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId: 0));
			return;
		}

		_startTimestamp = 0;
		_target = target;
		_handler = OnTick;
		target.FrameStarting += _handler;
	}

	/// <summary>
	/// Idempotent, and the only way this stops. Unsubscribes from the target it joined: a subscription
	/// left behind keeps the compositor reporting itself as animating for good.
	/// </summary>
	public void Stop()
	{
		if (_handler is not null)
		{
			_target!.FrameStarting -= _handler;
			_handler = null;
			_target = null;
		}
	}

	private void OnTick(object? sender, long timestampInTicks)
	{
		if (_startTimestamp == 0)
		{
			_startTimestamp = timestampInTicks - _target!.FrameIntervalInTicks;
		}

		Advance((timestampInTicks - _startTimestamp) / (double)TimeSpan.TicksPerMillisecond);
	}

	private void Advance(double currentElapsed)
	{
		if (currentElapsed >= 250)
		{
			_interactionTracker.SetPosition(FinalModifiedPosition, requestId: 0);
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId: 0));
			Stop();
			return;
		}

		var newPosition = _initialPosition + (float)(currentElapsed / 1000.0) * InitialVelocity;
		var clampedNewPosition = Vector3.Clamp(newPosition, _minPosition, _maxPosition);

		_interactionTracker.SetPosition(clampedNewPosition, requestId: 0);

		if (clampedNewPosition.Equals(FinalModifiedPosition))
		{
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId: 0));
			Stop();
		}
	}
}
