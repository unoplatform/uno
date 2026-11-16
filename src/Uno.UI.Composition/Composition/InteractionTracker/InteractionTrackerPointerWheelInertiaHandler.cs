#nullable enable

using System;
using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

/// <summary>
/// WinUI scrolls a notch with a sine ease-out keyframe animation from the current position to the clamped target,
/// D·sin(π/2·t/T) over T = 250ms, shortened by the share of the move the clamp left
/// (dwmcorei CInteractionTracker::ScrollToPosition and CalculatePositionAnimationDuration).
/// </summary>
internal class InteractionTrackerPointerWheelInertiaHandler : InteractionTrackerFrameInertiaHandler
{
	private const float DurationInSeconds = 0.25f;
	private const float MinDurationInSeconds = 0.001f;

	private readonly Vector3 _minPosition;
	private readonly Vector3 _maxPosition;
	private readonly Vector3 _initialPosition;
	private readonly Vector3 _distance;
	private readonly Vector3 _target;
	private readonly double _durationInSeconds;

	public InteractionTrackerPointerWheelInertiaHandler(InteractionTracker interactionTracker, Vector3 target)
		: base(interactionTracker, requestId: 0)
	{
		_minPosition = interactionTracker.MinPosition;
		_maxPosition = interactionTracker.MaxPosition;
		_initialPosition = interactionTracker.Position;

		_target = Vector3.Clamp(target, _minPosition, _maxPosition);
		_distance = _target - _initialPosition;

		var duration = _target == target
			? DurationInSeconds
			: Math.Max(DurationInSeconds * _distance.Length() / (target - _initialPosition).Length(), MinDurationInSeconds);
		_durationInSeconds = duration;

		InitialVelocity = _distance * (MathF.PI / 2 / duration);
	}

	public override Vector3 InitialVelocity { get; }

	public override Vector3 FinalPosition => _target;

	public override Vector3 FinalModifiedPosition => _target;

	protected override void Advance(long elapsedTicks)
	{
		var s = elapsedTicks / (double)TimeSpan.TicksPerSecond / _durationInSeconds;
		if (s >= 1)
		{
			Complete();
			return;
		}

		var newPosition = _initialPosition + _distance * (float)Math.Sin(Math.PI / 2 * s);
		var clampedNewPosition = Vector3.Clamp(newPosition, _minPosition, _maxPosition);

		InteractionTracker.SetPosition(clampedNewPosition, RequestId);

		if (clampedNewPosition.Equals(FinalModifiedPosition))
		{
			Complete();
		}
	}
}
