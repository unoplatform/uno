using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

public partial class InteractionTrackerInertiaStateEnteredArgs
{
	internal InteractionTrackerInertiaStateEnteredArgs()
	{
	}

	public Vector3? ModifiedRestingPosition { get; internal init; }

	public float? ModifiedRestingScale { get; internal init; }

	public Vector3 NaturalRestingPosition { get; internal init; }

	public float NaturalRestingScale { get; internal init; }

	public Vector3 PositionVelocityInPixelsPerSecond { get; internal init; }

	public int RequestId { get; internal init; }

	public float ScaleVelocityInPercentPerSecond { get; internal init; }

	public bool IsInertiaFromImpulse { get; internal init; }

	public bool IsFromBinding { get; internal init; }
}
