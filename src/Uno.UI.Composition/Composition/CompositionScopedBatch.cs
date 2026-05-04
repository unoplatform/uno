#nullable enable

using System;
using System.Collections.Generic;
using Uno;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionScopedBatch : global::Microsoft.UI.Composition.CompositionObject
	{
		// Tracks the keyframe animations that were started while this batch was active. Completed
		// fires only after all of them have finished (or the batch was cleared by End() with no
		// pending work).
		private readonly HashSet<KeyFrameAnimation> _pendingAnimations = new();
		private readonly EventHandler _onAnimationStopped;
		private bool _hasPending;

		internal CompositionScopedBatch() => throw new NotSupportedException("Use the ctor with Compositor");

		internal CompositionScopedBatch(Compositor compositor, CompositionBatchTypes batchType) : base(compositor)
		{
			BatchType = batchType;
			IsActive = true;
			_onAnimationStopped = OnTrackedAnimationStopped;
			compositor.RegisterScopedBatch(this);
		}

		public bool IsActive { get; private set; }

		public bool IsEnded { get; private set; }

		internal CompositionBatchTypes BatchType { get; }

		// Called by Compositor.RegisterAnimation when an animation is started while this batch is the
		// active one. Tracks the animation so we can fire Completed when it stops.
		internal void TrackAnimation(KeyFrameAnimation animation)
		{
			if (IsEnded || !IsActive)
			{
				return;
			}

			if (_pendingAnimations.Add(animation))
			{
				_hasPending = true;
				animation.Stopped += _onAnimationStopped;
			}
		}

		private void OnTrackedAnimationStopped(object? sender, EventArgs e)
		{
			if (sender is KeyFrameAnimation animation)
			{
				animation.Stopped -= _onAnimationStopped;
				_pendingAnimations.Remove(animation);
			}

			if (IsEnded && _pendingAnimations.Count == 0)
			{
				RaiseCompleted();
			}
		}

		private void RaiseCompleted()
		{
			// WinUI raises Completed on a composition thread, so subscribers on the UI thread
			// effectively run on a later turn. Uno's compositor lives on the UI thread; a
			// synchronous invocation re-enters subscribers (e.g. AnimatedIcon.OnAnimationCompleted
			// which itself starts a new animation) and can cascade into a stack overflow when
			// each completion immediately starts another animation. Posting through the
			// dispatcher matches WinUI semantics and breaks the synchronous chain.
			if (!Dispatching.DispatcherQueue.Main.TryEnqueue(InvokeCompleted))
			{
				InvokeCompleted();
			}
		}

		private void InvokeCompleted()
		{
			Completed?.Invoke(this, new CompositionBatchCompletedEventArgs());
		}

		// End() means "no more animations will be added". The Completed event fires once all
		// already-tracked animations have stopped. If no animations were tracked (e.g. a batch
		// wrapping only an InsertScalar), fire Completed immediately so callers don't deadlock.
		public void End()
		{
			if (IsEnded)
			{
				return;
			}

			IsEnded = true;
			IsActive = false;
			Compositor.UnregisterScopedBatch(this);

			if ((BatchType & CompositionBatchTypes.InfiniteAnimation) != 0)
			{
				return;
			}

			if (!_hasPending || _pendingAnimations.Count == 0)
			{
				RaiseCompleted();
			}
		}

		public void Resume() => IsActive = true;

		public void Suspend() => IsActive = false;

		public event TypedEventHandler<object, global::Microsoft.UI.Composition.CompositionBatchCompletedEventArgs>? Completed;
	}
}
