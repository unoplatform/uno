#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

internal sealed class AnimatedImageFrameProvider : IFrameProvider
{
	private readonly ImageFrames _frames;
	private readonly IReadOnlyList<int> _durations;
	private readonly Timer? _timer;
	private readonly Stopwatch? _stopwatch;
	private readonly long _totalDuration;
	private readonly long _memoryPressure;
	private readonly WeakReference<Action> _onFrameChanged;

	private int _currentFrame;
	private int _disposed;

	// Note: The Timer will keep holding onto the AnimatedImageFrameProvider until stopped (it's a static root).
	// But we only stop the timer when we dispose AnimatedImageFrameProvider from CompositionImageSurface finalizer.
	// The onFrameChanged Action is also holding onto CompositionImageSurface.
	// So, if AnimatedImageFrameProvider holds onto onFrameChanged, the CompositionImageSurface is never GC'ed.
	// That's why we make it a WeakReference.
	// Note that CompositionImageSurface keeps an unused private field storing onFrameChanged so that it's not GC'ed early.
	internal AnimatedImageFrameProvider(ImageFrames frames, Action onFrameChanged)
	{
		_frames = frames;
		_durations = frames.DurationsMs;

		if (_frames.Frames.Count < 2)
		{
			throw new ArgumentException("AnimatedImageFrameProvider should only be used when there is at least two frames");
		}

		Debug.Assert(_durations is not null);
		Debug.Assert(_durations.Count == _frames.Frames.Count);
		Debug.Assert(onFrameChanged is not null);

		long total = 0;
		long pressure = 0;
		for (var i = 0; i < _frames.Frames.Count; i++)
		{
			total += _durations[i];
			pressure += (long)_frames.Frames[i].PixelWidth * _frames.Frames[i].PixelHeight * 4;
		}

		_totalDuration = total;
		_onFrameChanged = new WeakReference<Action>(onFrameChanged);
		Debug.Assert(_totalDuration != 0);

		_memoryPressure = pressure;
		GC.AddMemoryPressure(_memoryPressure);

		_stopwatch = Stopwatch.StartNew();
		_timer = new Timer(OnTimerCallback, null, dueTime: _durations[0], period: Timeout.Infinite);
	}

	public IImage? CurrentImage => _frames.Frames[_currentFrame];

	private int GetCurrentFrameIndex()
	{
		var currentTimestampInMilliseconds = _stopwatch!.ElapsedMilliseconds % _totalDuration;
		for (int i = 0; i < _durations.Count; i++)
		{
			if (currentTimestampInMilliseconds < _durations[i])
			{
				return i;
			}

			currentTimestampInMilliseconds -= _durations[i];
		}

		throw new InvalidOperationException("This shouldn't be reachable. A timestamp in total duration range should map to a frame");
	}

	private void SetCurrentFrame()
	{
		var frameIndex = GetCurrentFrameIndex();
		if (_currentFrame != frameIndex)
		{
			_currentFrame = frameIndex;
			Debug.Assert(_onFrameChanged is not null);
			if (_onFrameChanged.TryGetTarget(out var onFrameChanged))
			{
				onFrameChanged();
			}
		}
	}

	private void OnTimerCallback(object? state)
	{
		SetCurrentFrame();

		var timestamp = _stopwatch!.ElapsedMilliseconds % _totalDuration;
		var nextFrameTimeStamp = 0;
		for (int i = 0; i <= _currentFrame; i++)
		{
			nextFrameTimeStamp += _durations[i];
		}

		var dueTime = nextFrameTimeStamp - timestamp;
		if (dueTime < 0)
		{
			// Defensive check. When pausing the program for debugging, the calculations can go wrong.
			dueTime = 16;
		}

		try
		{
			_timer!.Change(dueTime, period: Timeout.Infinite);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			_timer?.Dispose();
			_frames.Dispose();
			GC.RemoveMemoryPressure(_memoryPressure);
		}
	}
}
