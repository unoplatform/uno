#nullable enable

using System;
using System.Numerics;
using Uno.Foundation.Logging;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition.Interactions;

internal sealed partial class InteractionTrackerActiveInputInertiaHandler : IInteractionTrackerInertiaHandler
{
	private readonly InteractionTracker _interactionTracker;
	private readonly AxisHelper _xHelper;
	private readonly AxisHelper _yHelper;
	private readonly AxisHelper _zHelper;
	private readonly int _requestId;

	private ICompositionTarget? _target;
	private EventHandler<long>? _handler;
	private long _startTimestamp;

	/// <summary>Seconds since the motion started, as of the frame being processed.</summary>
	internal float ElapsedInSeconds { get; private set; }

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
				this.Log().Warn("No composition target to advance the inertia; completing it immediately.");
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

		ElapsedInSeconds = (float)((timestamp - _startTimestamp) / (double)TimeSpan.TicksPerSecond);

		if (_xHelper.HasCompleted && _yHelper.HasCompleted && _zHelper.HasCompleted)
		{
			Complete();
			return;
		}

		var newPosition = new Vector3(
			_xHelper.GetPosition(ElapsedInSeconds),
			_yHelper.GetPosition(ElapsedInSeconds),
			_zHelper.GetPosition(ElapsedInSeconds));

		_interactionTracker.SetPosition(newPosition, _requestId);
	}

	private void Complete()
	{
		Stop();
		_interactionTracker.SetPosition(FinalModifiedPosition, _requestId);
		_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, _requestId));
	}
}
