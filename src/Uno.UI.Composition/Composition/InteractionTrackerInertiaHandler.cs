#nullable enable

using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

namespace Microsoft.UI.Composition.Interactions;

internal sealed partial class InteractionTrackerInertiaHandler
{
	private readonly InteractionTracker _interactionTracker;
	private readonly AxisHelper _xHelper;
	private readonly AxisHelper _yHelper;
	private readonly AxisHelper _zHelper;

	//private float _lastElapsedInSeconds;

	private Timer? _timer;
	private Stopwatch? _stopwatch;

	// InteractionTracker works at 60 FPS, per documentation
	// https://learn.microsoft.com/en-us/windows/uwp/composition/interaction-tracker-manipulations#why-use-interactiontracker
	// > InteractionTracker was built to utilize the new Animation engine that operates on an independent thread at 60 FPS,resulting in smooth motion.
	private const int IntervalInMilliseconds = 17; // Ceiling of 1000/60

	public Vector3 InitialVelocity => new Vector3(_xHelper.InitialVelocity, _yHelper.InitialVelocity, _zHelper.InitialVelocity);
	public Vector3 FinalPosition => new Vector3(_xHelper.FinalValue, _yHelper.FinalValue, _zHelper.FinalValue);
	public Vector3 FinalModifiedPosition => new Vector3(_xHelper.FinalModifiedValue, _yHelper.FinalModifiedValue, _zHelper.FinalModifiedValue);
	public float FinalScale => _interactionTracker.Scale; // TODO: Scale not yet implemented

	public InteractionTrackerInertiaHandler(InteractionTracker interactionTracker, Vector3 translationVelocities)
	{
		_interactionTracker = interactionTracker;
		_xHelper = new AxisHelper(this, translationVelocities, Axis.X);
		_yHelper = new AxisHelper(this, translationVelocities, Axis.Y);
		_zHelper = new AxisHelper(this, translationVelocities, Axis.Z);
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
		var currentElapsedInSeconds = _stopwatch!.ElapsedMilliseconds / 1000.0f;

		if (_xHelper.HasCompleted && _yHelper.HasCompleted && _zHelper.HasCompleted)
		{
			_interactionTracker.SetPosition(FinalModifiedPosition, isFromUserManipulation: false/*TODO*/);
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker));
			_timer!.Dispose();
			_stopwatch!.Stop();
			return;
		}

		var newPosition = new Vector3(
			_xHelper.GetPosition(currentElapsedInSeconds),
			_yHelper.GetPosition(currentElapsedInSeconds),
			_zHelper.GetPosition(currentElapsedInSeconds));

		//var currentVelocity = VelocityAtTime(currentElapsedInSeconds);
		//var currentPosition = _interactionTracker.Position;

		// Far from WinUI calculations :/
		//var newPosition = new Vector3(
		//	CalculatePosition(currentElapsedInSeconds, _timeToMinimumVelocity.X, minPosition.X, maxPosition.X, _initialVelocity.X, currentVelocity.X, _positionDecayRate.X, currentPosition.X),
		//	CalculatePosition(currentElapsedInSeconds, _timeToMinimumVelocity.Y, minPosition.Y, maxPosition.Y, _initialVelocity.Y, currentVelocity.Y, _positionDecayRate.Y, currentPosition.Y),
		//	CalculatePosition(currentElapsedInSeconds, _timeToMinimumVelocity.Z, minPosition.Z, maxPosition.Z, _initialVelocity.Z, currentVelocity.Z, _positionDecayRate.Z, currentPosition.Z)
		//	);
		//var currentPosition = _interactionTracker.Position;
		//var newPosition = new Vector3(
		//	CalculatePosition(_initialPosition.X, _initialVelocity.X, 1.0f - _positionDecayRate.X, _timeToMinimumVelocity.X, _finalPosition.X, minPosition.X, maxPosition.X, currentPosition.X, currentElapsedInSeconds),
		//	CalculatePosition(_initialPosition.Y, _initialVelocity.Y, 1.0f - _positionDecayRate.Y, _timeToMinimumVelocity.Y, _finalPosition.Y, minPosition.Y, maxPosition.Y, currentPosition.Y, currentElapsedInSeconds),
		//	CalculatePosition(_initialPosition.Z, _initialVelocity.Z, 1.0f - _positionDecayRate.Z, _timeToMinimumVelocity.Z, _finalPosition.Z, minPosition.Z, maxPosition.Z, currentPosition.Z, currentElapsedInSeconds));

		//Console.WriteLine($"Initial: {_initialPosition.X}, Final: {_finalPosition.X}, Current: {newPosition.X}");

		_interactionTracker.SetPosition(newPosition, isFromUserManipulation: false/*TODO*/);
		//_lastElapsedInSeconds = currentElapsedInSeconds;
	}

	//private float CalculatePosition(
	//	float currentElapsedInSeconds, float timeToMinimumVelocity, float minPosition, float maxPosition, float initialVelocity, float currentVelocity, float positionDecayRate, float currentPosition)
	//{
	//	if (_dampingStateTimeInSeconds.HasValue || currentPosition < minPosition || currentPosition > maxPosition)
	//	{
	//		// This is an overpan from Interacting state. Use damping animation.
	//		_dampingStateTimeInSeconds ??= _stopwatch!.ElapsedMilliseconds / 1000.0f;
	//		if (initialVelocity <= 50.0)
	//		{
	//			// Use critically-damped animation.
	//		}
	//		else
	//		{
	//			// Use underdamped animation.
	//		}
	//	}

	//	var deltaTime = currentElapsedInSeconds >= timeToMinimumVelocity * 1000 ? 0.0f : currentElapsedInSeconds - _lastElapsedInSeconds;
	//	var deltaPosition = CalculateDeltaPosition(currentVelocity, 1.0f - positionDecayRate, deltaTime);
	//	return currentPosition + deltaPosition;
	//}



	//private Vector3 VelocityAtTime(float elapsedTime)
	//{
	//	return new Vector3(
	//		MathF.Pow(1 - _positionDecayRate.X, elapsedTime) * _initialVelocity.X,
	//		MathF.Pow(1 - _positionDecayRate.Y, elapsedTime) * _initialVelocity.Y,
	//		MathF.Pow(1 - _positionDecayRate.Z, elapsedTime) * _initialVelocity.Z);
	//}

	private static bool IsCloseReal(float a, float b, float epsilon)
		=> MathF.Abs(a - b) <= epsilon;

	private static bool IsCloseRealZero(float a, float epsilon)
		=> MathF.Abs(a) < epsilon;

}
