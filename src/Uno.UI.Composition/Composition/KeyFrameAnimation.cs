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
		private IKeyFrameEvaluator? _keyframeEvaluatorField;
		private float _playbackRate = 1.0f;

		// Concrete keyframe animations assign this when they Start(); routing through the property lets
		// a PlaybackRate set before Start() take effect the moment the evaluator is created.
		private protected IKeyFrameEvaluator? _keyframeEvaluator
		{
			get => _keyframeEvaluatorField;
			set
			{
				_keyframeEvaluatorField = value;
				value?.SetPlaybackRate(_playbackRate);
			}
		}

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

		// Concrete keyframe animations override this to parse and evaluate the expression. This base
		// fallback warns rather than silently ignoring the keyframe for any type that doesn't.
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

		// Driven by AnimationController.PlaybackRate. Stored so a rate set before Start() (before the
		// evaluator exists) is applied on creation via the _keyframeEvaluator setter.
		internal void SetPlaybackRate(float playbackRate)
		{
			_playbackRate = playbackRate;
			_keyframeEvaluator?.SetPlaybackRate(playbackRate);
		}

		internal void SeekTo(float progress) => _keyframeEvaluator?.SeekTo(progress);

		internal float Progress => _keyframeEvaluator!.Progress;

		internal TimeSpan Remaining => _keyframeEvaluator!.Remaining;
	}
}
