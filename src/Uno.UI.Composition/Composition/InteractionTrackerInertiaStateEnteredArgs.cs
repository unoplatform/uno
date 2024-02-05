using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

public partial class InteractionTrackerInertiaStateEnteredArgs
{
	internal InteractionTrackerInertiaStateEnteredArgs()
	{
	}

	// TODO: Set the properties.
	public Vector3? ModifiedRestingPosition { get; }

	public float? ModifiedRestingScale { get; }

	public Vector3 NaturalRestingPosition { get; }

	public float NaturalRestingScale { get; }

	public Vector3 PositionVelocityInPixelsPerSecond { get; }

	public int RequestId { get; }

	public float ScaleVelocityInPercentPerSecond { get; }

	public bool IsInertiaFromImpulse { get; }

	public bool IsFromBinding { get; }
}
