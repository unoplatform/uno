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
	private Vector3 _deltaPosition;
	private Vector3 _finalPosition;
	private Vector3 _timeToMinimumVelocity;
	private float _maxTimeToMinimumVelocity;
	private Timer? _timer;
	private Stopwatch? _stopwatch;

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

		_initialVelocity = translationVelocities;
		_initialPosition = interactionTracker.Position;
		_deltaPosition = new Vector3(CalculateDeltaPosition(_initialVelocity.X), CalculateDeltaPosition(_initialVelocity.Y), CalculateDeltaPosition(_initialVelocity.Z));
		_finalPosition = _initialPosition + _deltaPosition;

		_timeToMinimumVelocity = TimeToMinimumVelocity();
		_maxTimeToMinimumVelocity = Math.Max(Math.Max(_timeToMinimumVelocity.X, _timeToMinimumVelocity.Y), _timeToMinimumVelocity.Z);

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

		_stopwatch = Stopwatch.StartNew();
		_timer = new Timer(OnTick, null, 0, IntervalInMilliseconds);
	}

	private void OnTick(object? state)
	{
		var currentElapsed = _stopwatch!.ElapsedMilliseconds;
		if (currentElapsed >= _maxTimeToMinimumVelocity * 1000)
		{
			_interactionTracker.SetPosition(_finalPosition, isFromUserManipulation: false/*TODO*/);
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
			TimeToMinimumVelocityCore(initialVelocity.X, 1 - _positionDecayRate.X),
			TimeToMinimumVelocityCore(initialVelocity.Y, 1 - _positionDecayRate.Y),
			TimeToMinimumVelocityCore(initialVelocity.Z, 1 - _positionDecayRate.Z)
			);

		float TimeToMinimumVelocityCore(float initialVelocity, float decayRate)
		{
			if (initialVelocity > minimumVelocity)
			{
				if (!IsCloseReal(decayRate, 1.0f, epsilon))
				{
					if (IsCloseRealZero(decayRate, epsilon) /*|| (this.m_inertiaEnabled & 1) == 0*/)
					{
						return 0.0f;
					}
					else
					{
						return (MathF.Log(minimumVelocity) - MathF.Log(initialVelocity)) / MathF.Log(decayRate);
					}
				}

				time = (Math.Sign(initialVelocity) * float.MaxValue /*- this.m_initialValue*/) / initialVelocity;

				if (time < 0.0f)
				{
					return 0.0f;
				}
			}

			return time;
		}
	}

	bool IsCloseReal(float a, float b, float epsilon)
		=> MathF.Abs(a - b) <= epsilon;

	bool IsCloseRealZero(float a, float epsilon)
		=> MathF.Abs(a) < epsilon;

}
