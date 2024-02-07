#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;

namespace Microsoft.UI.Composition.Interactions;

internal sealed class InteractionTrackerInertiaState : InteractionTrackerState
{
	private readonly InteractionTrackerInertiaHandler _handler;

	public InteractionTrackerInertiaState(InteractionTracker interactionTracker, Vector3 translationVelocities) : base(interactionTracker)
	{
		_handler = new InteractionTrackerInertiaHandler(interactionTracker, translationVelocities);
	}

	protected override void EnterState(IInteractionTrackerOwner? owner)
	{
		owner?.InertiaStateEntered(_interactionTracker, new InteractionTrackerInertiaStateEnteredArgs());

		// If TryUpdatePosition is called with clamping option disabled, the position set can go outside the [MinPosition..MaxPosition] range.
		// We adjust MinPosition/MaxPosition when we enter idle.
		// Docs around InteractionTrackerClampingOption.Disabled: https://learn.microsoft.com/uwp/api/windows.ui.composition.interactions.interactiontrackerclampingoption
		// > If the input value is greater (or less) than the max (or min) value, it is not immediately
		// > clamped. Instead, the max/min is enforced to the newly input value
		// > of Position (and potentially clamped) the next time InteractionTracker enters
		// > the Inertia state.
		var position = _interactionTracker.Position;
		_interactionTracker.MinPosition = Vector3.Min(_interactionTracker.MinPosition, position);
		_interactionTracker.MaxPosition = Vector3.Max(_interactionTracker.MaxPosition, position);

		_handler.Start();
	}

	internal override void StartUserManipulation()
	{
		_interactionTracker.ChangeState(new InteractionTrackerInteractingState(_interactionTracker));
	}

	internal override void CompleteUserManipulation(Vector3 linearVelocity)
	{
	}

	internal override void ReceiveManipulationDelta(Point translationDelta)
	{
	}

	internal override void ReceiveInertiaStarting(Point linearVelocity)
	{
	}

	internal override void TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond)
	{
		// Inertia is restarted (state re-enters inertia) and inertia modifiers are evaluated with requested velocity added to current velocity
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, _handler.InitialVelocity + velocityInPixelsPerSecond));
	}

	internal override void TryUpdatePosition(Vector3 value, InteractionTrackerClampingOption option)
	{
		if (option == InteractionTrackerClampingOption.Auto)
		{
			value = Vector3.Clamp(value, _interactionTracker.MinPosition, _interactionTracker.MaxPosition);
		}

		_interactionTracker.SetPosition(value, isFromUserManipulation: true);
		_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker));
	}

	public override void Dispose() => _handler.Stop();
}
