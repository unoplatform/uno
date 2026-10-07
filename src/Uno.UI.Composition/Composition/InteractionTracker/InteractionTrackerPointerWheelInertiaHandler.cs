#nullable enable

using System;
using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

internal class InteractionTrackerPointerWheelInertiaHandler : InteractionTrackerFrameInertiaHandler
{
	private const double DurationInMilliseconds = 250;

	private readonly Vector3 _minPosition;
	private readonly Vector3 _maxPosition;
	private readonly Vector3 _initialPosition;
	private readonly Vector3 _calculatedFinalPosition;

	public InteractionTrackerPointerWheelInertiaHandler(InteractionTracker interactionTracker, Vector3 translationVelocities)
		: base(interactionTracker, requestId: 0)
	{
		_minPosition = interactionTracker.MinPosition;
		_maxPosition = interactionTracker.MaxPosition;
		_initialPosition = interactionTracker.Position;

		InitialVelocity = translationVelocities;

		// This handler works with constant velocity for 0.25 second.
		_calculatedFinalPosition = interactionTracker.Position + InitialVelocity * (float)(DurationInMilliseconds / 1000);
	}

	public override Vector3 InitialVelocity { get; }

	public override Vector3 FinalPosition => Vector3.Clamp(_calculatedFinalPosition, _minPosition, _maxPosition);

	public override Vector3 FinalModifiedPosition => FinalPosition;

	protected override void Advance(long elapsedTicks)
	{
		var elapsedInMilliseconds = elapsedTicks / (double)TimeSpan.TicksPerMillisecond;
		if (elapsedInMilliseconds >= DurationInMilliseconds)
		{
			Complete();
			return;
		}

		var newPosition = _initialPosition + (float)(elapsedInMilliseconds / 1000) * InitialVelocity;
		var clampedNewPosition = Vector3.Clamp(newPosition, _minPosition, _maxPosition);

		InteractionTracker.SetPosition(clampedNewPosition, RequestId);

		if (clampedNewPosition.Equals(FinalModifiedPosition))
		{
			Complete();
		}
	}
}
