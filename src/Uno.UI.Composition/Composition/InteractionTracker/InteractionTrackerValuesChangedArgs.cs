using System.Numerics;

namespace Windows.UI.Composition.Interactions;

public sealed partial class InteractionTrackerValuesChangedArgs
{
	internal InteractionTrackerValuesChangedArgs(Vector3 position, float scale, int requestId)
	{
		Position = position;
		Scale = scale;
		RequestId = requestId;
	}

	public Vector3 Position { get; }

	public int RequestId { get; }

	public float Scale { get; }
}
