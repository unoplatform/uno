using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.UI.Composition;

internal sealed class KeyFrameEvaluator<T> : IKeyFrameEvaluator
{
	private readonly long _startTimestamp;
	private readonly AnimationKeyFrame<T> _initialValue;
	private readonly AnimationKeyFrame<T> _finalValue;
	private readonly TimeSpan _duration;
	private readonly TimeSpan _totalDuration;
	private readonly SortedDictionary<float, AnimationKeyFrame<T>> _keyFrames;
	private readonly Func<AnimationKeyFrame<T>, AnimationKeyFrame<T>, float, T> _lerp;
	private readonly Compositor _compositor;

	private long? _pauseTimestamp;
	private long _totalPause;

	public KeyFrameEvaluator(
		AnimationKeyFrame<T> initialValue,
		AnimationKeyFrame<T> finalValue,
		TimeSpan duration,
		SortedDictionary<float, AnimationKeyFrame<T>> keyFrames,
		Func<AnimationKeyFrame<T>, AnimationKeyFrame<T>, float, T> lerp,
		int iterationCount,
		AnimationIterationBehavior iterationBehavior,
		Compositor compositor)
	{
		_startTimestamp = compositor.TimestampInTicks;
		_initialValue = initialValue;
		_finalValue = finalValue;
		_duration = duration;
		_totalDuration = iterationBehavior == AnimationIterationBehavior.Forever ? TimeSpan.MaxValue : duration * iterationCount;
		_keyFrames = keyFrames;
		_lerp = lerp;
		_compositor = compositor;
	}

	public (object Value, bool ShouldStop) Evaluate()
	{
		// While paused, freeze elapsed time at the pause point — otherwise the evaluator would
		// keep advancing as wall-clock time passes (and report "shouldStop" once it exceeds the
		// duration), even though the animation is being held in place by an AnimationController.
		var nowTimestamp = _pauseTimestamp ?? _compositor.TimestampInTicks;
		var elapsed = new TimeSpan(nowTimestamp - _totalPause - _startTimestamp);
		if (_pauseTimestamp is null && elapsed >= _totalDuration)
		{
			return (_finalValue.Value, true);
		}

		elapsed = TimeSpan.FromTicks(elapsed.Ticks % _duration.Ticks);

		return EvaluateInternal((float)elapsed.Ticks / _duration.Ticks);
	}

	public object Evaluate(float progress)
	{
		if (progress >= 1.0f)
		{
			return (_finalValue.Value, true);
		}

		return EvaluateInternal(progress).Value;
	}

	private (object Value, bool ShouldStop) EvaluateInternal(float currentFrame)
	{
		var lastKey = _keyFrames.Keys.Last();
		// Past the final keyframe: hold the last value. Without this the math below collapses
		// to "previousKeyFrame == nextKeyFrame", producing a divide-by-zero in the lerp ratio
		// and returning NaN — which would make any animated property (Opacity, Scale, …) drop
		// off into invisibility.
		if (currentFrame >= lastKey)
		{
			return (_keyFrames[lastKey].Value, false);
		}

		var nextKeyFrame = _keyFrames.Keys.FirstOrDefault(k => k >= currentFrame, lastKey);
		if (nextKeyFrame == currentFrame)
		{
			// currentFrame is one that exists in the dictionary already.
			return (_keyFrames[currentFrame].Value, false);
		}

		var previousKeyFrame = _keyFrames.Keys.LastOrDefault(k => k <= currentFrame);
		var previousValue = previousKeyFrame == 0.0f ? _initialValue : _keyFrames[previousKeyFrame];
		var nextValue = _keyFrames[nextKeyFrame];
		var newValue = _lerp(previousValue, nextValue, (currentFrame - previousKeyFrame) / (nextKeyFrame - previousKeyFrame));
		return (newValue, false);
	}


	public void Pause()
	{
		if (_pauseTimestamp is not null)
		{
			return;
		}

		_pauseTimestamp = _compositor.TimestampInTicks;
	}

	public void Resume()
	{
		if (_pauseTimestamp is null)
		{
			return;
		}

		_totalPause += (_compositor.TimestampInTicks - _pauseTimestamp.Value);
		_pauseTimestamp = null;
	}

	public float Progress
	{
		get
		{
			var timestamp = _pauseTimestamp ?? _compositor.TimestampInTicks - _totalPause;
			var elapsed = new TimeSpan(timestamp - _startTimestamp);
			if (elapsed >= _totalDuration)
			{
				return 1.0f;
			}

			elapsed = TimeSpan.FromTicks(elapsed.Ticks % _duration.Ticks);

			return (float)elapsed.Ticks / _duration.Ticks;
		}
	}

	/// <summary>
	/// The time remaining until the animation completes.
	/// </summary>
	public TimeSpan Remaining
	{
		get
		{
			if (_totalDuration == TimeSpan.MaxValue)
			{
				return TimeSpan.MaxValue;
			}

			var timestamp = _pauseTimestamp ?? _compositor.TimestampInTicks - _totalPause;
			var elapsed = new TimeSpan(timestamp - _startTimestamp);
			if (elapsed >= _totalDuration)
			{
				return TimeSpan.Zero;
			}

			return _totalDuration - elapsed;
		}
	}
}
