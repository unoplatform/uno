using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.UI.Composition;

internal sealed class KeyFrameEvaluator<T> : IKeyFrameEvaluator
{
	private readonly AnimationKeyFrame<T> _initialValue;
	private readonly AnimationKeyFrame<T> _finalValue;
	private readonly TimeSpan _duration;
	private readonly int _iterationCount;
	private readonly AnimationIterationBehavior _iterationBehavior;
	private readonly SortedDictionary<float, AnimationKeyFrame<T>> _keyFrames;
	private readonly Func<AnimationKeyFrame<T>, AnimationKeyFrame<T>, float, T> _lerp;
	private readonly Func<AnimationKeyFrame<T>, T> _resolve;
	private readonly Compositor _compositor;
	private long _lastTimestamp;
	private double _playhead;
	private bool _isPaused;
	private float _playbackRate = 1.0f;

	/// <summary>
	/// Initializes the evaluator. The <c>resolve</c> delegate resolves a keyframe to its value,
	/// evaluating an expression keyframe when present: the lerp path resolves its own endpoints,
	/// while this lets the held/final/exact-hit shortcuts resolve too instead of returning an
	/// expression keyframe's placeholder <see cref="AnimationKeyFrame{T}.Value"/>. A
	/// <see langword="null"/> delegate uses the value as-is (animation types without
	/// expression-keyframe support).
	/// </summary>
	public KeyFrameEvaluator(
		AnimationKeyFrame<T> initialValue,
		AnimationKeyFrame<T> finalValue,
		TimeSpan duration,
		SortedDictionary<float, AnimationKeyFrame<T>> keyFrames,
		Func<AnimationKeyFrame<T>, AnimationKeyFrame<T>, float, T> lerp,
		int iterationCount,
		AnimationIterationBehavior iterationBehavior,
		Compositor compositor,
		Func<AnimationKeyFrame<T>, T> resolve = null)
	{
		_initialValue = initialValue;
		_finalValue = finalValue;
		_duration = duration;
		_iterationCount = iterationBehavior == AnimationIterationBehavior.Forever ? Math.Max(iterationCount, 1) : Math.Max(iterationCount, 1);
		_iterationBehavior = iterationBehavior;
		_keyFrames = keyFrames;
		_lerp = lerp;
		_resolve = resolve;
		_compositor = compositor;
		_lastTimestamp = compositor.TimestampInTicks;
	}

	private T Resolve(AnimationKeyFrame<T> frame) => _resolve is null ? frame.Value : _resolve(frame);

	public (object Value, bool ShouldStop) Evaluate()
	{
		var currentProgress = UpdateProgress(out var shouldStop);
		if (shouldStop)
		{
			return (PlaybackRate < 0 ? Resolve(_initialValue) : Resolve(_finalValue), true);
		}

		return EvaluateInternal(currentProgress);
	}

	public object Evaluate(float progress)
	{
		if (progress <= 0.0f)
		{
			return Resolve(_initialValue);
		}

		if (progress >= 1.0f)
		{
			return Resolve(_finalValue);
		}

		return EvaluateInternal(progress).Value;
	}

	private (object Value, bool ShouldStop) EvaluateInternal(float currentFrame)
	{
		// No value keyframes to interpolate — e.g. an animation defined only with expression
		// keyframes, which Vector3/Vector4/Boolean animations discard. Hold the final value
		// instead of indexing into an empty sequence. Evaluate() still stops it once the
		// duration elapses.
		if (_keyFrames.Count == 0)
		{
			return (Resolve(_finalValue), false);
		}

		var lastKey = _keyFrames.Keys.Last();
		// Past the final keyframe: hold the last value. Without this the math below collapses
		// to "previousKeyFrame == nextKeyFrame", producing a divide-by-zero in the lerp ratio
		// and returning NaN — which would make any animated property (Opacity, Scale, …) drop
		// off into invisibility.
		if (currentFrame >= lastKey)
		{
			return (Resolve(_keyFrames[lastKey]), false);
		}

		var nextKeyFrame = _keyFrames.Keys.FirstOrDefault(k => k >= currentFrame, lastKey);
		if (nextKeyFrame == currentFrame)
		{
			// currentFrame is one that exists in the dictionary already.
			return (Resolve(_keyFrames[currentFrame]), false);
		}

		var previousKeyFrame = _keyFrames.Keys.LastOrDefault(k => k <= currentFrame);
		var previousValue = previousKeyFrame == 0.0f ? _initialValue : _keyFrames[previousKeyFrame];
		var nextValue = _keyFrames[nextKeyFrame];
		var newValue = _lerp(previousValue, nextValue, (currentFrame - previousKeyFrame) / (nextKeyFrame - previousKeyFrame));
		return (newValue, false);
	}


