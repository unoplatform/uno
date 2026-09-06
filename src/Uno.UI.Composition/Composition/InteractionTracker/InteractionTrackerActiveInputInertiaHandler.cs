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

	/// <summary>Seconds since the motion started, as of the tick being processed.</summary>
	internal float ElapsedInSeconds { get; private set; }

	private ICompositionTarget? _target;
	private EventHandler<long>? _handler;
	private long _startTimestamp;

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

	/// <summary>
	/// Advanced once per presented frame, so the motion is sampled on the cadence the frames are shown
	/// on. A fixed-interval timer cannot do that — it runs at its own rate regardless of the display's,
	/// so on a 120Hz panel a 17ms timer leaves most frames with nothing new to show — and it wrote
	/// composition state from a thread-pool thread.
	/// </summary>
	public void Start()
	{
		if (_handler is not null)
		{
			throw new InvalidOperationException("Cannot start inertia twice.");
		}

		if (Compositor.FrameDriverTargetResolver?.Invoke() is not { } target)
		{
			// Nothing will ever advance this motion, and Advance is the only path out of the inertia
			// state, so land in Idle here rather than stranding the tracker mid-inertia forever.
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("No frame-driver target resolved; completing inertia immediately.");
			}

			_interactionTracker.SetPosition(FinalModifiedPosition, _requestId);
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, _requestId));
			return;
		}

		// Anchored on the first tick rather than here, so the first step is a whole frame's worth
		// instead of however much of one is left when the finger lifts.
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

		Advance((float)((timestampInTicks - _startTimestamp) / (double)TimeSpan.TicksPerSecond));
	}

	private void Advance(float currentElapsedInSeconds)
	{
		ElapsedInSeconds = currentElapsedInSeconds;

		if (_xHelper.HasCompleted && _yHelper.HasCompleted && _zHelper.HasCompleted)
		{
			_interactionTracker.SetPosition(FinalModifiedPosition, _requestId);
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, _requestId));
			Stop();
			return;
		}

		var newPosition = new Vector3(
			_xHelper.GetPosition(currentElapsedInSeconds),
			_yHelper.GetPosition(currentElapsedInSeconds),
			_zHelper.GetPosition(currentElapsedInSeconds));

		_interactionTracker.SetPosition(newPosition, _requestId);
	}
}
