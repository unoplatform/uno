#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Uno;
using Uno.Foundation.Logging;

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

		// These match WinUI's non-virtual KeyFrameAnimation API surface; per-type behaviour is provided
		// by InsertExpressionKeyFrameCore. A virtual modifier here would diverge from WinUI and make the
		// sync generator emit a conflicting NotImplemented stub.
		public void InsertExpressionKeyFrame(float normalizedProgressKey, string value)
			=> InsertExpressionKeyFrame(normalizedProgressKey, value, Compositor.GetDefaultEasingFunction());

		public void InsertExpressionKeyFrame(float normalizedProgressKey, string value, CompositionEasingFunction easingFunction)
			=> InsertExpressionKeyFrameCore(normalizedProgressKey, value, easingFunction);

		// Derived animations that evaluate expressions at runtime override this (ScalarKeyFrameAnimation
		// and Vector2KeyFrameAnimation). Types without expression support (Vector3/Vector4/Boolean) fall
		// through to this, which warns and discards the keyframe so the unsupported path stays diagnosable.
		private protected virtual void InsertExpressionKeyFrameCore(float normalizedProgressKey, string value, CompositionEasingFunction easingFunction)
			=> WarnExpressionKeyFrameNotSupported();

		private void WarnExpressionKeyFrameNotSupported()
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Expression keyframes are not supported for '{GetType().Name}'; the keyframe will be ignored.");
			}
		}

		internal void Resume()
		{
			_keyframeEvaluator!.Resume();
		}

		internal float Progress => _keyframeEvaluator!.Progress;

		internal TimeSpan Remaining => _keyframeEvaluator!.Remaining;
	}
}