	public void Pause()
	{
		if (_isPaused)
		{
			return;
		}

		AdvanceToCurrentTimestamp();
		_isPaused = true;
	}

	public void SeekTo(float progress) => Seek(progress);

	public void Resume()
	{
		if (!_isPaused)
		{
			return;
		}

		_lastTimestamp = _compositor.TimestampInTicks;
		_isPaused = false;
	}

	public bool IsPaused => _isPaused;

	public void Seek(float progress)
	{
		_playhead = ClampPlayhead(progress);
		_lastTimestamp = _compositor.TimestampInTicks;
	}

	public float PlaybackRate
	{
		get => _playbackRate;
		set
		{
			AdvanceToCurrentTimestamp();
			_playbackRate = value;
		}
	}

	public float Progress
	{
		get
		{
			var currentProgress = UpdateProgress(out _);
			return currentProgress;
		}
	}

	/// <summary>
	/// The time remaining until the animation completes.
	/// </summary>
	public TimeSpan Remaining
	{
		get
		{
			if (_iterationBehavior == AnimationIterationBehavior.Forever || _playbackRate == 0.0f)
			{
				return TimeSpan.MaxValue;
			}

			AdvanceToCurrentTimestamp();

			var boundedPlayhead = Math.Clamp(_playhead, 0.0, _iterationCount);
			var remainingProgress = _playbackRate < 0.0f
				? boundedPlayhead
				: _iterationCount - boundedPlayhead;

			if (remainingProgress <= 0.0)
			{
				return TimeSpan.Zero;
			}

			var remainingTicks = remainingProgress * _duration.Ticks / Math.Abs(_playbackRate);
			return TimeSpan.FromTicks((long)Math.Ceiling(remainingTicks));
		}
	}

	private void AdvanceToCurrentTimestamp()
	{
		if (_isPaused)
		{
			return;
		}

		var now = _compositor.TimestampInTicks;
		var delta = now - _lastTimestamp;
		if (delta == 0)
		{
			return;
		}

		_lastTimestamp = now;

		if (_duration == TimeSpan.Zero || _playbackRate == 0.0f)
		{
			return;
		}

		_playhead += (double)delta * _playbackRate / _duration.Ticks;
	}

	private float UpdateProgress(out bool shouldStop)
	{
		AdvanceToCurrentTimestamp();

		shouldStop = false;

		if (_iterationBehavior != AnimationIterationBehavior.Forever)
		{
			if (_playbackRate < 0.0f)
			{
				if (_playhead <= 0.0)
				{
					_playhead = 0.0;
					shouldStop = true;
				}
			}
			else if (_playbackRate > 0.0f)
			{
				if (_playhead >= _iterationCount)
				{
					_playhead = _iterationCount;
					shouldStop = true;
				}
			}
		}

		return GetCurrentProgress();
	}

	private float GetCurrentProgress()
	{
		if (_iterationBehavior == AnimationIterationBehavior.Forever)
		{
			return (float)WrapProgress(_playhead);
		}

		var boundedPlayhead = Math.Clamp(_playhead, 0.0, _iterationCount);
		if (boundedPlayhead == _iterationCount)
		{
			return 1.0f;
		}

		return (float)(boundedPlayhead % 1.0);
	}

	private double ClampPlayhead(double playhead)
	{
		if (_iterationBehavior == AnimationIterationBehavior.Forever)
		{
			return playhead;
		}

		return Math.Clamp(playhead, 0.0, _iterationCount);
	}

	private static double WrapProgress(double progress)
	{
		var wrapped = progress % 1.0;
		if (wrapped < 0.0)
		{
			wrapped += 1.0;
		}

		return wrapped;
	}
}
