#nullable enable

using System;
using System.Numerics;
using System.Threading;

namespace Microsoft.UI.Composition.Interactions;

internal class InteractionTrackerInertiaHandler
{
	private readonly InteractionTracker _interactionTracker;
	private readonly Vector3 _positionDecayRate;
	private Vector3 _currentVelocity;
	private Vector3 _currentPosition;
	private Vector3 _deltaPosition;
	private Vector3 _finalPosition;
	private Timer? _timer;

	// InteractionTracker works at 60 FPS, per documentation
	// https://learn.microsoft.com/en-us/windows/uwp/composition/interaction-tracker-manipulations#why-use-interactiontracker
	// > InteractionTracker was built to utilize the new Animation engine that operates on an independent thread at 60 FPS,resulting in smooth motion.
	private const int IntervalInMilliseconds = 17; // Ceiling of 1000/60

	private const float IntervalInSeconds = IntervalInMilliseconds / 1000.0f;

	public InteractionTrackerInertiaHandler(InteractionTracker interactionTracker, Vector3 translationVelocities)
	{
		_interactionTracker = interactionTracker;

		// interactionTracker.PositionInteriaDecayRate not supported yet. It should affect the _finalPosition calculated below
		_positionDecayRate = /*interactionTracker.PositionInertiaDecayRate ??*/ new Vector3(0.95f);

		_currentVelocity = translationVelocities;
		_currentPosition = interactionTracker.Position;
		_deltaPosition = new Vector3(CalculateDeltaPosition(_currentVelocity.X), CalculateDeltaPosition(_currentVelocity.Y), CalculateDeltaPosition(_currentVelocity.Z));
		_finalPosition = _currentPosition + _deltaPosition;

		// Based on experiment on WinUI.
		// This was based on setting PositionInertiaDecayRate to 0.95 (the default)
		// Note that if velocity is too small, we can get delta in the opposite direction. In this case, we consider it zero and make no changes in position.
		static float CalculateDeltaPosition(float velocity)
		{
			var delta = (float)(0.3338081907 * velocity - 10.01424572);
			return Math.Sign(delta) == Math.Sign(velocity) ? delta : 0;
		}
	}

	public void Start()
	{
		if (_timer is not null)
		{
			throw new InvalidOperationException("Cannot start inertia timer twice.");
		}

		_timer = new Timer(OnTick, null, 0, IntervalInMilliseconds);
	}

	private void OnTick(object? state)
	{
		// Far from WinUI calculations :/
		_currentVelocity.X -= (_positionDecayRate.X * IntervalInSeconds * _currentVelocity.X);
		_currentVelocity.Y -= (_positionDecayRate.Y * IntervalInSeconds * _currentVelocity.Y);

		_currentPosition.X += _currentVelocity.X * IntervalInSeconds;
		_currentPosition.Y += _currentVelocity.Y * IntervalInSeconds;
		if (ReachedOrExceededFinalPosition(_currentPosition.X, _finalPosition.X, _deltaPosition.X) && ReachedOrExceededFinalPosition(_currentPosition.Y, _finalPosition.Y, _deltaPosition.Y))
		{
			_currentPosition = _finalPosition;
			_interactionTracker.SetPosition(_currentPosition, isFromUserManipulation: false/*TODO*/);
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker));
			_timer!.Dispose();
		}
		else
		{
			_interactionTracker.SetPosition(_currentPosition, isFromUserManipulation: false/*TODO*/);
		}

		bool ReachedOrExceededFinalPosition(float current, float final, float delta)
			=> Math.Abs(final - current) >= Math.Abs(delta);

	}
}
