#nullable enable

using System;
using System.Numerics;
using Uno.UI.Dispatching;
using Windows.Foundation;

namespace Microsoft.UI.Composition.Interactions;

internal sealed class InteractionTrackerCustomAnimationState : InteractionTrackerState
{
	private readonly CompositionAnimation _animation;
	private readonly int _requestId;
	private readonly bool _isScaleAnimation;
	private readonly Vector3 _centerPoint;
	private bool _isAnimationRunning;
	private bool _isFrameHandlerAttached;

	private InteractionTrackerCustomAnimationState(
		InteractionTracker interactionTracker,
		CompositionAnimation animation,
		int requestId,
		bool isScaleAnimation,
		Vector3 centerPoint) : base(interactionTracker)
	{
		_requestId = requestId;
		_isScaleAnimation = isScaleAnimation;
		_centerPoint = centerPoint;

		// Expression animations snapshot their state per target; keyframe animations return themselves.
		_animation = animation.CloneAnimation();
		_animation.Start(isScaleAnimation ? nameof(InteractionTracker.Scale) : nameof(InteractionTracker.Position), default, interactionTracker);
		_isAnimationRunning = true;

		if (_animation is KeyFrameAnimation keyFrameAnimation)
		{
			keyFrameAnimation.Stopped += OnAnimationStopped;
		}
	}

	internal static InteractionTrackerCustomAnimationState ForPosition(InteractionTracker interactionTracker, CompositionAnimation animation, int requestId)
		=> new(interactionTracker, animation, requestId, isScaleAnimation: false, centerPoint: default);

	internal static InteractionTrackerCustomAnimationState ForScale(InteractionTracker interactionTracker, CompositionAnimation animation, Vector3 centerPoint, int requestId)
		=> new(interactionTracker, animation, requestId, isScaleAnimation: true, centerPoint);

	protected override void EnterState(IInteractionTrackerOwner? owner)
	{
		if (_disposed)
		{
			return;
		}

		owner?.CustomAnimationStateEntered(_interactionTracker, new InteractionTrackerCustomAnimationStateEnteredArgs(_requestId, isFromBinding: false));

		if (_disposed)
		{
			return;
		}

		if (InteractionTrackerFrameClock.TrySubscribe(OnFrame))
		{
			_isFrameHandlerAttached = true;
		}
		else
		{
			// No frame clock (e.g. no XAML host): jump to the end rather than never completing.
			ApplyValue(_animation is KeyFrameAnimation keyFrameAnimation ? keyFrameAnimation.Evaluate(1.0f) : _animation.Evaluate());
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, _requestId));
		}
	}

	private void OnFrame(object? sender, object args)
	{
		if (_disposed)
		{
			return;
		}

		ApplyValue(_animation.Evaluate());

		if (!_isAnimationRunning)
		{
			_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, _requestId));
		}
	}

	private void ApplyValue(object? value)
	{
		if (_isScaleAnimation)
		{
			if (value is float scale)
			{
				scale = Math.Clamp(scale, _interactionTracker.MinScale, _interactionTracker.MaxScale);
				_interactionTracker.SetScale(scale, _centerPoint, _requestId);
			}
		}
		else if (value is Vector3 position)
		{
			position = Vector3.Clamp(position, _interactionTracker.MinPosition, _interactionTracker.MaxPosition);
			_interactionTracker.SetPosition(position, _requestId);
		}
	}

	private void OnAnimationStopped(object? sender, EventArgs e) => _isAnimationRunning = false;

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

	internal override void ReceivePointerWheel(int delta, bool isHorizontal)
	{
	}

	internal override void TryUpdatePositionWithAdditionalVelocity(Vector3 velocityInPixelsPerSecond, int requestId)
	{
		// State changes to inertia with inertia modifiers evaluated using requested velocity as initial velocity.
		// TODO: inertia modifiers not yet implemented.
		_interactionTracker.ChangeState(new InteractionTrackerInertiaState(_interactionTracker, velocityInPixelsPerSecond, requestId, isFromPointerWheel: false));
	}

	internal override void TryUpdatePosition(Vector3 value, InteractionTrackerClampingOption option, int requestId)
	{
		if (option == InteractionTrackerClampingOption.Auto)
		{
			value = Vector3.Clamp(value, _interactionTracker.MinPosition, _interactionTracker.MaxPosition);
		}

		_interactionTracker.SetPosition(value, requestId);
		_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId));
	}

	internal override void TryUpdateScale(float value, Vector3 centerPoint, int requestId)
	{
		value = Math.Clamp(value, _interactionTracker.MinScale, _interactionTracker.MaxScale);
		_interactionTracker.SetScale(value, centerPoint, requestId);
		_interactionTracker.ChangeState(new InteractionTrackerIdleState(_interactionTracker, requestId));
	}

	public override void Dispose()
	{
		base.Dispose();

		if (_animation is KeyFrameAnimation keyFrameAnimation)
		{
			keyFrameAnimation.Stopped -= OnAnimationStopped;
		}

		if (_isAnimationRunning)
		{
			_isAnimationRunning = false;
			_animation.Stop();
		}

		if (_isFrameHandlerAttached)
		{
			_isFrameHandlerAttached = false;
			if (NativeDispatcher.Main.HasThreadAccess)
			{
				InteractionTrackerFrameClock.Unsubscribe(OnFrame);
			}
			else
			{
				NativeDispatcher.Main.Enqueue(() => InteractionTrackerFrameClock.Unsubscribe(OnFrame));
			}
		}
	}
}
