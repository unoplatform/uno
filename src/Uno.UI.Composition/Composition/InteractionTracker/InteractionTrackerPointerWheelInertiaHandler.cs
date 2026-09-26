#nullable enable

using System;
using System.Numerics;
using Uno.Foundation.Logging;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition.Interactions;

/// <summary>
/// The wheel curve measured on WinUI 3's ScrollView: velocity v0·(1 − (t/T)²) over T = 257ms, so position
/// D·1.5·(s − s³/3) with s = t/T, and v0 = 1.5·D/T. It has no first-frame jump, and a notch arriving mid-motion
/// restarts it over what is left plus the new notch.
/// </summary>
internal class InteractionTrackerPointerWheelInertiaHandler : IInteractionTrackerInertiaHandler
{
	private const double DurationInSeconds = 0.257;
	private const float LaunchFactor = 1.5f;

	private readonly InteractionTracker _interactionTracker;
	private readonly Vector3 _minPosition;
	private readonly Vector3 _maxPosition;
	private readonly Vector3 _initialPosition;
	private readonly Vector3 _distance;
	private readonly Vector3 _calculatedFinalPosition;

	private ICompositionTarget? _target;
	private EventHandler<long>? _handler;
	private long _startTimestamp;

	public InteractionTrackerPointerWheelInertiaHandler(InteractionTracker interactionTracker, Vector3 translationVelocities)
	{
		_interactionTracker = interactionTracker;
		_minPosition = interactionTracker.MinPosition;
		_maxPosition = interactionTracker.MaxPosition;
		_initialPosition = _interactionTracker.Position;

		InitialVelocity = translationVelocities;

		_distance = InitialVelocity * (float)DurationInSeconds / LaunchFactor;
		_calculatedFinalPosition = interactionTracker.Position + _distance;
	}

	/// <summary>The launch velocity that makes the curve travel <paramref name="distance"/>.</summary>
	internal static Vector3 GetLaunchVelocity(Vector3 distance) => distance * LaunchFactor / (float)DurationInSeconds;

	/// <inheritdoc cref="GetLaunchVelocity(Vector3)"/>
	internal static float GetLaunchVelocity(float distance) => distance * LaunchFactor / (float)DurationInSeconds;

	public Vector3 InitialVelocity { get; }

	public Vector3 FinalPosition => Vector3.Clamp(_calculatedFinalPosition, _minPosition, _maxPosition);

	public Vector3 FinalModifiedPosition => FinalPosition;

	public float FinalScale => _interactionTracker.Scale; // TODO: Scale not yet implemented

	/// <summary>Advanced once per frame, on the UI thread, from the frame's timestamp.</summary>
	public void Start()
	{
		if (_handler is not null)
		{
			throw new InvalidOperationException("Cannot start inertia twice.");
		}

		if (_interactionTracker.FrameTarget is not { } target)
		{
			// Nothing would ever advance the motion, and advancing is the only way out of the inertia state.
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("No composition target to advance the wheel inertia; completing it immediately.");
			}

			Complete();
			return;
		}

		_startTimestamp = 0;
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

	private void OnFrameStarting(object? sender, long timestamp)
	{
		if (_startTimestamp == 0)
		{
			// A tick already queued can run before the owner hears about the inertia, which must come first.
			if (!_interactionTracker.State.HasEntered)
			{
				return;
			}

			// Back-dated by a frame, so the first frame moves by a whole frame's worth.
			_startTimestamp = timestamp - _target!.FrameIntervalInTicks;
		}

		var s = (timestamp - _startTimestamp) / (double)TimeSpan.TicksPerSecond / DurationInSeconds;
		if (s >= 1)
		{
			Complete();
			return;
		}

		var newPosition = _initialPosition + _distance * (float)(LaunchFactor * (s - s * s * s / 3));
		var clampedNewPosition = Vector3.Clamp(newPosition, _minPosition, _maxPosition);

		_interactionTracker.SetPosition(clampedNewPosition, requestId: 0);

		if (clampedNewPosition.Equals(FinalModifiedPosition))
		{
			Stop();
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId: 0));
		}
	}

	private void Complete()
	{
		Stop();
		_interactionTracker.SetPosition(FinalModifiedPosition, requestId: 0);
		_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId: 0));
	}
}
