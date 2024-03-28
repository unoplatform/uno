#nullable enable

using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

namespace Windows.UI.Composition.Interactions;

internal sealed partial class InteractionTrackerActiveInputInertiaHandler : IInteractionTrackerInertiaHandler
{
	private readonly InteractionTracker _interactionTracker;
	private readonly AxisHelper _xHelper;
	private readonly AxisHelper _yHelper;
	private readonly AxisHelper _zHelper;
	private readonly int _requestId;

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

	public InteractionTrackerActiveInputInertiaHandler(InteractionTracker interactionTracker, Vector3 translationVelocities, int requestId)
	{
		_interactionTracker = interactionTracker;
		_xHelper = new AxisHelper(this, translationVelocities, Axis.X);
		_yHelper = new AxisHelper(this, translationVelocities, Axis.Y);
		_zHelper = new AxisHelper(this, translationVelocities, Axis.Z);
		_requestId = requestId;
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
			_interactionTracker.SetPosition(FinalModifiedPosition, _requestId);
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, _requestId));
			_timer!.Dispose();
			_stopwatch!.Stop();
			return;
		}

		var newPosition = new Vector3(
			_xHelper.GetPosition(currentElapsedInSeconds),
			_yHelper.GetPosition(currentElapsedInSeconds),
			_zHelper.GetPosition(currentElapsedInSeconds));

		_interactionTracker.SetPosition(newPosition, _requestId);
	}
}
