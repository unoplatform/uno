#nullable enable

using System;
using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

/// <summary>
/// The wheel curve measured on WinUI 3's ScrollView: velocity v0·(1 − (t/T)²) over T = 257ms, so position
/// D·1.5·(s − s³/3) with s = t/T, and v0 = 1.5·D/T. It has no first-frame jump, and a notch arriving mid-motion
/// restarts it over what is left plus the new notch.
/// </summary>
internal class InteractionTrackerPointerWheelInertiaHandler : InteractionTrackerFrameInertiaHandler
{
	private const double DurationInSeconds = 0.257;
	private const float LaunchFactor = 1.5f;

	private readonly Vector3 _minPosition;
	private readonly Vector3 _maxPosition;
	private readonly Vector3 _initialPosition;
	private readonly Vector3 _distance;
	private readonly Vector3 _calculatedFinalPosition;

	public InteractionTrackerPointerWheelInertiaHandler(InteractionTracker interactionTracker, Vector3 translationVelocities)
		: base(interactionTracker, requestId: 0)
	{
		_minPosition = interactionTracker.MinPosition;
		_maxPosition = interactionTracker.MaxPosition;
		_initialPosition = interactionTracker.Position;

		InitialVelocity = translationVelocities;

		_distance = InitialVelocity * (float)DurationInSeconds / LaunchFactor;
		_calculatedFinalPosition = interactionTracker.Position + _distance;
	}

	/// <summary>The launch velocity that makes the curve travel <paramref name="distance"/>.</summary>
	internal static Vector3 GetLaunchVelocity(Vector3 distance) => distance * LaunchFactor / (float)DurationInSeconds;

	/// <inheritdoc cref="GetLaunchVelocity(Vector3)"/>
	internal static float GetLaunchVelocity(float distance) => distance * LaunchFactor / (float)DurationInSeconds;

	public override Vector3 InitialVelocity { get; }

	public override Vector3 FinalPosition => Vector3.Clamp(_calculatedFinalPosition, _minPosition, _maxPosition);

	public override Vector3 FinalModifiedPosition => FinalPosition;

	protected override void Advance(long elapsedTicks)
	{
		var s = elapsedTicks / (double)TimeSpan.TicksPerSecond / DurationInSeconds;
		if (s >= 1)
		{
			Complete();
			return;
		}

		var newPosition = _initialPosition + _distance * (float)(LaunchFactor * (s - s * s * s / 3));
		var clampedNewPosition = Vector3.Clamp(newPosition, _minPosition, _maxPosition);

		InteractionTracker.SetPosition(clampedNewPosition, RequestId);

		if (clampedNewPosition.Equals(FinalModifiedPosition))
		{
			Complete();
		}
	}
}
