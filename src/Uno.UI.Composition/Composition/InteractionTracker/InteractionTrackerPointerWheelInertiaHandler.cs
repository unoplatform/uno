#nullable enable

using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

namespace Windows.UI.Composition.Interactions;

internal class InteractionTrackerPointerWheelInertiaHandler : IInteractionTrackerInertiaHandler
{
	// InteractionTracker works at 60 FPS, per documentation
	// https://learn.microsoft.com/en-us/windows/uwp/composition/interaction-tracker-manipulations#why-use-interactiontracker
	// > InteractionTracker was built to utilize the new Animation engine that operates on an independent thread at 60 FPS,resulting in smooth motion.
	private const int IntervalInMilliseconds = 17; // Ceiling of 1000/60

	private Timer? _timer;
	private Stopwatch? _stopwatch;

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

	public void Start()
	{
		if (_timer is not null)
		{
			throw new InvalidOperationException("Cannot start inertia timer twice.");
		}

		_stopwatch = Stopwatch.StartNew();
		_timer = new Timer(OnTick, null, 0, IntervalInMilliseconds);
	}

	public void Stop()
	{
		_timer?.Dispose();
		_stopwatch?.Stop();
	}

	private void OnTick(object? state)
	{
		var currentElapsed = _stopwatch!.ElapsedMilliseconds;

		if (currentElapsed >= 250)
		{
			_interactionTracker.SetPosition(FinalModifiedPosition, requestId: 0);
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId: 0));
			_timer!.Dispose();
			_stopwatch!.Stop();
			return;
		}

		var newPosition = _initialPosition + (currentElapsed / 1000.0f) * InitialVelocity;
		var clampedNewPosition = Vector3.Clamp(newPosition, _minPosition, _maxPosition);

		_interactionTracker.SetPosition(clampedNewPosition, requestId: 0);

		if (clampedNewPosition.Equals(FinalModifiedPosition))
		{
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId: 0));
			_timer!.Dispose();
			_stopwatch!.Stop();
		}
	}
}
