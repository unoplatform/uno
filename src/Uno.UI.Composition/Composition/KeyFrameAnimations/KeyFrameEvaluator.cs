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
	private readonly Func<AnimationKeyFrame<T>, T> _resolve;
	private readonly Compositor _compositor;

	private long? _pauseTimestamp;
	private long _totalPause;

	// PlaybackRate scales the wall-clock → animation-time mapping. Because the rate can change while
	// the animation is running (AnimatedVisualPlayer flips it mid-play), elapsed time is accumulated
	// against an anchor: virtual (rate-scaled) elapsed frozen at the last rate change, plus the raw
	// elapsed since then multiplied by the current rate. A negative rate runs the animation backwards.
	private double _playbackRate = 1.0;
	private double _virtualElapsedAtAnchorTicks;
	private long _rawElapsedAtAnchorTicks;

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
		_startTimestamp = compositor.TimestampInTicks;
		_initialValue = initialValue;
		_finalValue = finalValue;
		_duration = duration;
		_totalDuration = iterationBehavior == AnimationIterationBehavior.Forever ? TimeSpan.MaxValue : duration * iterationCount;
		_keyFrames = keyFrames;
		_lerp = lerp;
		_resolve = resolve;
		_compositor = compositor;
	}

	private T Resolve(AnimationKeyFrame<T> frame) => _resolve is null ? frame.Value : _resolve(frame);

	// Raw elapsed ticks excluding time spent paused.
	private long RawElapsedTicks(long nowTimestamp) => nowTimestamp - _totalPause - _startTimestamp;

	// Rate-scaled elapsed ticks. Equals RawElapsedTicks when the rate is a constant 1.
	private double VirtualElapsedTicks(long nowTimestamp)
		=> _virtualElapsedAtAnchorTicks + (RawElapsedTicks(nowTimestamp) - _rawElapsedAtAnchorTicks) * _playbackRate;

	public void SetPlaybackRate(float playbackRate)
	{
		if (playbackRate == _playbackRate)
		{
			return;
		}

		// Freeze the virtual elapsed at the current point before switching rate so the animation keeps
		// advancing continuously from wherever it is rather than jumping.
		var nowTimestamp = _pauseTimestamp ?? _compositor.TimestampInTicks;
		var rawElapsed = RawElapsedTicks(nowTimestamp);
		_virtualElapsedAtAnchorTicks += (rawElapsed - _rawElapsedAtAnchorTicks) * _playbackRate;
		_rawElapsedAtAnchorTicks = rawElapsed;
		_playbackRate = playbackRate;
	}

	public (object Value, bool ShouldStop) Evaluate()
	{
		// While paused, freeze elapsed time at the pause point — otherwise the evaluator would
		// keep advancing as wall-clock time passes (and report "shouldStop" once it exceeds the
		// duration), even though the animation is being held in place by an AnimationController.
		var nowTimestamp = _pauseTimestamp ?? _compositor.TimestampInTicks;
		var virtualTicks = VirtualElapsedTicks(nowTimestamp);
		if (_pauseTimestamp is null)
		{
			// Forward playback ends past the final iteration; reverse playback ends back at the start.
			if (_playbackRate >= 0 && virtualTicks >= _totalDuration.Ticks)
			{
				return (Resolve(_finalValue), true);
			}

			if (_playbackRate < 0 && virtualTicks <= 0)
			{
				return (Resolve(_initialValue), true);
			}
		}

		if (virtualTicks < 0)
		{
			virtualTicks = 0;
		}

		var frameTicks = (long)(virtualTicks % _duration.Ticks);

		return EvaluateInternal((float)frameTicks / _duration.Ticks);
	}

	public object Evaluate(float progress)
	{
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
		if (_pauseTimestamp is not null)
		{
			return;
		}

		_pauseTimestamp = _compositor.TimestampInTicks;
	}

	public void SeekTo(float progress)
	{
		// Re-anchor virtual elapsed to the given progress WITHOUT pausing, so clock-driven playback (at
		// the current, possibly negative, rate) continues from here. A paused animation stays paused and
		// simply holds the new position.
		var nowTimestamp = _pauseTimestamp ?? _compositor.TimestampInTicks;
		_virtualElapsedAtAnchorTicks = (double)progress * _duration.Ticks;
		_rawElapsedAtAnchorTicks = RawElapsedTicks(nowTimestamp);
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
			var nowTimestamp = _pauseTimestamp ?? _compositor.TimestampInTicks;
			var virtualTicks = VirtualElapsedTicks(nowTimestamp);
			if (_playbackRate >= 0 && virtualTicks >= _totalDuration.Ticks)
			{
				return 1.0f;
			}

			if (virtualTicks <= 0)
			{
				return 0.0f;
			}

			var frameTicks = (long)(virtualTicks % _duration.Ticks);

			return (float)frameTicks / _duration.Ticks;
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

			var nowTimestamp = _pauseTimestamp ?? _compositor.TimestampInTicks;
			var virtualTicks = VirtualElapsedTicks(nowTimestamp);

			// Remaining wall-clock time depends on the current rate: the faster we play, the sooner
			// the (fixed) animation-time distance to the end is covered.
			var rateMagnitude = Math.Abs(_playbackRate);
			if (rateMagnitude == 0)
			{
				return TimeSpan.MaxValue;
			}

			var remainingVirtualTicks = _playbackRate >= 0
				? _totalDuration.Ticks - virtualTicks
				: virtualTicks;

			if (remainingVirtualTicks <= 0)
			{
				return TimeSpan.Zero;
			}

			return TimeSpan.FromTicks((long)(remainingVirtualTicks / rateMagnitude));
		}
	}
}
