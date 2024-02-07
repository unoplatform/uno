#nullable enable

using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

namespace Microsoft.UI.Composition.Interactions;

internal class InteractionTrackerInertiaHandler
{
	private readonly InteractionTracker _interactionTracker;
	private readonly Vector3 _positionDecayRate;
	private readonly Vector3 _initialVelocity;
	private readonly Vector3 _initialPosition;
	private readonly Vector3 _finalPosition;
	private readonly Vector3 _timeToMinimumVelocity;
	private readonly float _maxTimeToMinimumVelocity;
	private Timer? _timer;
	private Stopwatch? _stopwatch;

	// InteractionTracker works at 60 FPS, per documentation
	// https://learn.microsoft.com/en-us/windows/uwp/composition/interaction-tracker-manipulations#why-use-interactiontracker
	// > InteractionTracker was built to utilize the new Animation engine that operates on an independent thread at 60 FPS,resulting in smooth motion.
	private const int IntervalInMilliseconds = 17; // Ceiling of 1000/60

	public InteractionTrackerInertiaHandler(InteractionTracker interactionTracker, Vector3 translationVelocities)
	{
		_interactionTracker = interactionTracker;

		_positionDecayRate = interactionTracker.PositionInertiaDecayRate ?? new Vector3(0.95f);

		_initialVelocity = translationVelocities;
		_initialPosition = interactionTracker.Position;

		_timeToMinimumVelocity = TimeToMinimumVelocity();
		_maxTimeToMinimumVelocity = Math.Max(Math.Max(_timeToMinimumVelocity.X, _timeToMinimumVelocity.Y), _timeToMinimumVelocity.Z);

		var deltaPosition = new Vector3(
			CalculateDeltaPosition(_initialVelocity.X, 1.0f - _positionDecayRate.X, _timeToMinimumVelocity.X),
			CalculateDeltaPosition(_initialVelocity.Y, 1.0f - _positionDecayRate.Y, _timeToMinimumVelocity.Y),
			CalculateDeltaPosition(_initialVelocity.Z, 1.0f - _positionDecayRate.Z, _timeToMinimumVelocity.Z));

		_finalPosition = _initialPosition + deltaPosition;

		static float CalculateDeltaPosition(float velocity, float decayRate, float time)
		{
			float epsilon = 0.0000011920929f;

			if (IsCloseReal(decayRate, 1.0f, epsilon))
			{
				return velocity * time;
			}
			else if (IsCloseRealZero(decayRate, epsilon) /*|| !_isInertiaEnabled*/)
			{
				return 0.0f;
			}
			else
			{
				float val = MathF.Pow(decayRate, time);
				return ((val - 1.0f) * velocity) / MathF.Log(decayRate);
			}

		}
	}

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
		if (currentElapsed >= _maxTimeToMinimumVelocity * 1000)
		{
			var position = Vector3.Clamp(_finalPosition, _interactionTracker.MinPosition, _interactionTracker.MaxPosition);
			_interactionTracker.SetPosition(position, isFromUserManipulation: false/*TODO*/);
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker));
			_timer!.Dispose();
			_stopwatch!.Stop();
			return;
		}

		// Far from WinUI calculations :/
		var percentageX = 100.0f * MathF.Exp(MathF.Log(200.0f / 100.0f) / _timeToMinimumVelocity.X * (currentElapsed / 1000.0f)) - 100.0f;
		var percentageY = 100.0f * MathF.Exp(MathF.Log(200.0f / 100.0f) / _timeToMinimumVelocity.Y * (currentElapsed / 1000.0f)) - 100.0f;
		var percentageZ = 100.0f * MathF.Exp(MathF.Log(200.0f / 100.0f) / _timeToMinimumVelocity.Z * (currentElapsed / 1000.0f)) - 100.0f;

		var positionX = _initialPosition.X + (_finalPosition.X - _initialPosition.X) * percentageX / 100.0f;
		var positionY = _initialPosition.Y + (_finalPosition.Y - _initialPosition.Y) * percentageY / 100.0f;
		var positionZ = _initialPosition.Z + (_finalPosition.Z - _initialPosition.Z) * percentageZ / 100.0f;

		_interactionTracker.SetPosition(new(positionX, positionY, positionZ), isFromUserManipulation: false/*TODO*/);
	}

	private Vector3 TimeToMinimumVelocity()
	{
		var epsilon = 0.0000011920929f;

		var time = 0.0f;
		var minimumVelocity = 50.0f;

		var initialVelocity = Vector3.Abs(_initialVelocity);
		return new Vector3(
			TimeToMinimumVelocityCore(initialVelocity.X, 1 - _positionDecayRate.X, _initialPosition.X),
			TimeToMinimumVelocityCore(initialVelocity.Y, 1 - _positionDecayRate.Y, _initialPosition.Y),
			TimeToMinimumVelocityCore(initialVelocity.Z, 1 - _positionDecayRate.Z, _initialPosition.Z)
			);

		float TimeToMinimumVelocityCore(float initialVelocity, float decayRate, float initialPosition)
		{
			if (initialVelocity > minimumVelocity)
			{
				if (!IsCloseReal(decayRate, 1.0f, epsilon))
				{
					if (IsCloseRealZero(decayRate, epsilon) /*|| !_isInertiaEnabled*/)
					{
						return 0.0f;
					}
					else
					{
						return (MathF.Log(minimumVelocity) - MathF.Log(initialVelocity)) / MathF.Log(decayRate);
					}
				}

				time = (Math.Sign(initialVelocity) * float.MaxValue - initialPosition) / initialVelocity;

				if (time < 0.0f)
				{
					return 0.0f;
				}
			}

			return time;
		}
	}

	private static bool IsCloseReal(float a, float b, float epsilon)
		=> MathF.Abs(a - b) <= epsilon;

	private static bool IsCloseRealZero(float a, float epsilon)
		=> MathF.Abs(a) < epsilon;

}
