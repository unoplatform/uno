#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Uno;

namespace Microsoft.UI.Composition
{
	public partial class KeyFrameAnimation : CompositionAnimation
	{
		private protected IKeyFrameEvaluator? _keyframeEvaluator;

		internal KeyFrameAnimation() => throw new NotSupportedException();

		internal KeyFrameAnimation(Compositor compositor) : base(compositor)
		{
		}

		internal override bool IsTrackedByCompositor => true;

		[NotImplemented]
		public AnimationStopBehavior StopBehavior { get; set; }

		public int IterationCount { get; set; } = 1;

		public AnimationIterationBehavior IterationBehavior { get; set; }

		public TimeSpan Duration { get; set; }

		[NotImplemented]
		public TimeSpan DelayTime { get; set; }

		public int KeyFrameCount => KeyFrameCountCore;

		private protected virtual int KeyFrameCountCore { get; }

		[NotImplemented]
		public AnimationDirection Direction { get; set; }

		internal event EventHandler? Stopped;

		internal override object Evaluate()
		{
			var (value, shouldStop) = _keyframeEvaluator!.Evaluate();
			if (shouldStop)
			{
				Stop();
			}

			return value;
		}

		internal object Evaluate(float progress)
		{
			return _keyframeEvaluator!.Evaluate(progress);
		}

		internal override void Stop()
		{
			base.Stop();
			Stopped?.Invoke(this, EventArgs.Empty);
		}

		internal void Pause()
		{
			_keyframeEvaluator!.Pause();
		}

		// Default base-class implementations: derived animations that want to evaluate the
		// expression at runtime override these (see ScalarKeyFrameAnimation and Vector2KeyFrameAnimation).
		// Vector3/Vector4 currently treat expression keyframes as no-ops which mirrors the prior
		// NotImplemented behavior.
		public virtual void InsertExpressionKeyFrame(float normalizedProgressKey, string value)
		{
		}

		public virtual void InsertExpressionKeyFrame(float normalizedProgressKey, string value, CompositionEasingFunction easingFunction)
		{
		}

		internal void Resume()
		{
			_keyframeEvaluator!.Resume();
		}

		internal float Progress => _keyframeEvaluator!.Progress;

		internal TimeSpan Remaining => _keyframeEvaluator!.Remaining;
	}
}
