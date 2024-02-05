#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

public partial class InteractionTracker : CompositionObject
{
	private InteractionTrackerState _state;

	private InteractionTracker(Compositor compositor, IInteractionTrackerOwner? owner = null) : base(compositor)
	{
		_state = new InteractionTrackerIdleState(this);
		Owner = owner;
		InteractionSources = new CompositionInteractionSourceCollection(compositor);
	}

	public IInteractionTrackerOwner? Owner { get; }

	public float MinScale { get; set; } = 1.0f;

	public float MaxScale { get; set; } = 1.0f;

	public float Scale { get; } = 1.0f;

	public Vector3 MinPosition { get; set; }

	public Vector3 MaxPosition { get; set; }

	public Vector3 Position { get; }

	public CompositionInteractionSourceCollection InteractionSources { get; }

	//internal Vector3 EffectivePositionInertiaDecayRate => PositionInertiaDecayRate ?? new Vector3(0.95f);

	public static InteractionTracker Create(Compositor compositor) => new InteractionTracker(compositor);

	public static InteractionTracker CreateWithOwner(Compositor compositor, IInteractionTrackerOwner owner) => new InteractionTracker(compositor, owner);

	internal void ChangeState(InteractionTrackerState newState) => _state = newState;

	public int TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond)
		=> _state.TryUpdatePositionWithAdditionalVelocity(velocityInPixelsPerSecond);

	internal void StartUserManipulation()
	{
		_state.StartUserManipulation();
	}

	internal void CompleteUserManipulation()
	{
		_state.CompleteUserManipulation();
	}

	internal void ReceiveManipulationDelta()
	{
		_state.ReceiveManipulationDelta();
	}

	// TODO: Inertia -> Idle
	// This state transition happens when the function(s) being used to update position and/or scale are no longer
	// resulting in change. In other words, position and scale velocity have both gotten to zero.
	// This state transition can also happen if a call is made to explicitly update position or scale without animation or velocity.
	// These calls will end inertia and transition to Idle with the updated property values.


}
