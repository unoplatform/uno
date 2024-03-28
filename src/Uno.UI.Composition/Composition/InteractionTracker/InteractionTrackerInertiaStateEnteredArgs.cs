using System.Numerics;

namespace Windows.UI.Composition.Interactions;

public partial class InteractionTrackerInertiaStateEnteredArgs
{
	internal InteractionTrackerInertiaStateEnteredArgs()
	{
	}

	public required Vector3? ModifiedRestingPosition { get; init; }

	public required float? ModifiedRestingScale { get; init; }

	public required Vector3 NaturalRestingPosition { get; init; }

	public required float NaturalRestingScale { get; init; }

	public required Vector3 PositionVelocityInPixelsPerSecond { get; init; }

	public required int RequestId { get; init; }

	public required float ScaleVelocityInPercentPerSecond { get; init; }

	public required bool IsInertiaFromImpulse { get; init; }

	public required bool IsFromBinding { get; init; }
}
