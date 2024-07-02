using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Windows.UI.Composition;

internal sealed class KeyFrameEvaluator<T> : IKeyFrameEvaluator
{
	private readonly long _startTimestamp;
	private readonly T _initialValue;
	private readonly T _finalValue;
	private readonly TimeSpan _duration;
	private readonly TimeSpan _totalDuration;
	private readonly SortedDictionary<float, T> _keyFrames;
	private readonly Func<T, T, float, T> _lerp;
	private readonly Compositor _compositor;

	public KeyFrameEvaluator(
		T initialValue,
		T finalValue,
		TimeSpan duration,
		SortedDictionary<float, T> keyFrames,
		Func<T, T, float, T> lerp,
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
		var elapsed = Stopwatch.GetElapsedTime(_startTimestamp, _compositor.TimestampInTicks);
		if (elapsed >= _totalDuration)
		{
			return (_finalValue, true);
		}

		elapsed = TimeSpan.FromTicks(elapsed.Ticks % _duration.Ticks);

		var currentFrame = (float)elapsed.Ticks / _duration.Ticks;
		var nextKeyFrame = _keyFrames.Keys.First(k => k >= currentFrame);
		if (nextKeyFrame == currentFrame)
		{
			// currentFrame is one that exists in the dictionary already.
			return (_keyFrames[currentFrame], false);
		}

		var previousKeyFrame = _keyFrames.Keys.LastOrDefault(k => k <= currentFrame);
		var previousValue = previousKeyFrame == 0.0f ? _initialValue : _keyFrames[previousKeyFrame];
		var nextValue = _keyFrames[nextKeyFrame];
		var newValue = _lerp(previousValue, nextValue, (currentFrame - previousKeyFrame) / (nextKeyFrame - previousKeyFrame));
		return (newValue, false);
	}
}
