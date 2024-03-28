using System.Numerics;

namespace Windows.UI.Composition.Interactions;

internal interface IInteractionTrackerInertiaHandler
{
	Vector3 InitialVelocity { get; }
	Vector3 FinalPosition { get; }
	Vector3 FinalModifiedPosition { get; }
	float FinalScale { get; }

	void Start();
	void Stop();
}
